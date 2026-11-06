using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class Claim
{
    public int ClaimId { get; set; }
    public string ClaimReferenceNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public int HospitalId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime DischargeDate { get; set; }
    public string ClaimType { get; set; } = "Reimbursement";
    public string? ClaimDescription { get; set; }
    public decimal TotalClaimedAmount { get; set; }
    public ClaimStatus ClaimStatus { get; set; } = ClaimStatus.Draft;
    public DateTime? SubmittedDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ApplicationUser? Customer { get; set; }
    public virtual InsurancePolicy? Policy { get; set; }
    public virtual Hospital? Hospital { get; set; }
    public virtual ICollection<ClaimExpense> Expenses { get; set; } = new List<ClaimExpense>();
    public virtual ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
    public virtual ICollection<VerificationRun> VerificationRuns { get; set; } = new List<VerificationRun>();
    public virtual ICollection<ClaimStatusHistory> StatusHistories { get; set; } = new List<ClaimStatusHistory>();
    public virtual ClaimDecision? Decision { get; set; }
}
