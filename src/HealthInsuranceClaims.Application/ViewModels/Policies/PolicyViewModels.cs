using System.ComponentModel.DataAnnotations;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Application.ViewModels.Policies;

public class PolicyListViewModel
{
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public PolicyType PolicyType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? OverallCoverageLimit { get; set; }
    public bool IsActive { get; set; }
    public int CoveragesCount { get; set; }
    public int ExclusionsCount { get; set; }
    public int AssignedCustomersCount { get; set; }
}

public class PolicyFormViewModel
{
    public int PolicyId { get; set; }

    [Required(ErrorMessage = "Policy Number is required")]
    [Display(Name = "Policy Number")]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Policy Name is required")]
    [Display(Name = "Policy Name")]
    public string PolicyName { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? PolicyDescription { get; set; }

    [Required]
    [Display(Name = "Policy Type")]
    public PolicyType PolicyType { get; set; } = PolicyType.Individual;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddYears(1).Date;

    [Range(0, 100000000, ErrorMessage = "Limit must be a non-negative amount")]
    [Display(Name = "Overall Coverage Limit (₹)")]
    public decimal? OverallCoverageLimit { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
}

public class PolicyDetailsViewModel
{
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string? PolicyDescription { get; set; }
    public PolicyType PolicyType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? OverallCoverageLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }

    public List<PolicyCoverage> Coverages { get; set; } = new();
    public List<PolicyExclusion> Exclusions { get; set; } = new();
}

public class CoverageFormViewModel
{
    public int CoverageId { get; set; }

    public int PolicyId { get; set; }

    [Required]
    [Display(Name = "Expense Category")]
    public ExpenseCategory ExpenseCategory { get; set; }

    [Display(Name = "Coverage Description")]
    public string? CoverageDescription { get; set; }

    [Required]
    [Range(1, 10000000, ErrorMessage = "Maximum coverage amount must be greater than 0")]
    [Display(Name = "Maximum Coverage Amount (₹)")]
    public decimal MaxCoverageAmount { get; set; }

    [Display(Name = "Limit Type")]
    public CoverageLimitType CoverageLimitType { get; set; } = CoverageLimitType.CategoryCap;
}

public class ExclusionFormViewModel
{
    public int ExclusionId { get; set; }

    public int PolicyId { get; set; }

    [Required]
    [Display(Name = "Excluded Expense Category")]
    public ExpenseCategory ExcludedCategory { get; set; }

    [Required(ErrorMessage = "Exclusion description is required")]
    [Display(Name = "Exclusion Reason/Description")]
    public string ExclusionDescription { get; set; } = string.Empty;
}

public class AssignPolicyViewModel
{
    [Required(ErrorMessage = "Please select a customer")]
    [Display(Name = "Customer")]
    public string CustomerId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a policy")]
    [Display(Name = "Insurance Policy")]
    public int PolicyId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Expiry Date")]
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddYears(1).Date;
}

public class CustomerPolicyDisplayViewModel
{
    public int CustomerPolicyId { get; set; }
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string? PolicyDescription { get; set; }
    public PolicyType PolicyType { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal? OverallCoverageLimit { get; set; }
    public bool IsActive { get; set; }
    public List<PolicyCoverage> Coverages { get; set; } = new();
    public List<PolicyExclusion> Exclusions { get; set; } = new();
}

public class CustomerPolicySelectionViewModel
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public decimal? OverallCoverageLimit { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string DisplayText => $"{CustomerName} ({CustomerEmail}) — {PolicyNumber} - {PolicyName} [Expires: {ExpiryDate:dd-MMM-yyyy}]";
}
