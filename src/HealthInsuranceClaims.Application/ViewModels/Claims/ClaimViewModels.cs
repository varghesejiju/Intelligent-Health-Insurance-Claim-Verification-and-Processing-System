using System.ComponentModel.DataAnnotations;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace HealthInsuranceClaims.Application.ViewModels.Claims;

public class CreateClaimViewModel
{
    [Display(Name = "Patient Account / Customer ID")]
    public string? CustomerId { get; set; }

    [Required(ErrorMessage = "Please select an insurance policy")]
    [Display(Name = "Insurance Policy")]
    public int PolicyId { get; set; }

    [Required(ErrorMessage = "Please select a network hospital")]
    [Display(Name = "Hospital")]
    public int HospitalId { get; set; }

    [Required(ErrorMessage = "Patient Name is required")]
    [StringLength(150, ErrorMessage = "Patient Name cannot exceed 150 characters")]
    [Display(Name = "Patient Name")]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Admission Date")]
    public DateTime AdmissionDate { get; set; } = DateTime.UtcNow.AddDays(-3).Date;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Discharge Date")]
    public DateTime DischargeDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Display(Name = "Claim Type")]
    public string ClaimType { get; set; } = "Reimbursement";

    [Display(Name = "Diagnosis / Treatment Description")]
    public string? ClaimDescription { get; set; }
}

public class AddExpenseViewModel
{
    public int ClaimId { get; set; }

    [Required]
    [Display(Name = "Expense Category")]
    public ExpenseCategory ExpenseCategory { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [Display(Name = "Item Description")]
    public string ExpenseDescription { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Expense Date")]
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Range(1, 10000000, ErrorMessage = "Amount must be greater than 0")]
    [Display(Name = "Claimed Amount (₹)")]
    public decimal ClaimedAmount { get; set; }
}

public class UploadDocumentViewModel
{
    public int ClaimId { get; set; }

    [Required]
    [Display(Name = "Document Type")]
    public DocumentType DocumentType { get; set; } = DocumentType.HospitalBill;

    [Required(ErrorMessage = "Please select a file to upload")]
    [Display(Name = "File (PDF, JPG, PNG - Max 10MB)")]
    public IFormFile? File { get; set; }
}

public class ClaimDetailsViewModel
{
    public int ClaimId { get; set; }
    public string ClaimReferenceNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    public int PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public decimal? OverallCoverageLimit { get; set; }

    public int HospitalId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string? HospitalAddress { get; set; }

    public string PatientName { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime DischargeDate { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string? ClaimDescription { get; set; }
    public decimal TotalClaimedAmount { get; set; }
    public ClaimStatus ClaimStatus { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime CreatedDate { get; set; }

    public List<ClaimExpense> Expenses { get; set; } = new();
    public List<ClaimDocument> Documents { get; set; } = new();
    public List<ClaimStatusHistory> StatusHistories { get; set; } = new();
    public VerificationRun? LatestVerificationRun { get; set; }
    public ClaimDecision? Decision { get; set; }

    public List<ClaimListViewModel> PatientPreviousClaims { get; set; } = new();
    public decimal TotalPreviousApprovedAmount { get; set; }
    public decimal RemainingCoverageLimit { get; set; }
    public ClaimAiAssessmentViewModel? AiAssessment { get; set; }
}

public class ClaimListViewModel
{
    public int ClaimId { get; set; }
    public string ClaimReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime DischargeDate { get; set; }
    public string ClaimType { get; set; } = "Reimbursement";
    public decimal TotalClaimedAmount { get; set; }
    public decimal? IndicativeEligibleAmount { get; set; }
    public decimal? FinalApprovedAmount { get; set; }
    public decimal? DeductionAmount { get; set; }
    public DecisionType? DecisionType { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? OfficerName { get; set; }
    public ClaimStatus ClaimStatus { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool HasExceptions { get; set; }
}

public class OfficerDecisionViewModel
{
    public int ClaimId { get; set; }
    public string ClaimReferenceNumber { get; set; } = string.Empty;
    public decimal TotalClaimed { get; set; }
    public decimal IndicativeEligible { get; set; }
    public decimal IndicativeNonEligible { get; set; }

    [Required]
    [Display(Name = "Decision")]
    public DecisionType Decision { get; set; } = DecisionType.Approved;

    [Required]
    [Range(0, 100000000, ErrorMessage = "Approved amount must be positive")]
    [Display(Name = "Approved Amount (₹)")]
    public decimal ApprovedAmount { get; set; }

    [Display(Name = "Decision Justification / Reason")]
    public string DecisionReason { get; set; } = string.Empty;
}

public class RequestInformationViewModel
{
    public int ClaimId { get; set; }
    public string ClaimReferenceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please specify what details or records are needed.")]
    [Display(Name = "Information Request Comments")]
    public string Comment { get; set; } = string.Empty;
}

public class RespondInformationViewModel
{
    public int ClaimId { get; set; }
    public string ClaimReferenceNumber { get; set; } = string.Empty;
    public string LatestOfficerRequest { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide your clarification or remarks.")]
    [Display(Name = "Customer Response / Remarks")]
    public string ResponseComment { get; set; } = string.Empty;
}
