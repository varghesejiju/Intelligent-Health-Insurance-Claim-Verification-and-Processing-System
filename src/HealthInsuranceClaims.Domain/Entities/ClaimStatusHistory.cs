using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class ClaimStatusHistory
{
    public int HistoryId { get; set; }
    public int ClaimId { get; set; }
    public ClaimStatus PreviousStatus { get; set; }
    public ClaimStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }

    // Navigation property
    public virtual Claim? Claim { get; set; }
}
