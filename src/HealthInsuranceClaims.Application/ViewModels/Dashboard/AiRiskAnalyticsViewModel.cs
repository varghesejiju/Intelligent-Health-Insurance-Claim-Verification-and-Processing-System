using HealthInsuranceClaims.Application.ViewModels.Claims;

namespace HealthInsuranceClaims.Application.ViewModels.Dashboard;

public class AiRiskAnalyticsViewModel
{
    public int TotalEvaluatedClaims { get; set; }
    public int LowRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int HighRiskCount { get; set; }

    public decimal LowRiskPercentage => TotalEvaluatedClaims > 0 ? Math.Round((decimal)LowRiskCount / TotalEvaluatedClaims * 100, 1) : 0m;
    public decimal MediumRiskPercentage => TotalEvaluatedClaims > 0 ? Math.Round((decimal)MediumRiskCount / TotalEvaluatedClaims * 100, 1) : 0m;
    public decimal HighRiskPercentage => TotalEvaluatedClaims > 0 ? Math.Round((decimal)HighRiskCount / TotalEvaluatedClaims * 100, 1) : 0m;

    public double ModelAccuracy { get; set; } = 94.8;
    public double ModelAucRoc { get; set; } = 0.962;
    public double ModelF1Score { get; set; } = 0.931;

    public List<ClaimAiAssessmentViewModel> HighRiskWatchlist { get; set; } = new();
    public List<ClaimAiAssessmentViewModel> RecentAssessments { get; set; } = new();
}
