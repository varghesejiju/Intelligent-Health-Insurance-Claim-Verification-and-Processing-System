namespace HealthInsuranceClaims.Domain.Entities;

public class CustomerPolicy
{
    public int CustomerPolicyId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ApplicationUser? Customer { get; set; }
    public virtual InsurancePolicy? Policy { get; set; }
}
