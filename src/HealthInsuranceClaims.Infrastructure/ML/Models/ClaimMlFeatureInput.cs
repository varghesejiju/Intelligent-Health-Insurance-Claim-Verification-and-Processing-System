using Microsoft.ML.Data;

namespace HealthInsuranceClaims.Infrastructure.ML.Models;

public class ClaimMlFeatureInput
{
    [LoadColumn(0)]
    public float TotalClaimedAmount { get; set; }

    [LoadColumn(1)]
    public float LengthOfStayDays { get; set; }

    [LoadColumn(2)]
    public float DailyCostRatio { get; set; }

    [LoadColumn(3)]
    public float RoomRentRatio { get; set; }

    [LoadColumn(4)]
    public float PharmacyRatio { get; set; }

    [LoadColumn(5)]
    public float ExpenseItemsCount { get; set; }

    [LoadColumn(6)]
    public float PolicySumInsured { get; set; }

    [LoadColumn(7)]
    public float SumInsuredUtilizationRatio { get; set; }

    [LoadColumn(8)]
    public float PatientPriorClaimsCount { get; set; }

    [LoadColumn(9)]
    public float PatientPriorApprovedTotal { get; set; }

    [LoadColumn(10)]
    public string ClaimType { get; set; } = "Reimbursement";

    [LoadColumn(11)]
    public string HospitalType { get; set; } = "Network";

    [LoadColumn(12)]
    public bool IsFraudRisk { get; set; }

    [LoadColumn(13)]
    public string RecommendedDecision { get; set; } = "Approved";

    [LoadColumn(14)]
    public float ExpectedApprovedAmount { get; set; }
}

public class FraudRiskPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}

public class DecisionPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedDecision { get; set; } = string.Empty;

    [ColumnName("Score")]
    public float[] Scores { get; set; } = Array.Empty<float>();
}

public class PayoutPrediction
{
    [ColumnName("Score")]
    public float PredictedApprovedAmount { get; set; }
}
