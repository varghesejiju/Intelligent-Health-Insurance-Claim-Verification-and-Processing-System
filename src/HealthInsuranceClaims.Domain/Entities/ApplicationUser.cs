using Microsoft.AspNetCore.Identity;

namespace HealthInsuranceClaims.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string AccountStatus { get; set; } = "Active";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Optional association with a network hospital (for HospitalDesk role)
    public int? HospitalId { get; set; }
    public virtual Hospital? Hospital { get; set; }

    // Navigation properties
    public virtual ICollection<CustomerPolicy> CustomerPolicies { get; set; } = new List<CustomerPolicy>();
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public virtual ICollection<ClaimDecision> Decisions { get; set; } = new List<ClaimDecision>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
