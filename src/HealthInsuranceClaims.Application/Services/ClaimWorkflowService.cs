using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthInsuranceClaims.Application.Services;

public class ClaimWorkflowService : IClaimWorkflowService
{
    private readonly IApplicationDbContext _context;
    private readonly IClaimVerificationService _verificationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ClaimWorkflowService> _logger;

    public ClaimWorkflowService(
        IApplicationDbContext context,
        IClaimVerificationService verificationService,
        IAuditService auditService,
        ILogger<ClaimWorkflowService> logger)
    {
        _context = context;
        _verificationService = verificationService;
        _auditService = auditService;
        _logger = logger;
    }

    public bool CanTransition(ClaimStatus currentStatus, ClaimStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (ClaimStatus.Draft, ClaimStatus.Submitted) => true,
            (ClaimStatus.Submitted, ClaimStatus.UnderReview) => true,
            (ClaimStatus.UnderReview, ClaimStatus.InformationRequested) => true,
            (ClaimStatus.InformationRequested, ClaimStatus.Resubmitted) => true,
            (ClaimStatus.Resubmitted, ClaimStatus.UnderReview) => true,
            (ClaimStatus.UnderReview, ClaimStatus.Approved) => true,
            (ClaimStatus.UnderReview, ClaimStatus.PartiallyApproved) => true,
            (ClaimStatus.UnderReview, ClaimStatus.Rejected) => true,
            (ClaimStatus.Approved, ClaimStatus.Closed) => true,
            (ClaimStatus.PartiallyApproved, ClaimStatus.Closed) => true,
            (ClaimStatus.Rejected, ClaimStatus.Closed) => true,
            _ => false
        };
    }

    public async Task<(bool Success, string Message)> SubmitClaimAsync(int claimId, string userId, string userName, int? hospitalId = null)
    {
        var claim = await _context.Claims
            .Include(c => c.Expenses)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
            return (false, "Claim not found.");

        bool isOwner = claim.CustomerId == userId;
        bool isHospitalDesk = hospitalId.HasValue && claim.HospitalId == hospitalId.Value;

        if (!isOwner && !isHospitalDesk)
            return (false, "Unauthorized: You can only submit claims belonging to your account or hospital.");

        if (claim.ClaimStatus != ClaimStatus.Draft)
            return (false, $"Claim cannot be submitted because it is in '{claim.ClaimStatus}' state.");

        if (claim.Expenses == null || !claim.Expenses.Any())
            return (false, "Please add at least one medical expense item before submitting.");

        if (claim.TotalClaimedAmount <= 0)
            return (false, "Claim amount must be greater than ₹0.");

        var oldStatus = claim.ClaimStatus;
        claim.ClaimStatus = ClaimStatus.Submitted;
        claim.SubmittedDate = DateTime.UtcNow;

        _context.ClaimStatusHistories.Add(new ClaimStatusHistory
        {
            ClaimId = claim.ClaimId,
            PreviousStatus = oldStatus,
            NewStatus = ClaimStatus.Submitted,
            ChangedBy = userId,
            ChangedByName = userName,
            ChangedDate = DateTime.UtcNow,
            Comment = "Claim submitted by customer for verification and officer review."
        });

        await _context.SaveChangesAsync();

        // Trigger automatic rule-based verification
        try
        {
            await _verificationService.VerifyClaimAsync(claim.ClaimId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing automatic verification for claim {ClaimId}", claimId);
        }

        await _auditService.LogAsync(
            userId,
            userName,
            "ClaimSubmitted",
            "Claim",
            claim.ClaimId.ToString(),
            $"Claim {claim.ClaimReferenceNumber} submitted with total claimed amount ₹{claim.TotalClaimedAmount:N2}");

        return (true, "Claim successfully submitted. Automatic policy verification completed.");
    }

    public async Task<(bool Success, string Message)> MarkUnderReviewAsync(int claimId, string officerId, string officerName)
    {
        var claim = await _context.Claims.FindAsync(claimId);
        if (claim == null)
            return (false, "Claim not found.");

        if (claim.ClaimStatus != ClaimStatus.Submitted && claim.ClaimStatus != ClaimStatus.Resubmitted)
            return (false, $"Claim cannot be transitioned to UnderReview from '{claim.ClaimStatus}'.");

        var oldStatus = claim.ClaimStatus;
        claim.ClaimStatus = ClaimStatus.UnderReview;

        _context.ClaimStatusHistories.Add(new ClaimStatusHistory
        {
            ClaimId = claim.ClaimId,
            PreviousStatus = oldStatus,
            NewStatus = ClaimStatus.UnderReview,
            ChangedBy = officerId,
            ChangedByName = officerName,
            ChangedDate = DateTime.UtcNow,
            Comment = "Claim review initiated by Insurance Officer."
        });

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            officerId,
            officerName,
            "ClaimUnderReview",
            "Claim",
            claim.ClaimId.ToString(),
            $"Officer {officerName} started reviewing claim {claim.ClaimReferenceNumber}");

        return (true, "Claim is now marked as Under Review.");
    }

    public async Task<(bool Success, string Message)> RequestInformationAsync(int claimId, string officerId, string officerName, string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return (false, "You must specify what additional information or documents are required.");

        var claim = await _context.Claims.FindAsync(claimId);
        if (claim == null)
            return (false, "Claim not found.");

        if (claim.ClaimStatus != ClaimStatus.UnderReview)
            return (false, "Additional information can only be requested while claim is Under Review.");

        var oldStatus = claim.ClaimStatus;
        claim.ClaimStatus = ClaimStatus.InformationRequested;

        _context.ClaimStatusHistories.Add(new ClaimStatusHistory
        {
            ClaimId = claim.ClaimId,
            PreviousStatus = oldStatus,
            NewStatus = ClaimStatus.InformationRequested,
            ChangedBy = officerId,
            ChangedByName = officerName,
            ChangedDate = DateTime.UtcNow,
            Comment = comment.Trim()
        });

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            officerId,
            officerName,
            "InformationRequested",
            "Claim",
            claim.ClaimId.ToString(),
            $"Officer {officerName} requested information for claim {claim.ClaimReferenceNumber}: {comment}");

        return (true, "Information request sent to customer.");
    }

    public async Task<(bool Success, string Message)> ResubmitClaimAsync(int claimId, string customerId, string customerName, string comment, int? hospitalId = null)
    {
        var claim = await _context.Claims.FindAsync(claimId);
        if (claim == null)
            return (false, "Claim not found.");

        bool isOwner = claim.CustomerId == customerId;
        bool isHospitalDesk = hospitalId.HasValue && claim.HospitalId == hospitalId.Value;

        if (!isOwner && !isHospitalDesk)
            return (false, "Unauthorized: You can only respond to claims belonging to your account or hospital.");

        if (claim.ClaimStatus != ClaimStatus.InformationRequested)
            return (false, "Claim is not in InformationRequested state.");

        var oldStatus = claim.ClaimStatus;
        claim.ClaimStatus = ClaimStatus.Resubmitted;

        _context.ClaimStatusHistories.Add(new ClaimStatusHistory
        {
            ClaimId = claim.ClaimId,
            PreviousStatus = oldStatus,
            NewStatus = ClaimStatus.Resubmitted,
            ChangedBy = customerId,
            ChangedByName = customerName,
            ChangedDate = DateTime.UtcNow,
            Comment = string.IsNullOrWhiteSpace(comment) ? "Customer provided requested documentation/clarification." : comment.Trim()
        });

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            customerId,
            customerName,
            "ClaimResubmitted",
            "Claim",
            claim.ClaimId.ToString(),
            $"Customer responded to information request for claim {claim.ClaimReferenceNumber}");

        return (true, "Response submitted successfully. Claim returned to officer review queue.");
    }

    public async Task<(bool Success, string Message)> RecordDecisionAsync(
        int claimId,
        string officerId,
        string officerName,
        DecisionType decision,
        decimal approvedAmount,
        string reason)
    {
        var claim = await _context.Claims
            .Include(c => c.Decision)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
            return (false, "Claim not found.");

        if (claim.ClaimStatus != ClaimStatus.UnderReview)
            return (false, "Claim must be Under Review to record a final decision.");

        if (decision == DecisionType.Rejected && approvedAmount > 0)
        {
            approvedAmount = 0m;
        }

        if ((decision == DecisionType.PartiallyApproved || decision == DecisionType.Rejected) && string.IsNullOrWhiteSpace(reason))
        {
            return (false, "A mandatory reason must be provided for Partial Approval or Rejection.");
        }

        if (decision == DecisionType.Approved && approvedAmount <= 0)
        {
            return (false, "Approved amount must be greater than ₹0 for full approval.");
        }

        var oldStatus = claim.ClaimStatus;
        var newStatus = decision switch
        {
            DecisionType.Approved => ClaimStatus.Approved,
            DecisionType.PartiallyApproved => ClaimStatus.PartiallyApproved,
            DecisionType.Rejected => ClaimStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        };

        claim.ClaimStatus = newStatus;

        // Record or Update Decision Entity
        if (claim.Decision == null)
        {
            claim.Decision = new ClaimDecision
            {
                ClaimId = claim.ClaimId,
                OfficerId = officerId,
                Decision = decision,
                ApprovedAmount = approvedAmount,
                DecisionReason = reason?.Trim() ?? "Claim approved per verified terms.",
                DecisionDate = DateTime.UtcNow
            };
            _context.ClaimDecisions.Add(claim.Decision);
        }
        else
        {
            claim.Decision.OfficerId = officerId;
            claim.Decision.Decision = decision;
            claim.Decision.ApprovedAmount = approvedAmount;
            claim.Decision.DecisionReason = reason?.Trim() ?? "Claim decision updated.";
            claim.Decision.DecisionDate = DateTime.UtcNow;
        }

        // Record Status History
        _context.ClaimStatusHistories.Add(new ClaimStatusHistory
        {
            ClaimId = claim.ClaimId,
            PreviousStatus = oldStatus,
            NewStatus = newStatus,
            ChangedBy = officerId,
            ChangedByName = officerName,
            ChangedDate = DateTime.UtcNow,
            Comment = $"Officer decision: {decision}. Approved Amount: ₹{approvedAmount:N2}. Reason: {reason}"
        });

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            officerId,
            officerName,
            $"Claim{decision}",
            "Claim",
            claim.ClaimId.ToString(),
            $"Final decision {decision} recorded by officer {officerName}. Approved: ₹{approvedAmount:N2}. Reason: {reason}");

        return (true, $"Final claim decision '{decision}' recorded successfully.");
    }
}
