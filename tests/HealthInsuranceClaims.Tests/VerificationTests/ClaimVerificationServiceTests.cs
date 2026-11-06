using HealthInsuranceClaims.Application.Services;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthInsuranceClaims.Tests.VerificationTests;

public class ClaimVerificationServiceTests
{
    private readonly ClaimVerificationService _sut;

    public ClaimVerificationServiceTests()
    {
        var loggerMock = new Mock<ILogger<ClaimVerificationService>>();
        // CalculateVerification is pure synchronous logic and does not require DbContext
        _sut = new ClaimVerificationService(null!, loggerMock.Object);
    }

    [Fact]
    public void Test1_CoverageLimit_WhenExpenseExceedsLimit_CapsEligibleAndDeductsDifference()
    {
        // Scenario 1 from Specification:
        // Claimed hospital expense = ₹12,000. Configured policy limit = ₹8,000.
        // Expected: Eligible = ₹8,000, Non-eligible = ₹4,000. Reason indicates limit cap.

        // Arrange
        var policy = new InsurancePolicy
        {
            PolicyId = 1,
            PolicyNumber = "POL-TEST-001",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(11),
            IsActive = true
        };
        policy.Coverages.Add(new PolicyCoverage
        {
            PolicyId = 1,
            ExpenseCategory = ExpenseCategory.HospitalRoomCharges,
            MaxCoverageAmount = 8000m,
            IsActive = true
        });

        var claim = new Claim
        {
            ClaimId = 101,
            AdmissionDate = DateTime.UtcNow.AddDays(-5),
            DischargeDate = DateTime.UtcNow.AddDays(-2),
            Expenses = new List<ClaimExpense>
            {
                new()
                {
                    ExpenseId = 1,
                    ExpenseCategory = ExpenseCategory.HospitalRoomCharges,
                    ClaimedAmount = 12000m,
                    ExpenseDescription = "Room charges for 3 days"
                }
            }
        };

        // Act
        var result = _sut.CalculateVerification(claim, policy);

        // Assert
        Assert.Equal(12000m, result.TotalClaimed);
        Assert.Equal(8000m, result.TotalEligible);
        Assert.Equal(4000m, result.TotalNonEligible);
        Assert.False(result.HasExceptions);

        var expenseResult = Assert.Single(result.Results);
        Assert.Equal(8000m, expenseResult.EligibleAmount);
        Assert.Equal(4000m, expenseResult.NonEligibleAmount);
        Assert.Contains("exceeds the configured category limit", expenseResult.Reason);
    }

    [Fact]
    public void Test2_ExcludedExpense_WhenCategoryIsExcluded_SetsEligibleToZeroAndDeductsFullAmount()
    {
        // Scenario 2 from Specification:
        // Claimed expense = ₹5,000. Policy explicitly excludes that expense category.
        // Expected: Eligible = ₹0, Non-eligible = ₹5,000. Exclusion reason displayed.

        // Arrange
        var policy = new InsurancePolicy
        {
            PolicyId = 2,
            PolicyNumber = "POL-TEST-002",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(11),
            IsActive = true
        };
        policy.Exclusions.Add(new PolicyExclusion
        {
            PolicyId = 2,
            ExcludedCategory = ExpenseCategory.OtherMedicalExpenses,
            ExclusionDescription = "Cosmetic procedures and luxury items are explicitly excluded.",
            IsActive = true
        });

        var claim = new Claim
        {
            ClaimId = 102,
            AdmissionDate = DateTime.UtcNow.AddDays(-4),
            DischargeDate = DateTime.UtcNow.AddDays(-1),
            Expenses = new List<ClaimExpense>
            {
                new()
                {
                    ExpenseId = 2,
                    ExpenseCategory = ExpenseCategory.OtherMedicalExpenses,
                    ClaimedAmount = 5000m,
                    ExpenseDescription = "Cosmetic dental work"
                }
            }
        };

        // Act
        var result = _sut.CalculateVerification(claim, policy);

        // Assert
        Assert.Equal(5000m, result.TotalClaimed);
        Assert.Equal(0m, result.TotalEligible);
        Assert.Equal(5000m, result.TotalNonEligible);

        var expenseResult = Assert.Single(result.Results);
        Assert.True(expenseResult.IsExcluded);
        Assert.Equal(0m, expenseResult.EligibleAmount);
        Assert.Equal(5000m, expenseResult.NonEligibleAmount);
        Assert.Contains("Excluded category", expenseResult.Reason);
    }

    [Fact]
    public void Test3_MissingCoverage_WhenCategoryNotConfigured_CreatesManualReviewException()
    {
        // Scenario 3 from Specification:
        // If an expense category has no matching coverage configuration:
        // Do not automatically assume it is covered. Create a verification exception and mark for manual review.

        // Arrange
        var policy = new InsurancePolicy
        {
            PolicyId = 3,
            PolicyNumber = "POL-TEST-003",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(11),
            IsActive = true
            // No coverage rules added for LaboratoryTests
        };

        var claim = new Claim
        {
            ClaimId = 103,
            AdmissionDate = DateTime.UtcNow.AddDays(-3),
            DischargeDate = DateTime.UtcNow.AddDays(-1),
            Expenses = new List<ClaimExpense>
            {
                new()
                {
                    ExpenseId = 3,
                    ExpenseCategory = ExpenseCategory.LaboratoryTests,
                    ClaimedAmount = 4500m,
                    ExpenseDescription = "Specialized genetic blood panel"
                }
            }
        };

        // Act
        var result = _sut.CalculateVerification(claim, policy);

        // Assert
        Assert.Equal(4500m, result.TotalClaimed);
        Assert.Equal(0m, result.TotalEligible); // Not automatically assumed covered
        Assert.Equal(4500m, result.TotalNonEligible);
        Assert.True(result.HasExceptions);

        var exception = Assert.Single(result.Exceptions);
        Assert.Equal("MissingCategoryCoverage", exception.ExceptionType);
        Assert.True(exception.RequiresManualReview);
        Assert.Contains("lacks a configured coverage limit rule", exception.Message);
    }

    [Fact]
    public void Test4_ValidExpense_WithinConfiguredLimit_CalculatesFullEligibility()
    {
        // Expense within limits:
        // Claimed amount: ₹1,500. Limit: ₹3,000.
        // Expected: Eligible = ₹1,500, Non-eligible = ₹0.

        // Arrange
        var policy = new InsurancePolicy
        {
            PolicyId = 4,
            PolicyNumber = "POL-TEST-004",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddMonths(11),
            IsActive = true
        };
        policy.Coverages.Add(new PolicyCoverage
        {
            PolicyId = 4,
            ExpenseCategory = ExpenseCategory.DoctorConsultation,
            MaxCoverageAmount = 3000m,
            IsActive = true
        });

        var claim = new Claim
        {
            ClaimId = 104,
            AdmissionDate = DateTime.UtcNow.AddDays(-2),
            DischargeDate = DateTime.UtcNow.AddDays(-1),
            Expenses = new List<ClaimExpense>
            {
                new()
                {
                    ExpenseId = 4,
                    ExpenseCategory = ExpenseCategory.DoctorConsultation,
                    ClaimedAmount = 1500m,
                    ExpenseDescription = "Specialist physician consultation"
                }
            }
        };

        // Act
        var result = _sut.CalculateVerification(claim, policy);

        // Assert
        Assert.Equal(1500m, result.TotalClaimed);
        Assert.Equal(1500m, result.TotalEligible);
        Assert.Equal(0m, result.TotalNonEligible);
        Assert.False(result.HasExceptions);

        var expenseResult = Assert.Single(result.Results);
        Assert.Equal(1500m, expenseResult.EligibleAmount);
        Assert.Equal(0m, expenseResult.NonEligibleAmount);
        Assert.Contains("within the configured limit", expenseResult.Reason);
    }

    [Fact]
    public void Test5_PolicyDateMismatch_WhenAdmissionDatePrecedesPolicyStart_FlagsManualReviewException()
    {
        // Scenario 4: Policy tenure validity check

        // Arrange
        var policy = new InsurancePolicy
        {
            PolicyId = 5,
            PolicyNumber = "POL-TEST-005",
            StartDate = DateTime.UtcNow.AddMonths(1), // Starts in future!
            EndDate = DateTime.UtcNow.AddMonths(13),
            IsActive = true
        };

        var claim = new Claim
        {
            ClaimId = 105,
            AdmissionDate = DateTime.UtcNow,
            DischargeDate = DateTime.UtcNow.AddDays(2),
            Expenses = new List<ClaimExpense>()
        };

        // Act
        var result = _sut.CalculateVerification(claim, policy);

        // Assert
        Assert.True(result.HasExceptions);
        Assert.Contains(result.Exceptions, e => e.ExceptionType == "PolicyDateMismatch");
    }
}
