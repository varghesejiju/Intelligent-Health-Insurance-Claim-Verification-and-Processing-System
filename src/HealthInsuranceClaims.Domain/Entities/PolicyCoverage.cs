using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class PolicyCoverage
{
    public int CoverageId { get; set; }
    public int PolicyId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; }
    public string? CoverageDescription { get; set; }
    public decimal MaxCoverageAmount { get; set; }
    public CoverageLimitType CoverageLimitType { get; set; } = CoverageLimitType.CategoryCap;
    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual InsurancePolicy? Policy { get; set; }
}
