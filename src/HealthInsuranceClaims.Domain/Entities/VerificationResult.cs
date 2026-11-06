using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class VerificationResult
{
    public int ResultId { get; set; }
    public int VerificationRunId { get; set; }
    public int ExpenseId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal EligibleAmount { get; set; }
    public decimal NonEligibleAmount { get; set; }
    public bool IsExcluded { get; set; }
    public decimal? AppliedLimit { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Navigation properties
    public virtual VerificationRun? VerificationRun { get; set; }
    public virtual ClaimExpense? Expense { get; set; }
}
