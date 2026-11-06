using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthInsuranceClaims.Application.Services;

public class ClaimService : IClaimService
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ClaimService> _logger;

    public ClaimService(
        IApplicationDbContext context,
        IFileStorageService fileStorageService,
        IAuditService auditService,
        ILogger<ClaimService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<string> GenerateClaimReferenceNumberAsync()
    {
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
        var prefix = $"CLM-{yearMonth}-";

        var countThisMonth = await _context.Claims
            .CountAsync(c => c.ClaimReferenceNumber.StartsWith(prefix));

        return $"{prefix}{(countThisMonth + 1):D4}";
    }

    public async Task<int> CreateDraftClaimAsync(CreateClaimViewModel model, string customerId, string customerName)
    {
        var claimRef = await GenerateClaimReferenceNumberAsync();

        var claim = new Claim
        {
            ClaimReferenceNumber = claimRef,
            CustomerId = customerId,
            PolicyId = model.PolicyId,
            HospitalId = model.HospitalId,
            PatientName = model.PatientName.Trim(),
            AdmissionDate = model.AdmissionDate,
            DischargeDate = model.DischargeDate,
            ClaimType = model.ClaimType,
            ClaimDescription = model.ClaimDescription?.Trim(),
            TotalClaimedAmount = 0m,
            ClaimStatus = ClaimStatus.Draft,
            CreatedDate = DateTime.UtcNow
        };

        _context.Claims.Add(claim);

        // Record Initial Draft History
        claim.StatusHistories.Add(new ClaimStatusHistory
        {
            PreviousStatus = ClaimStatus.Draft,
            NewStatus = ClaimStatus.Draft,
            ChangedBy = customerId,
            ChangedByName = customerName,
            ChangedDate = DateTime.UtcNow,
            Comment = "Draft claim initialized."
        });

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            customerId,
            customerName,
            "DraftClaimCreated",
            "Claim",
            claim.ClaimId.ToString(),
            $"Draft claim {claimRef} created for patient {claim.PatientName}");

        return claim.ClaimId;
    }

    public async Task<bool> AddExpenseAsync(AddExpenseViewModel model, string userId, int? hospitalId = null)
    {
        var claim = await _context.Claims
            .Include(c => c.Expenses)
            .FirstOrDefaultAsync(c => c.ClaimId == model.ClaimId);

        if (claim == null) return false;

        bool isOwner = claim.CustomerId == userId;
        bool isHospitalDesk = hospitalId.HasValue && claim.HospitalId == hospitalId.Value;

        // Resource & Status Check: only owning customer or authorized hospital desk can add to draft
        if ((!isOwner && !isHospitalDesk) || claim.ClaimStatus != ClaimStatus.Draft)
            return false;

        var expense = new ClaimExpense
        {
            ClaimId = model.ClaimId,
            ExpenseCategory = model.ExpenseCategory,
            ExpenseDescription = model.ExpenseDescription.Trim(),
            ExpenseDate = model.ExpenseDate,
            ClaimedAmount = model.ClaimedAmount
        };

        _context.ClaimExpenses.Add(expense);
        await _context.SaveChangesAsync();

        // Recalculate strictly from database items
        claim.TotalClaimedAmount = await _context.ClaimExpenses
            .Where(e => e.ClaimId == model.ClaimId)
            .SumAsync(e => e.ClaimedAmount);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveExpenseAsync(int expenseId, string userId, int? hospitalId = null)
    {
        var expense = await _context.ClaimExpenses
            .Include(e => e.Claim)
            .FirstOrDefaultAsync(e => e.ExpenseId == expenseId);

        if (expense == null || expense.Claim == null) return false;

        bool isOwner = expense.Claim.CustomerId == userId;
        bool isHospitalDesk = hospitalId.HasValue && expense.Claim.HospitalId == hospitalId.Value;

        if ((!isOwner && !isHospitalDesk) || expense.Claim.ClaimStatus != ClaimStatus.Draft)
            return false;

        var claimId = expense.ClaimId;
        _context.ClaimExpenses.Remove(expense);
        await _context.SaveChangesAsync();

        // Recalculate total claimed amount
        var claim = await _context.Claims.FindAsync(claimId);
        if (claim != null)
        {
            claim.TotalClaimedAmount = await _context.ClaimExpenses
                .Where(e => e.ClaimId == claimId)
                .SumAsync(e => e.ClaimedAmount);

            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<(bool Success, string Message)> UploadDocumentAsync(
        int claimId,
        DocumentType documentType,
        IFormFile file,
        string userId,
        string userName,
        int? hospitalId = null)
    {
        var claim = await _context.Claims.FindAsync(claimId);
        if (claim == null)
            return (false, "Claim not found.");

        bool isOwner = claim.CustomerId == userId;
        bool isHospitalDesk = hospitalId.HasValue && claim.HospitalId == hospitalId.Value;
        bool canUpload = (isOwner || isHospitalDesk) && (claim.ClaimStatus == ClaimStatus.Draft || claim.ClaimStatus == ClaimStatus.InformationRequested);

        if (!canUpload)
        {
            return (false, "Documents cannot be added while the claim is in this status.");
        }

        var saveResult = await _fileStorageService.SaveFileAsync(file);
        if (!saveResult.Success)
        {
            return (false, saveResult.ErrorMessage);
        }

        var document = new ClaimDocument
        {
            ClaimId = claimId,
            DocumentType = documentType,
            OriginalFileName = file.FileName,
            StoredFileName = saveResult.StoredFileName,
            FilePath = saveResult.FilePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedBy = userId,
            UploadedDate = DateTime.UtcNow
        };

        _context.ClaimDocuments.Add(document);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId,
            userName,
            "DocumentUploaded",
            "ClaimDocument",
            document.DocumentId.ToString(),
            $"Uploaded document '{file.FileName}' ({documentType}) for claim {claim.ClaimReferenceNumber}");

        return (true, "Document uploaded successfully.");
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, string userId, int? hospitalId = null)
    {
        var document = await _context.ClaimDocuments
            .Include(d => d.Claim)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null || document.Claim == null) return false;

        bool isOwner = document.Claim.CustomerId == userId;
        bool isHospitalDesk = hospitalId.HasValue && document.Claim.HospitalId == hospitalId.Value;

        if ((!isOwner && !isHospitalDesk) || document.Claim.ClaimStatus != ClaimStatus.Draft)
            return false;

        _fileStorageService.DeleteFile(document.StoredFileName);
        _context.ClaimDocuments.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ClaimDetailsViewModel?> GetClaimDetailsAsync(int claimId, string? userId = null, bool isStaff = false, int? hospitalId = null)
    {
        var claim = await _context.Claims
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Expenses)
            .Include(c => c.Documents)
            .Include(c => c.StatusHistories)
            .Include(c => c.Decision)
                .ThenInclude(d => d!.Officer)
            .Include(c => c.VerificationRuns)
                .ThenInclude(r => r.Results)
            .Include(c => c.VerificationRuns)
                .ThenInclude(r => r.Exceptions)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);
        if (claim == null) return null;

        // Resource Access Isolation Check: Customers can only access their own claim; Hospital Desk only their hospital's claims
        if (!isStaff)
        {
            bool isOwner = userId != null && claim.CustomerId == userId;
            bool isHospital = hospitalId.HasValue && claim.HospitalId == hospitalId.Value;
            if (!isOwner && !isHospital)
            {
                return null;
            }
        }

        var latestRun = claim.VerificationRuns
            .OrderByDescending(r => r.RunDate)
            .FirstOrDefault();

        // Fetch previous claims for this patient / customer / policy excluding current claim
        var priorClaims = await _context.Claims
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
                .ThenInclude(d => d!.Officer)
            .Where(c => c.ClaimId != claim.ClaimId && (c.CustomerId == claim.CustomerId || c.PolicyId == claim.PolicyId))
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();

        var priorClaimsVm = priorClaims.Select(c => new ClaimListViewModel
        {
            ClaimId = c.ClaimId,
            ClaimReferenceNumber = c.ClaimReferenceNumber,
            HospitalName = c.Hospital?.HospitalName ?? string.Empty,
            PatientName = c.PatientName,
            TotalClaimedAmount = c.TotalClaimedAmount,
            FinalApprovedAmount = c.Decision?.ApprovedAmount,
            DeductionAmount = c.Decision != null && c.Decision.Decision == DecisionType.PartiallyApproved
                ? (c.TotalClaimedAmount - c.Decision.ApprovedAmount)
                : (c.Decision != null && c.Decision.Decision == DecisionType.Rejected ? c.TotalClaimedAmount : 0m),
            DecisionType = c.Decision?.Decision,
            DecisionReason = c.Decision?.DecisionReason,
            DecisionDate = c.Decision?.DecisionDate,
            OfficerName = c.Decision?.Officer?.FullName ?? "Insurance Officer",
            ClaimStatus = c.ClaimStatus,
            SubmittedDate = c.SubmittedDate,
            CreatedDate = c.CreatedDate
        }).ToList();

        decimal totalPrevApproved = priorClaims
            .Where(c => c.Decision != null && (c.ClaimStatus == ClaimStatus.Approved || c.ClaimStatus == ClaimStatus.PartiallyApproved))
            .Sum(c => c.Decision!.ApprovedAmount);

        decimal policyLimit = claim.Policy?.OverallCoverageLimit ?? 0m;
        decimal remainingLimit = policyLimit > 0m ? Math.Max(0m, policyLimit - totalPrevApproved) : 0m;

        return new ClaimDetailsViewModel
        {
            ClaimId = claim.ClaimId,
            ClaimReferenceNumber = claim.ClaimReferenceNumber,
            CustomerId = claim.CustomerId,
            CustomerName = claim.Customer?.FullName ?? "Unknown",
            CustomerEmail = claim.Customer?.Email ?? string.Empty,
            CustomerPhone = claim.Customer?.PhoneNumber,

            PolicyId = claim.PolicyId,
            PolicyNumber = claim.Policy?.PolicyNumber ?? string.Empty,
            PolicyName = claim.Policy?.PolicyName ?? string.Empty,
            OverallCoverageLimit = claim.Policy?.OverallCoverageLimit,

            HospitalId = claim.HospitalId,
            HospitalName = claim.Hospital?.HospitalName ?? string.Empty,
            HospitalAddress = claim.Hospital?.Address,

            PatientName = claim.PatientName,
            AdmissionDate = claim.AdmissionDate,
            DischargeDate = claim.DischargeDate,
            ClaimType = claim.ClaimType,
            ClaimDescription = claim.ClaimDescription,
            TotalClaimedAmount = claim.TotalClaimedAmount,
            ClaimStatus = claim.ClaimStatus,
            SubmittedDate = claim.SubmittedDate,
            CreatedDate = claim.CreatedDate,

            Expenses = claim.Expenses.OrderBy(e => e.ExpenseDate).ToList(),
            Documents = claim.Documents.OrderByDescending(d => d.UploadedDate).ToList(),
            StatusHistories = claim.StatusHistories.OrderByDescending(h => h.ChangedDate).ToList(),
            LatestVerificationRun = latestRun,
            Decision = claim.Decision,

            PatientPreviousClaims = priorClaimsVm,
            TotalPreviousApprovedAmount = totalPrevApproved,
            RemainingCoverageLimit = remainingLimit
        };
    }

    public async Task<List<ClaimListViewModel>> GetCustomerClaimsAsync(string customerId)
    {
        return await _context.Claims
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
            .Include(c => c.VerificationRuns)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                CustomerName = c.Customer != null ? c.Customer.FullName : "",
                PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber : "",
                HospitalName = c.Hospital != null ? c.Hospital.HospitalName : "",
                PatientName = c.PatientName,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                FinalApprovedAmount = c.Decision != null ? c.Decision.ApprovedAmount : null,
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<List<ClaimListViewModel>> GetAllClaimsAsync(
        string? search = null,
        ClaimStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.Claims
            .Include(c => c.Customer)
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
            .Include(c => c.VerificationRuns)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c =>
                c.ClaimReferenceNumber.Contains(s) ||
                c.PatientName.Contains(s) ||
                (c.Customer != null && c.Customer.FullName.Contains(s)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.ClaimStatus == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(c => c.CreatedDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(c => c.CreatedDate <= endOfDay);
        }

        return await query
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                CustomerName = c.Customer != null ? c.Customer.FullName : "Unknown",
                PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber : "",
                HospitalName = c.Hospital != null ? c.Hospital.HospitalName : "",
                PatientName = c.PatientName,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                FinalApprovedAmount = c.Decision != null ? c.Decision.ApprovedAmount : null,
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<List<ClaimListViewModel>> GetHospitalClaimsAsync(int hospitalId)
    {
        return await _context.Claims
            .Include(c => c.Customer)
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
            .Include(c => c.VerificationRuns)
            .Where(c => c.HospitalId == hospitalId)
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                CustomerName = c.Customer != null ? c.Customer.FullName : "",
                PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber : "",
                HospitalName = c.Hospital != null ? c.Hospital.HospitalName : "",
                PatientName = c.PatientName,
                AdmissionDate = c.AdmissionDate,
                DischargeDate = c.DischargeDate,
                ClaimType = c.ClaimType,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                FinalApprovedAmount = c.Decision != null ? (decimal?)c.Decision.ApprovedAmount : null,
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<List<ClaimListViewModel>> GetPendingOfficerClaimsAsync()
    {
        return await _context.Claims
            .Include(c => c.Customer)
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.VerificationRuns)
            .Where(c => c.ClaimStatus == ClaimStatus.Submitted ||
                        c.ClaimStatus == ClaimStatus.UnderReview ||
                        c.ClaimStatus == ClaimStatus.Resubmitted)
            .OrderByDescending(c => c.SubmittedDate)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                CustomerName = c.Customer != null ? c.Customer.FullName : "Unknown",
                PolicyNumber = c.Policy != null ? c.Policy.PolicyNumber : "",
                HospitalName = c.Hospital != null ? c.Hospital.HospitalName : "",
                PatientName = c.PatientName,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<List<ClaimListViewModel>> GetDecidedClaimsAsync(DecisionType? decisionType = null, string? search = null)
    {
        var query = _context.Claims
            .Include(c => c.Customer)
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
                .ThenInclude(d => d!.Officer)
            .Where(c => c.ClaimStatus == ClaimStatus.Approved ||
                        c.ClaimStatus == ClaimStatus.PartiallyApproved ||
                        c.ClaimStatus == ClaimStatus.Rejected)
            .AsQueryable();

        if (decisionType.HasValue)
        {
            query = query.Where(c => c.Decision != null && c.Decision.Decision == decisionType.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(c => c.ClaimReferenceNumber.ToLower().Contains(search) ||
                                     c.PatientName.ToLower().Contains(search) ||
                                     (c.Customer != null && c.Customer.FullName.ToLower().Contains(search)) ||
                                     (c.Hospital != null && c.Hospital.HospitalName.ToLower().Contains(search)));
        }

        var list = await query
            .OrderByDescending(c => c.Decision != null ? c.Decision.DecisionDate : c.CreatedDate)
            .ToListAsync();

        return list.Select(c => new ClaimListViewModel
        {
            ClaimId = c.ClaimId,
            ClaimReferenceNumber = c.ClaimReferenceNumber,
            CustomerName = c.Customer?.FullName ?? "Unknown",
            PolicyNumber = c.Policy?.PolicyNumber ?? string.Empty,
            HospitalName = c.Hospital?.HospitalName ?? string.Empty,
            PatientName = c.PatientName,
            AdmissionDate = c.AdmissionDate,
            DischargeDate = c.DischargeDate,
            ClaimType = c.ClaimType,
            TotalClaimedAmount = c.TotalClaimedAmount,
            FinalApprovedAmount = c.Decision?.ApprovedAmount,
            DeductionAmount = c.Decision != null && c.Decision.Decision == DecisionType.PartiallyApproved
                ? (c.TotalClaimedAmount - c.Decision.ApprovedAmount)
                : (c.Decision != null && c.Decision.Decision == DecisionType.Rejected ? c.TotalClaimedAmount : 0m),
            DecisionType = c.Decision?.Decision,
            DecisionReason = c.Decision?.DecisionReason,
            DecisionDate = c.Decision?.DecisionDate,
            OfficerName = c.Decision?.Officer?.FullName ?? "Insurance Officer",
            ClaimStatus = c.ClaimStatus,
            SubmittedDate = c.SubmittedDate,
            CreatedDate = c.CreatedDate
        }).ToList();
    }
}
