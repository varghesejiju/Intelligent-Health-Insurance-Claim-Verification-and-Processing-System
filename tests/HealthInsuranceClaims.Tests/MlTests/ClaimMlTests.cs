using HealthInsuranceClaims.Infrastructure.ML;
using HealthInsuranceClaims.Infrastructure.ML.Models;
using Microsoft.ML;
using Xunit;

namespace HealthInsuranceClaims.Tests.MlTests;

public class ClaimMlTests
{
    [Fact]
    public void GenerateSyntheticData_ShouldContainBalancedSamples()
    {
        // Act
        var samples = ClaimMlModelBuilder.GenerateRealisticSyntheticData();

        // Assert
        Assert.NotNull(samples);
        Assert.True(samples.Count >= 500, "Should generate at least 500 training samples");
        Assert.Contains(samples, s => s.IsFraudRisk);
        Assert.Contains(samples, s => !s.IsFraudRisk);
    }

    [Fact]
    public void TrainFraudRiskModel_ShouldProduceValidTrainedModelAndMetrics()
    {
        // Arrange
        var tempModelPath = Path.Combine(Path.GetTempPath(), $"test_ml_model_{Guid.NewGuid():N}.zip");
        var builder = new ClaimMlModelBuilder();

        try
        {
            // Act
            var (model, metrics) = builder.TrainFraudRiskModel(tempModelPath);

            // Assert
            Assert.NotNull(model);
            Assert.True(File.Exists(tempModelPath), "Model file should be saved to disk");
            Assert.True(metrics.AreaUnderRocCurve > 0.85, $"AUC ({metrics.AreaUnderRocCurve}) should exceed 0.85");
            Assert.True(metrics.Accuracy > 0.80, $"Accuracy ({metrics.Accuracy}) should exceed 0.80");

            // Test Prediction
            var mlContext = new MLContext(seed: 42);
            var predEngine = mlContext.Model.CreatePredictionEngine<ClaimMlFeatureInput, FraudRiskPrediction>(model);

            // Predict on a normal, compliant low-cost claim
            var compliantClaim = new ClaimMlFeatureInput
            {
                TotalClaimedAmount = 24000f,
                LengthOfStayDays = 3f,
                DailyCostRatio = 8000f,
                RoomRentRatio = 0.15f,
                PharmacyRatio = 0.25f,
                ExpenseItemsCount = 4,
                PolicySumInsured = 500000f,
                SumInsuredUtilizationRatio = 0.048f,
                PatientPriorClaimsCount = 0,
                PatientPriorApprovedTotal = 0,
                ClaimType = "Reimbursement",
                HospitalType = "Network"
            };

            var compliantPred = predEngine.Predict(compliantClaim);
            Assert.False(compliantPred.PredictedLabel, "Compliant routine claim should not be flagged as high fraud risk");
            Assert.True(compliantPred.Probability < 0.5f, "Probability should be low for compliant claim");
        }
        finally
        {
            if (File.Exists(tempModelPath))
            {
                File.Delete(tempModelPath);
            }
        }
    }
}
