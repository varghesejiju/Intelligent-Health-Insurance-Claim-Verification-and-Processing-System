using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class InsurancePolicy
{
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string? PolicyDescription { get; set; }
    public PolicyType PolicyType { get; set; } = PolicyType.Individual;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? OverallCoverageLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<PolicyCoverage> Coverages { get; set; } = new List<PolicyCoverage>();
    public virtual ICollection<PolicyExclusion> Exclusions { get; set; } = new List<PolicyExclusion>();
    public virtual ICollection<CustomerPolicy> CustomerPolicies { get; set; } = new List<CustomerPolicy>();
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
