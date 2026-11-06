using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class ClaimDecision
{
    public int DecisionId { get; set; }
    public int ClaimId { get; set; }
    public string OfficerId { get; set; } = string.Empty;
    public DecisionType Decision { get; set; }
    public decimal ApprovedAmount { get; set; }
    public string DecisionReason { get; set; } = string.Empty;
    public DateTime DecisionDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Claim? Claim { get; set; }
    public virtual ApplicationUser? Officer { get; set; }
}
