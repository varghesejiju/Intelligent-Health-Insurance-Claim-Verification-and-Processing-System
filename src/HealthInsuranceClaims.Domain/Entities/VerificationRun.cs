namespace HealthInsuranceClaims.Domain.Entities;

public class VerificationRun
{
    public int VerificationRunId { get; set; }
    public int ClaimId { get; set; }
    public DateTime RunDate { get; set; } = DateTime.UtcNow;
    public decimal TotalClaimed { get; set; }
    public decimal TotalEligible { get; set; }
    public decimal TotalNonEligible { get; set; }
    public bool HasExceptions { get; set; }
    public string? SummaryNotes { get; set; }

    // Navigation properties
    public virtual Claim? Claim { get; set; }
    public virtual ICollection<VerificationResult> Results { get; set; } = new List<VerificationResult>();
    public virtual ICollection<VerificationException> Exceptions { get; set; } = new List<VerificationException>();
}
