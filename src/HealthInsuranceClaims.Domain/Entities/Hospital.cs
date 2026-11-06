namespace HealthInsuranceClaims.Domain.Entities;

public class Hospital
{
    public int HospitalId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
