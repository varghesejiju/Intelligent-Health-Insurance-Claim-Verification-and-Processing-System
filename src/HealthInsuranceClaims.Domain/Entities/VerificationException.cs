namespace HealthInsuranceClaims.Domain.Entities;

public class VerificationException
{
    public int ExceptionId { get; set; }
    public int VerificationRunId { get; set; }
    public int? ExpenseId { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool RequiresManualReview { get; set; } = true;

    // Navigation properties
    public virtual VerificationRun? VerificationRun { get; set; }
    public virtual ClaimExpense? Expense { get; set; }
}
