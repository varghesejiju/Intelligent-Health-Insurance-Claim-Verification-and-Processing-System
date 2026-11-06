using HealthInsuranceClaims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<InsurancePolicy> InsurancePolicies { get; }
    DbSet<CustomerPolicy> CustomerPolicies { get; }
    DbSet<PolicyCoverage> PolicyCoverages { get; }
    DbSet<PolicyExclusion> PolicyExclusions { get; }
    DbSet<Hospital> Hospitals { get; }
    DbSet<Claim> Claims { get; }
    DbSet<ClaimExpense> ClaimExpenses { get; }
    DbSet<ClaimDocument> ClaimDocuments { get; }
    DbSet<VerificationRun> VerificationRuns { get; }
    DbSet<VerificationResult> VerificationResults { get; }
    DbSet<VerificationException> VerificationExceptions { get; }
    DbSet<ClaimStatusHistory> ClaimStatusHistories { get; }
    DbSet<ClaimDecision> ClaimDecisions { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
