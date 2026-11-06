namespace HealthInsuranceClaims.Domain.Entities;

public class AuditLog
{
    public int AuditId { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
