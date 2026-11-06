using HealthInsuranceClaims.Application.ViewModels.Policies;
using HealthInsuranceClaims.Domain.Entities;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IPolicyService
{
    Task<List<PolicyListViewModel>> GetAllPoliciesAsync();
    Task<List<InsurancePolicy>> GetActivePoliciesAsync();
    Task<InsurancePolicy?> GetPolicyByIdAsync(int policyId);
    Task<PolicyDetailsViewModel?> GetPolicyDetailsAsync(int policyId);
    Task<int> CreatePolicyAsync(PolicyFormViewModel model);
    Task<bool> UpdatePolicyAsync(PolicyFormViewModel model);
    Task<bool> TogglePolicyStatusAsync(int policyId);

    Task<bool> AddCoverageAsync(CoverageFormViewModel model);
    Task<bool> DeleteCoverageAsync(int coverageId);

    Task<bool> AddExclusionAsync(ExclusionFormViewModel model);
    Task<bool> DeleteExclusionAsync(int exclusionId);

    Task<bool> AssignPolicyToCustomerAsync(AssignPolicyViewModel model);
    Task<List<CustomerPolicyDisplayViewModel>> GetCustomerPoliciesAsync(string customerId);
    Task<CustomerPolicyDisplayViewModel?> GetCustomerPolicyDetailsAsync(string customerId, int policyId);
    Task<List<CustomerPolicySelectionViewModel>> GetActiveCustomerPoliciesForClaimCreationAsync();
}
