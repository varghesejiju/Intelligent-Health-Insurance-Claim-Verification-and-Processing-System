using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IClaimWorkflowService
{
    Task<(bool Success, string Message)> SubmitClaimAsync(int claimId, string userId, string userName, int? hospitalId = null);
    Task<(bool Success, string Message)> MarkUnderReviewAsync(int claimId, string officerId, string officerName);
    Task<(bool Success, string Message)> RequestInformationAsync(int claimId, string officerId, string officerName, string comment);
    Task<(bool Success, string Message)> ResubmitClaimAsync(int claimId, string customerId, string customerName, string comment, int? hospitalId = null);
    Task<(bool Success, string Message)> RecordDecisionAsync(int claimId, string officerId, string officerName, DecisionType decision, decimal approvedAmount, string reason);
    bool CanTransition(ClaimStatus currentStatus, ClaimStatus newStatus);
}
