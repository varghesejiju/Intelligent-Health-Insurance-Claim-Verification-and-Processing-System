using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Application.ViewModels.Dashboard;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<CustomerDashboardViewModel> GetCustomerDashboardAsync(string customerId)
    {
        var user = await _userManager.FindByIdAsync(customerId);
        var claims = await _context.Claims
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
            .Include(c => c.VerificationRuns)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        var activePolicies = await _context.CustomerPolicies
            .CountAsync(cp => cp.CustomerId == customerId && cp.IsActive);

        var recentClaims = claims
            .OrderByDescending(c => c.CreatedDate)
            .Take(5)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                PolicyNumber = c.Policy?.PolicyNumber ?? "",
                HospitalName = c.Hospital?.HospitalName ?? "",
                PatientName = c.PatientName,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                FinalApprovedAmount = c.Decision?.ApprovedAmount,
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToList();

        return new CustomerDashboardViewModel
        {
            CustomerName = user?.FullName ?? "Customer",
            ActivePoliciesCount = activePolicies,
            TotalClaims = claims.Count,
            DraftClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Draft),
            SubmittedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Submitted || c.ClaimStatus == ClaimStatus.Resubmitted),
            UnderReviewClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.UnderReview || c.ClaimStatus == ClaimStatus.InformationRequested),
            ApprovedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Approved),
            PartiallyApprovedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.PartiallyApproved),
            RejectedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Rejected),
            TotalClaimedAmount = claims.Sum(c => c.TotalClaimedAmount),
            TotalApprovedAmount = claims.Where(c => c.Decision != null).Sum(c => c.Decision!.ApprovedAmount),
            RecentClaims = recentClaims
        };
    }

    public async Task<OfficerDashboardViewModel> GetOfficerDashboardAsync()
    {
        var claims = await _context.Claims
            .Include(c => c.Customer)
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
                .ThenInclude(d => d!.Officer)
            .Include(c => c.VerificationRuns)
            .ToListAsync();

        var pendingQueue = claims
            .Where(c => c.ClaimStatus == ClaimStatus.Submitted ||
                        c.ClaimStatus == ClaimStatus.UnderReview ||
                        c.ClaimStatus == ClaimStatus.Resubmitted ||
                        c.ClaimStatus == ClaimStatus.InformationRequested)
            .OrderByDescending(c => c.SubmittedDate ?? c.CreatedDate)
            .Take(10)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                CustomerName = c.Customer?.FullName ?? "Unknown",
                PolicyNumber = c.Policy?.PolicyNumber ?? "",
                HospitalName = c.Hospital?.HospitalName ?? "",
                PatientName = c.PatientName,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToList();

        var decidedClaims = claims
            .Where(c => c.ClaimStatus == ClaimStatus.Approved ||
                        c.ClaimStatus == ClaimStatus.PartiallyApproved ||
                        c.ClaimStatus == ClaimStatus.Rejected)
            .OrderByDescending(c => c.Decision != null ? c.Decision.DecisionDate : c.CreatedDate)
            .Take(15)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                CustomerName = c.Customer?.FullName ?? "Unknown",
                PolicyNumber = c.Policy?.PolicyNumber ?? "",
                HospitalName = c.Hospital?.HospitalName ?? "",
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
            })
            .ToList();

        var pendingClaims = claims.Where(c => c.ClaimStatus == ClaimStatus.Submitted || c.ClaimStatus == ClaimStatus.UnderReview || c.ClaimStatus == ClaimStatus.Resubmitted);

        return new OfficerDashboardViewModel
        {
            TotalClaims = claims.Count,
            PendingReviews = pendingClaims.Count(),
            InfoRequestedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.InformationRequested),
            ApprovedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Approved),
            PartiallyApprovedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.PartiallyApproved),
            RejectedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Rejected),
            TotalPendingClaimValue = pendingClaims.Sum(c => c.TotalClaimedAmount),
            TotalApprovedAmount = claims.Where(c => c.Decision != null).Sum(c => c.Decision!.ApprovedAmount),
            PendingReviewQueue = pendingQueue,
            DecidedClaims = decidedClaims
        };
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var customers = await _userManager.GetUsersInRoleAsync("Customer");
        var officers = await _userManager.GetUsersInRoleAsync("InsuranceOfficer");

        var totalPolicies = await _context.InsurancePolicies.CountAsync();
        var activePolicies = await _context.InsurancePolicies.CountAsync(p => p.IsActive);
        var totalHospitals = await _context.Hospitals.CountAsync();

        var claims = await _context.Claims
            .Include(c => c.Customer)
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
            .Include(c => c.VerificationRuns)
            .ToListAsync();

        var statusBreakdown = Enum.GetValues<ClaimStatus>()
            .ToDictionary(s => s, s => claims.Count(c => c.ClaimStatus == s));

        var recentClaims = claims
            .OrderByDescending(c => c.CreatedDate)
            .Take(6)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                CustomerName = c.Customer?.FullName ?? "Unknown",
                PolicyNumber = c.Policy?.PolicyNumber ?? "",
                HospitalName = c.Hospital?.HospitalName ?? "",
                PatientName = c.PatientName,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                FinalApprovedAmount = c.Decision?.ApprovedAmount,
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToList();

        var recentLogs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(6)
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            TotalCustomers = customers.Count,
            TotalOfficers = officers.Count,
            TotalPolicies = totalPolicies,
            ActivePolicies = activePolicies,
            TotalHospitals = totalHospitals,
            TotalClaims = claims.Count,
            AggregateClaimedAmount = claims.Sum(c => c.TotalClaimedAmount),
            AggregateApprovedAmount = claims.Where(c => c.Decision != null).Sum(c => c.Decision!.ApprovedAmount),
            ClaimsByStatus = statusBreakdown,
            RecentClaims = recentClaims,
            RecentAuditLogs = recentLogs
        };
    }

    public async Task<HospitalDeskDashboardViewModel> GetHospitalDeskDashboardAsync(int hospitalId)
    {
        var hospital = await _context.Hospitals.FindAsync(hospitalId);
        var claims = await _context.Claims
            .Include(c => c.Policy)
            .Include(c => c.Hospital)
            .Include(c => c.Decision)
            .Include(c => c.VerificationRuns)
            .Where(c => c.HospitalId == hospitalId)
            .ToListAsync();

        var recentClaims = claims
            .OrderByDescending(c => c.CreatedDate)
            .Take(8)
            .Select(c => new ClaimListViewModel
            {
                ClaimId = c.ClaimId,
                ClaimReferenceNumber = c.ClaimReferenceNumber,
                PolicyNumber = c.Policy?.PolicyNumber ?? "",
                HospitalName = c.Hospital?.HospitalName ?? "",
                PatientName = c.PatientName,
                TotalClaimedAmount = c.TotalClaimedAmount,
                IndicativeEligibleAmount = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => (decimal?)r.TotalEligible).FirstOrDefault(),
                FinalApprovedAmount = c.Decision?.ApprovedAmount,
                ClaimStatus = c.ClaimStatus,
                SubmittedDate = c.SubmittedDate,
                CreatedDate = c.CreatedDate,
                HasExceptions = c.VerificationRuns.OrderByDescending(r => r.RunDate).Select(r => r.HasExceptions).FirstOrDefault()
            })
            .ToList();

        return new HospitalDeskDashboardViewModel
        {
            HospitalId = hospitalId,
            HospitalName = hospital?.HospitalName ?? "Network Hospital",
            TotalClaims = claims.Count,
            DraftClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Draft),
            PendingReviewClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Submitted || c.ClaimStatus == ClaimStatus.UnderReview || c.ClaimStatus == ClaimStatus.Resubmitted),
            InfoRequestedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.InformationRequested),
            ApprovedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Approved),
            PartiallyApprovedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.PartiallyApproved),
            RejectedClaims = claims.Count(c => c.ClaimStatus == ClaimStatus.Rejected),
            TotalClaimedAmount = claims.Sum(c => c.TotalClaimedAmount),
            TotalApprovedAmount = claims.Where(c => c.Decision != null).Sum(c => c.Decision!.ApprovedAmount),
            RecentClaims = recentClaims
        };
    }
}
