using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();
    public DbSet<CustomerPolicy> CustomerPolicies => Set<CustomerPolicy>();
    public DbSet<PolicyCoverage> PolicyCoverages => Set<PolicyCoverage>();
    public DbSet<PolicyExclusion> PolicyExclusions => Set<PolicyExclusion>();
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimExpense> ClaimExpenses => Set<ClaimExpense>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();
    public DbSet<VerificationRun> VerificationRuns => Set<VerificationRun>();
    public DbSet<VerificationResult> VerificationResults => Set<VerificationResult>();
    public DbSet<VerificationException> VerificationExceptions => Set<VerificationException>();
    public DbSet<ClaimStatusHistory> ClaimStatusHistories => Set<ClaimStatusHistory>();
    public DbSet<ClaimDecision> ClaimDecisions => Set<ClaimDecision>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // InsurancePolicy configuration
        builder.Entity<InsurancePolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId);
            entity.HasIndex(e => e.PolicyNumber).IsUnique();
            entity.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PolicyName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.OverallCoverageLimit).HasColumnType("decimal(18,2)");
        });

        // CustomerPolicy configuration
        builder.Entity<CustomerPolicy>(entity =>
        {
            entity.HasKey(e => e.CustomerPolicyId);
            entity.HasIndex(e => new { e.CustomerId, e.PolicyId });
            entity.HasOne(e => e.Customer)
                .WithMany(u => u.CustomerPolicies)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Policy)
                .WithMany(p => p.CustomerPolicies)
                .HasForeignKey(e => e.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PolicyCoverage configuration
        builder.Entity<PolicyCoverage>(entity =>
        {
            entity.HasKey(e => e.CoverageId);
            entity.HasIndex(e => new { e.PolicyId, e.ExpenseCategory });
            entity.Property(e => e.MaxCoverageAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Policy)
                .WithMany(p => p.Coverages)
                .HasForeignKey(e => e.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PolicyExclusion configuration
        builder.Entity<PolicyExclusion>(entity =>
        {
            entity.HasKey(e => e.ExclusionId);
            entity.HasIndex(e => new { e.PolicyId, e.ExcludedCategory });
            entity.HasOne(e => e.Policy)
                .WithMany(p => p.Exclusions)
                .HasForeignKey(e => e.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Hospital configuration
        builder.Entity<Hospital>(entity =>
        {
            entity.HasKey(e => e.HospitalId);
            entity.Property(e => e.HospitalName).IsRequired().HasMaxLength(200);
        });

        // ApplicationUser and Identity Table Names (Clean schema without AspNet prefix)
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasOne(u => u.Hospital)
                .WithMany()
                .HasForeignKey(u => u.HospitalId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        // Claim configuration
        builder.Entity<Claim>(entity =>
        {
            entity.HasKey(e => e.ClaimId);
            entity.HasIndex(e => e.ClaimReferenceNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ClaimStatus);
            entity.HasIndex(e => e.SubmittedDate);

            entity.Property(e => e.ClaimReferenceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PatientName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.TotalClaimedAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Customer)
                .WithMany(u => u.Claims)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Policy)
                .WithMany(p => p.Claims)
                .HasForeignKey(e => e.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Hospital)
                .WithMany(h => h.Claims)
                .HasForeignKey(e => e.HospitalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ClaimExpense configuration
        builder.Entity<ClaimExpense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId);
            entity.HasIndex(e => e.ClaimId);
            entity.Property(e => e.ClaimedAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Claim)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ClaimDocument configuration
        builder.Entity<ClaimDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId);
            entity.HasIndex(e => e.ClaimId);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoredFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.Claim)
                .WithMany(c => c.Documents)
                .HasForeignKey(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VerificationRun configuration
        builder.Entity<VerificationRun>(entity =>
        {
            entity.HasKey(e => e.VerificationRunId);
            entity.HasIndex(e => e.ClaimId);
            entity.Property(e => e.TotalClaimed).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalEligible).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNonEligible).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Claim)
                .WithMany(c => c.VerificationRuns)
                .HasForeignKey(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VerificationResult configuration
        builder.Entity<VerificationResult>(entity =>
        {
            entity.HasKey(e => e.ResultId);
            entity.HasIndex(e => e.VerificationRunId);
            entity.Property(e => e.ClaimedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.EligibleAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NonEligibleAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AppliedLimit).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.VerificationRun)
                .WithMany(r => r.Results)
                .HasForeignKey(e => e.VerificationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Expense)
                .WithMany()
                .HasForeignKey(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // VerificationException configuration
        builder.Entity<VerificationException>(entity =>
        {
            entity.HasKey(e => e.ExceptionId);
            entity.HasIndex(e => e.VerificationRunId);

            entity.HasOne(e => e.VerificationRun)
                .WithMany(r => r.Exceptions)
                .HasForeignKey(e => e.VerificationRunId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Expense)
                .WithMany()
                .HasForeignKey(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ClaimStatusHistory configuration
        builder.Entity<ClaimStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);
            entity.HasIndex(e => e.ClaimId);

            entity.HasOne(e => e.Claim)
                .WithMany(c => c.StatusHistories)
                .HasForeignKey(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ClaimDecision configuration
        builder.Entity<ClaimDecision>(entity =>
        {
            entity.HasKey(e => e.DecisionId);
            entity.HasIndex(e => e.ClaimId).IsUnique();
            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DecisionReason).IsRequired().HasMaxLength(1000);

            entity.HasOne(e => e.Claim)
                .WithOne(c => c.Decision)
                .HasForeignKey<ClaimDecision>(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Officer)
                .WithMany(u => u.Decisions)
                .HasForeignKey(e => e.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog configuration
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
