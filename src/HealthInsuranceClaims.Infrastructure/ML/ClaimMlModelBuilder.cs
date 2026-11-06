using HealthInsuranceClaims.Infrastructure.ML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace HealthInsuranceClaims.Infrastructure.ML;

public class ClaimMlModelBuilder
{
    private readonly MLContext _mlContext;

    public ClaimMlModelBuilder()
    {
        // Fixed seed for reproducible training
        _mlContext = new MLContext(seed: 2026);
    }

    public (ITransformer Model, CalibratedBinaryClassificationMetrics Metrics) TrainFraudRiskModel(string modelSavePath)
    {
        var trainingData = GenerateRealisticSyntheticData();
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Train/Test Split (80% train, 20% test)
        var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Feature Engineering Pipeline
        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("ClaimTypeEncoded", nameof(ClaimMlFeatureInput.ClaimType))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("HospitalTypeEncoded", nameof(ClaimMlFeatureInput.HospitalType)))
            .Append(_mlContext.Transforms.Concatenate("Features",
                nameof(ClaimMlFeatureInput.TotalClaimedAmount),
                nameof(ClaimMlFeatureInput.LengthOfStayDays),
                nameof(ClaimMlFeatureInput.DailyCostRatio),
                nameof(ClaimMlFeatureInput.RoomRentRatio),
                nameof(ClaimMlFeatureInput.PharmacyRatio),
                nameof(ClaimMlFeatureInput.ExpenseItemsCount),
                nameof(ClaimMlFeatureInput.PolicySumInsured),
                nameof(ClaimMlFeatureInput.SumInsuredUtilizationRatio),
                nameof(ClaimMlFeatureInput.PatientPriorClaimsCount),
                nameof(ClaimMlFeatureInput.PatientPriorApprovedTotal),
                "ClaimTypeEncoded",
                "HospitalTypeEncoded"))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: nameof(ClaimMlFeatureInput.IsFraudRisk),
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.1));

        // Train
        var model = pipeline.Fit(split.TrainSet);

        // Evaluate
        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: nameof(ClaimMlFeatureInput.IsFraudRisk));

        // Ensure directory exists & save
        var dir = Path.GetDirectoryName(modelSavePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _mlContext.Model.Save(model, dataView.Schema, modelSavePath);

        return (model, metrics);
    }

    public static List<ClaimMlFeatureInput> GenerateRealisticSyntheticData()
    {
        var data = new List<ClaimMlFeatureInput>();
        var random = new Random(42);

        // 1. Normal Compliant Hospitalizations (Appendicitis, Typhoid, Normal Delivery, Fever) -> Low Risk, Approved
        for (int i = 0; i < 300; i++)
        {
            float stay = random.Next(2, 6);
            float dailyRate = random.Next(6000, 16000);
            float total = stay * dailyRate;
            float roomRatio = (float)(random.NextDouble() * 0.15 + 0.10); // 10-25%
            float pharmRatio = (float)(random.NextDouble() * 0.20 + 0.15); // 15-35%
            float policyLimit = 500000f;

            data.Add(new ClaimMlFeatureInput
            {
                TotalClaimedAmount = total,
                LengthOfStayDays = stay,
                DailyCostRatio = total / stay,
                RoomRentRatio = roomRatio,
                PharmacyRatio = pharmRatio,
                ExpenseItemsCount = random.Next(3, 7),
                PolicySumInsured = policyLimit,
                SumInsuredUtilizationRatio = total / policyLimit,
                PatientPriorClaimsCount = random.Next(0, 2),
                PatientPriorApprovedTotal = random.Next(0, 30000),
                ClaimType = "Reimbursement",
                HospitalType = "Network",
                IsFraudRisk = false,
                RecommendedDecision = "Approved",
                ExpectedApprovedAmount = total
            });
        }

        // 2. High-Risk / Anomalous Claims (Inflated Daily Cost, Sudden spikes, Non-network luxury items) -> High Risk, Rejected or Partial
        for (int i = 0; i < 120; i++)
        {
            float stay = random.Next(1, 3);
            float dailyRate = random.Next(45000, 95000); // Unusually high daily rate
            float total = stay * dailyRate;
            float roomRatio = (float)(random.NextDouble() * 0.35 + 0.40); // 40-75% Room Rent (luxury room)
            float pharmRatio = (float)(random.NextDouble() * 0.30 + 0.30);
            float policyLimit = 300000f;

            data.Add(new ClaimMlFeatureInput
            {
                TotalClaimedAmount = total,
                LengthOfStayDays = stay,
                DailyCostRatio = total / stay,
                RoomRentRatio = roomRatio,
                PharmacyRatio = pharmRatio,
                ExpenseItemsCount = random.Next(8, 15),
                PolicySumInsured = policyLimit,
                SumInsuredUtilizationRatio = Math.Min(1.0f, total / policyLimit),
                PatientPriorClaimsCount = random.Next(3, 8),
                PatientPriorApprovedTotal = random.Next(80000, 220000),
                ClaimType = "Reimbursement",
                HospitalType = random.Next(2) == 0 ? "NonNetwork" : "Network",
                IsFraudRisk = true,
                RecommendedDecision = random.Next(2) == 0 ? "Rejected" : "PartiallyApproved",
                ExpectedApprovedAmount = total * 0.45f
            });
        }

        // 3. Medium-Risk / Deductible Claims (Exceeded room sub-caps, slight exclusions) -> Medium Risk, Partially Approved
        for (int i = 0; i < 180; i++)
        {
            float stay = random.Next(3, 7);
            float dailyRate = random.Next(20000, 35000);
            float total = stay * dailyRate;
            float roomRatio = (float)(random.NextDouble() * 0.15 + 0.25); // 25-40% room rent
            float pharmRatio = (float)(random.NextDouble() * 0.15 + 0.20);
            float policyLimit = 500000f;

            data.Add(new ClaimMlFeatureInput
            {
                TotalClaimedAmount = total,
                LengthOfStayDays = stay,
                DailyCostRatio = total / stay,
                RoomRentRatio = roomRatio,
                PharmacyRatio = pharmRatio,
                ExpenseItemsCount = random.Next(5, 10),
                PolicySumInsured = policyLimit,
                SumInsuredUtilizationRatio = total / policyLimit,
                PatientPriorClaimsCount = random.Next(1, 3),
                PatientPriorApprovedTotal = random.Next(20000, 80000),
                ClaimType = "Cashless",
                HospitalType = "Network",
                IsFraudRisk = false,
                RecommendedDecision = "PartiallyApproved",
                ExpectedApprovedAmount = total * 0.85f
            });
        }

        return data;
    }
}
