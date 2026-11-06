using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Policies;
using HealthInsuranceClaims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Application.Services;

public class PolicyService : IPolicyService
{
    private readonly IApplicationDbContext _context;

    public PolicyService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PolicyListViewModel>> GetAllPoliciesAsync()
    {
        return await _context.InsurancePolicies
            .Select(p => new PolicyListViewModel
            {
                PolicyId = p.PolicyId,
                PolicyNumber = p.PolicyNumber,
                PolicyName = p.PolicyName,
                PolicyType = p.PolicyType,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                OverallCoverageLimit = p.OverallCoverageLimit,
                IsActive = p.IsActive,
                CoveragesCount = p.Coverages.Count,
                ExclusionsCount = p.Exclusions.Count,
                AssignedCustomersCount = p.CustomerPolicies.Count(cp => cp.IsActive)
            })
            .OrderByDescending(p => p.PolicyId)
            .ToListAsync();
    }

    public async Task<List<InsurancePolicy>> GetActivePoliciesAsync()
    {
        return await _context.InsurancePolicies
            .Where(p => p.IsActive)
            .OrderBy(p => p.PolicyName)
            .ToListAsync();
    }

    public async Task<InsurancePolicy?> GetPolicyByIdAsync(int policyId)
    {
        return await _context.InsurancePolicies
            .Include(p => p.Coverages)
            .Include(p => p.Exclusions)
            .FirstOrDefaultAsync(p => p.PolicyId == policyId);
    }

    public async Task<PolicyDetailsViewModel?> GetPolicyDetailsAsync(int policyId)
    {
        var policy = await _context.InsurancePolicies
            .Include(p => p.Coverages)
            .Include(p => p.Exclusions)
            .FirstOrDefaultAsync(p => p.PolicyId == policyId);

        if (policy == null) return null;

        return new PolicyDetailsViewModel
        {
            PolicyId = policy.PolicyId,
            PolicyNumber = policy.PolicyNumber,
            PolicyName = policy.PolicyName,
            PolicyDescription = policy.PolicyDescription,
            PolicyType = policy.PolicyType,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            OverallCoverageLimit = policy.OverallCoverageLimit,
            IsActive = policy.IsActive,
            CreatedDate = policy.CreatedDate,
            Coverages = policy.Coverages.ToList(),
            Exclusions = policy.Exclusions.ToList()
        };
    }

    public async Task<int> CreatePolicyAsync(PolicyFormViewModel model)
    {
        var policy = new InsurancePolicy
        {
            PolicyNumber = model.PolicyNumber.Trim().ToUpper(),
            PolicyName = model.PolicyName.Trim(),
            PolicyDescription = model.PolicyDescription,
            PolicyType = model.PolicyType,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            OverallCoverageLimit = model.OverallCoverageLimit,
            IsActive = model.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        _context.InsurancePolicies.Add(policy);
        await _context.SaveChangesAsync();
        return policy.PolicyId;
    }

    public async Task<bool> UpdatePolicyAsync(PolicyFormViewModel model)
    {
        var policy = await _context.InsurancePolicies.FindAsync(model.PolicyId);
        if (policy == null) return false;

        policy.PolicyNumber = model.PolicyNumber.Trim().ToUpper();
        policy.PolicyName = model.PolicyName.Trim();
        policy.PolicyDescription = model.PolicyDescription;
        policy.PolicyType = model.PolicyType;
        policy.StartDate = model.StartDate;
        policy.EndDate = model.EndDate;
        policy.OverallCoverageLimit = model.OverallCoverageLimit;
        policy.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePolicyStatusAsync(int policyId)
    {
        var policy = await _context.InsurancePolicies.FindAsync(policyId);
        if (policy == null) return false;

        policy.IsActive = !policy.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddCoverageAsync(CoverageFormViewModel model)
    {
        var exists = await _context.PolicyCoverages
            .AnyAsync(c => c.PolicyId == model.PolicyId && c.ExpenseCategory == model.ExpenseCategory);

        if (exists) return false;

        var coverage = new PolicyCoverage
        {
            PolicyId = model.PolicyId,
            ExpenseCategory = model.ExpenseCategory,
            CoverageDescription = model.CoverageDescription,
            MaxCoverageAmount = model.MaxCoverageAmount,
            CoverageLimitType = model.CoverageLimitType,
            IsActive = true
        };

        _context.PolicyCoverages.Add(coverage);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCoverageAsync(int coverageId)
    {
        var coverage = await _context.PolicyCoverages.FindAsync(coverageId);
        if (coverage == null) return false;

        _context.PolicyCoverages.Remove(coverage);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddExclusionAsync(ExclusionFormViewModel model)
    {
        var exists = await _context.PolicyExclusions
            .AnyAsync(e => e.PolicyId == model.PolicyId && e.ExcludedCategory == model.ExcludedCategory);

        if (exists) return false;

        var exclusion = new PolicyExclusion
        {
            PolicyId = model.PolicyId,
            ExcludedCategory = model.ExcludedCategory,
            ExclusionDescription = model.ExclusionDescription.Trim(),
            IsActive = true
        };

        _context.PolicyExclusions.Add(exclusion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteExclusionAsync(int exclusionId)
    {
        var exclusion = await _context.PolicyExclusions.FindAsync(exclusionId);
        if (exclusion == null) return false;

        _context.PolicyExclusions.Remove(exclusion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignPolicyToCustomerAsync(AssignPolicyViewModel model)
    {
        var existing = await _context.CustomerPolicies
            .FirstOrDefaultAsync(cp => cp.CustomerId == model.CustomerId && cp.PolicyId == model.PolicyId);

        if (existing != null)
        {
            existing.IsActive = true;
            existing.ExpiryDate = model.ExpiryDate;
        }
        else
        {
            var assignment = new CustomerPolicy
            {
                CustomerId = model.CustomerId,
                PolicyId = model.PolicyId,
                AssignedDate = DateTime.UtcNow,
                ExpiryDate = model.ExpiryDate,
                IsActive = true
            };
            _context.CustomerPolicies.Add(assignment);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CustomerPolicyDisplayViewModel>> GetCustomerPoliciesAsync(string customerId)
    {
        return await _context.CustomerPolicies
            .Include(cp => cp.Policy)
                .ThenInclude(p => p!.Coverages)
            .Include(cp => cp.Policy)
                .ThenInclude(p => p!.Exclusions)
            .Where(cp => cp.CustomerId == customerId && cp.IsActive)
            .Select(cp => new CustomerPolicyDisplayViewModel
            {
                CustomerPolicyId = cp.CustomerPolicyId,
                PolicyId = cp.PolicyId,
                PolicyNumber = cp.Policy!.PolicyNumber,
                PolicyName = cp.Policy.PolicyName,
                PolicyDescription = cp.Policy.PolicyDescription,
                PolicyType = cp.Policy.PolicyType,
                AssignedDate = cp.AssignedDate,
                ExpiryDate = cp.ExpiryDate,
                OverallCoverageLimit = cp.Policy.OverallCoverageLimit,
                IsActive = cp.Policy.IsActive,
                Coverages = cp.Policy.Coverages.ToList(),
                Exclusions = cp.Policy.Exclusions.ToList()
            })
            .ToListAsync();
    }

    public async Task<CustomerPolicyDisplayViewModel?> GetCustomerPolicyDetailsAsync(string customerId, int policyId)
    {
        var cp = await _context.CustomerPolicies
            .Include(cp => cp.Policy)
                .ThenInclude(p => p!.Coverages)
            .Include(cp => cp.Policy)
                .ThenInclude(p => p!.Exclusions)
            .FirstOrDefaultAsync(cp => cp.CustomerId == customerId && cp.PolicyId == policyId);

        if (cp == null || cp.Policy == null) return null;

        return new CustomerPolicyDisplayViewModel
        {
            CustomerPolicyId = cp.CustomerPolicyId,
            PolicyId = cp.PolicyId,
            PolicyNumber = cp.Policy.PolicyNumber,
            PolicyName = cp.Policy.PolicyName,
            PolicyDescription = cp.Policy.PolicyDescription,
            PolicyType = cp.Policy.PolicyType,
            AssignedDate = cp.AssignedDate,
            ExpiryDate = cp.ExpiryDate,
            OverallCoverageLimit = cp.Policy.OverallCoverageLimit,
            IsActive = cp.Policy.IsActive,
            Coverages = cp.Policy.Coverages.ToList(),
            Exclusions = cp.Policy.Exclusions.ToList()
        };
    }

    public async Task<List<CustomerPolicySelectionViewModel>> GetActiveCustomerPoliciesForClaimCreationAsync()
    {
        return await _context.CustomerPolicies
            .Include(cp => cp.Customer)
            .Include(cp => cp.Policy)
            .Where(cp => cp.IsActive && cp.ExpiryDate >= DateTime.UtcNow && cp.Policy != null && cp.Policy.IsActive)
            .OrderBy(cp => cp.Customer != null ? cp.Customer.FullName : "")
            .Select(cp => new CustomerPolicySelectionViewModel
            {
                CustomerId = cp.CustomerId,
                CustomerName = cp.Customer != null ? cp.Customer.FullName : "Customer",
                CustomerEmail = cp.Customer != null ? (cp.Customer.Email ?? "") : "",
                PolicyId = cp.PolicyId,
                PolicyNumber = cp.Policy != null ? cp.Policy.PolicyNumber : "",
                PolicyName = cp.Policy != null ? cp.Policy.PolicyName : "",
                OverallCoverageLimit = cp.Policy != null ? cp.Policy.OverallCoverageLimit : null,
                ExpiryDate = cp.ExpiryDate
            })
            .ToListAsync();
    }
}
