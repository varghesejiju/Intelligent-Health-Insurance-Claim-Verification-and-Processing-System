namespace HealthInsuranceClaims.Application.ViewModels.Claims;

public class ClaimAiAssessmentViewModel
{
    public int ClaimId { get; set; }
    public string ClaimReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public decimal TotalClaimedAmount { get; set; }

    // Risk Meter (0 to 100%)
    public decimal FraudRiskScore { get; set; }
    public string RiskLevel { get; set; } = "Low"; // "Low", "Medium", "High"
    public string RiskLevelBadgeClass => RiskLevel switch
    {
        "High" => "bg-danger text-white",
        "Medium" => "bg-warning text-dark",
        _ => "bg-success text-white"
    };

    // Recommended Decision (Approved, PartiallyApproved, Rejected)
    public string RecommendedDecision { get; set; } = "Approved";
    public decimal DecisionConfidence { get; set; }

    // Estimated Admissible Amount
    public decimal EstimatedAdmissibleAmount { get; set; }

    // Explainable AI Factors
    public List<string> RiskFactors { get; set; } = new();
    public List<string> PositiveFactors { get; set; } = new();

    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
}
