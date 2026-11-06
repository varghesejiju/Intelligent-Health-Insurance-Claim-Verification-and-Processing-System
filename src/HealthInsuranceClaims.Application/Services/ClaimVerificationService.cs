using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthInsuranceClaims.Application.Services;

public class ClaimVerificationService : IClaimVerificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ClaimVerificationService> _logger;

    public ClaimVerificationService(IApplicationDbContext context, ILogger<ClaimVerificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VerificationRun> VerifyClaimAsync(int claimId)
    {
        var claim = await _context.Claims
            .Include(c => c.Expenses)
            .Include(c => c.Policy)
                .ThenInclude(p => p!.Coverages)
            .Include(c => c.Policy)
                .ThenInclude(p => p!.Exclusions)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
        {
            throw new ArgumentException($"Claim with ID {claimId} not found.", nameof(claimId));
        }

        if (claim.Policy == null)
        {
            throw new InvalidOperationException($"Claim {claim.ClaimReferenceNumber} has no assigned policy.");
        }

        // Execute pure rule-based calculation
        var run = CalculateVerification(claim, claim.Policy);

        // Persist run and associated details
        _context.VerificationRuns.Add(run);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Verification run {RunId} completed for claim {ClaimRef}. Total claimed: ₹{Claimed}, Eligible: ₹{Eligible}, Non-eligible: ₹{NonEligible}, Exceptions: {ExceptionCount}",
            run.VerificationRunId, claim.ClaimReferenceNumber, run.TotalClaimed, run.TotalEligible, run.TotalNonEligible, run.Exceptions.Count);

        return run;
    }

    public VerificationRun CalculateVerification(Claim claim, InsurancePolicy policy)
    {
        var run = new VerificationRun
        {
            ClaimId = claim.ClaimId,
            RunDate = DateTime.UtcNow,
            TotalClaimed = 0m,
            TotalEligible = 0m,
            TotalNonEligible = 0m,
            HasExceptions = false,
            SummaryNotes = string.Empty
        };

        var notes = new List<string>();

        // 1. Date Validity Check
        if (claim.AdmissionDate < policy.StartDate || claim.AdmissionDate > policy.EndDate)
        {
            run.HasExceptions = true;
            run.Exceptions.Add(new VerificationException
            {
                ExceptionType = "PolicyDateMismatch",
                Message = $"Admission date ({claim.AdmissionDate:dd-MMM-yyyy}) falls outside the policy tenure ({policy.StartDate:dd-MMM-yyyy} to {policy.EndDate:dd-MMM-yyyy}).",
                RequiresManualReview = true
            });
            notes.Add("Admission date is outside policy validity dates.");
        }

        if (!policy.IsActive)
        {
            run.HasExceptions = true;
            run.Exceptions.Add(new VerificationException
            {
                ExceptionType = "PolicyInactive",
                Message = "The insurance policy is marked as inactive in system configuration.",
                RequiresManualReview = true
            });
            notes.Add("Policy is inactive.");
        }

        // Active coverages and exclusions lookup
        var activeCoverages = policy.Coverages?.Where(c => c.IsActive).ToDictionary(c => c.ExpenseCategory)
            ?? new Dictionary<ExpenseCategory, PolicyCoverage>();

        var activeExclusions = policy.Exclusions?.Where(e => e.IsActive).ToDictionary(e => e.ExcludedCategory)
            ?? new Dictionary<ExpenseCategory, PolicyExclusion>();

        decimal totalClaimedSum = 0m;
        decimal totalEligibleSum = 0m;
        decimal totalNonEligibleSum = 0m;

        // 2. Evaluate Each Claim Expense Item
        if (claim.Expenses != null && claim.Expenses.Any())
        {
            foreach (var expense in claim.Expenses)
            {
                totalClaimedSum += expense.ClaimedAmount;

                var result = new VerificationResult
                {
                    ExpenseId = expense.ExpenseId,
                    ExpenseCategory = expense.ExpenseCategory,
                    ClaimedAmount = expense.ClaimedAmount
                };

                // Check 2a: Explicit Policy Exclusion
                if (activeExclusions.TryGetValue(expense.ExpenseCategory, out var exclusion))
                {
                    result.IsExcluded = true;
                    result.EligibleAmount = 0m;
                    result.NonEligibleAmount = expense.ClaimedAmount;
                    result.Reason = $"Excluded category: {exclusion.ExclusionDescription}";

                    totalNonEligibleSum += expense.ClaimedAmount;
                    run.Results.Add(result);
                    continue;
                }

                // Check 2b: Missing Coverage Configuration
                if (!activeCoverages.TryGetValue(expense.ExpenseCategory, out var coverage))
                {
                    result.IsExcluded = false;
                    result.EligibleAmount = 0m;
                    result.NonEligibleAmount = expense.ClaimedAmount;
                    result.Reason = "No matching coverage rule configured in policy. Flagged for officer manual review.";

                    totalNonEligibleSum += expense.ClaimedAmount;
                    run.HasExceptions = true;

                    run.Exceptions.Add(new VerificationException
                    {
                        ExpenseId = expense.ExpenseId,
                        ExceptionType = "MissingCategoryCoverage",
                        Message = $"Category '{expense.ExpenseCategory}' lacks a configured coverage limit rule in policy {policy.PolicyNumber}.",
                        RequiresManualReview = true
                    });

                    run.Results.Add(result);
                    continue;
                }

                // Check 2c: Configured Limit Evaluation
                result.AppliedLimit = coverage.MaxCoverageAmount;

                if (expense.ClaimedAmount <= coverage.MaxCoverageAmount)
                {
                    result.EligibleAmount = expense.ClaimedAmount;
                    result.NonEligibleAmount = 0m;
                    result.Reason = $"Claimed amount ₹{expense.ClaimedAmount:N2} is within the configured limit of ₹{coverage.MaxCoverageAmount:N2}.";

                    totalEligibleSum += expense.ClaimedAmount;
                }
                else
                {
                    // Exceeds category limit
                    var excess = expense.ClaimedAmount - coverage.MaxCoverageAmount;
                    result.EligibleAmount = coverage.MaxCoverageAmount;
                    result.NonEligibleAmount = excess;
                    result.Reason = $"Expense exceeds the configured category limit of ₹{coverage.MaxCoverageAmount:N2}. Capped at ₹{coverage.MaxCoverageAmount:N2} (Deduction of ₹{excess:N2}).";

                    totalEligibleSum += coverage.MaxCoverageAmount;
                    totalNonEligibleSum += excess;
                }

                run.Results.Add(result);
            }
        }
        else
        {
            run.HasExceptions = true;
            run.Exceptions.Add(new VerificationException
            {
                ExceptionType = "NoExpensesAttached",
                Message = "Claim has no expense line items submitted.",
                RequiresManualReview = true
            });
            notes.Add("No expense items attached.");
        }

        // Check 3: Overall Policy Coverage Limit Cap
        if (policy.OverallCoverageLimit.HasValue && totalEligibleSum > policy.OverallCoverageLimit.Value)
        {
            var overallExcess = totalEligibleSum - policy.OverallCoverageLimit.Value;
            run.HasExceptions = true;
            run.Exceptions.Add(new VerificationException
            {
                ExceptionType = "OverallPolicyLimitExceeded",
                Message = $"Aggregate eligible expense amount (₹{totalEligibleSum:N2}) exceeds policy overall coverage limit of ₹{policy.OverallCoverageLimit.Value:N2} by ₹{overallExcess:N2}.",
                RequiresManualReview = true
            });

            notes.Add($"Eligible amount exceeds overall policy limit of ₹{policy.OverallCoverageLimit.Value:N2}.");
        }

        run.TotalClaimed = totalClaimedSum;
        run.TotalEligible = totalEligibleSum;
        run.TotalNonEligible = totalNonEligibleSum;

        if (run.HasExceptions)
        {
            notes.Add($"{run.Exceptions.Count} exception(s) require manual review by the Insurance Officer.");
        }
        else
        {
            notes.Add("Automatic rule verification completed with no policy exceptions.");
        }

        run.SummaryNotes = string.Join(" ", notes);

        return run;
    }
}
