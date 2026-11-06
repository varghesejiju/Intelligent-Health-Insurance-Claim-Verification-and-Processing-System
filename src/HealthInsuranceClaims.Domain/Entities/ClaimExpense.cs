using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class ClaimExpense
{
    public int ExpenseId { get; set; }
    public int ClaimId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; }
    public string? ExpenseDescription { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal ClaimedAmount { get; set; }

    // Navigation property
    public virtual Claim? Claim { get; set; }
}
