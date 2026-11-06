using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class PolicyExclusion
{
    public int ExclusionId { get; set; }
    public int PolicyId { get; set; }
    public ExpenseCategory ExcludedCategory { get; set; }
    public string? ExclusionDescription { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual InsurancePolicy? Policy { get; set; }
}
