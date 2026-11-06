using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Application.ViewModels.Dashboard;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IClaimAiService
{
    ClaimAiAssessmentViewModel AssessClaim(ClaimDetailsViewModel claim);
    Task<ClaimAiAssessmentViewModel> AssessClaimAsync(int claimId);
    Task<AiRiskAnalyticsViewModel> GetAiRiskAnalyticsAsync();
    Task<bool> TrainOrRetrainModelAsync();
    bool IsModelTrained();
}
