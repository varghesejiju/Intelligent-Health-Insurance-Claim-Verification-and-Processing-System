using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthInsuranceClaims.Web.Controllers;

[Authorize(Roles = "HospitalDesk")]
public class HospitalDeskController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IClaimService _claimService;
    private readonly IClaimWorkflowService _workflowService;
    private readonly IPolicyService _policyService;
    private readonly IHospitalService _hospitalService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HospitalDeskController(
        IDashboardService dashboardService,
        IClaimService claimService,
        IClaimWorkflowService workflowService,
        IPolicyService policyService,
        IHospitalService hospitalService,
        UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _claimService = claimService;
        _workflowService = workflowService;
        _policyService = policyService;
        _hospitalService = hospitalService;
        _userManager = userManager;
    }

    private async Task<(ApplicationUser User, int HospitalId)> GetCurrentHospitalDeskInfoAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) throw new InvalidOperationException("User not authenticated.");

        int hospitalId = user.HospitalId ?? 1;
        if (!user.HospitalId.HasValue)
        {
            var defaultHospital = (await _hospitalService.GetActiveHospitalsAsync()).FirstOrDefault();
            if (defaultHospital != null)
            {
                hospitalId = defaultHospital.HospitalId;
            }
        }

        return (user, hospitalId);
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var (_, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var model = await _dashboardService.GetHospitalDeskDashboardAsync(hospitalId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateClaim()
    {
        var (_, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var hospital = await _hospitalService.GetHospitalByIdAsync(hospitalId);
        var activePolicies = await _policyService.GetActiveCustomerPoliciesForClaimCreationAsync();

        ViewBag.CustomerPolicies = activePolicies;
        ViewBag.HospitalName = hospital?.HospitalName ?? "Network Hospital";
        ViewBag.HospitalId = hospitalId;

        var model = new CreateClaimViewModel
        {
            HospitalId = hospitalId,
            ClaimType = "Cashless",
            AdmissionDate = DateTime.UtcNow.AddDays(-2).Date,
            DischargeDate = DateTime.UtcNow.Date
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClaim(CreateClaimViewModel model)
    {
        var (user, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        model.HospitalId = hospitalId;

        if (model.DischargeDate < model.AdmissionDate)
        {
            ModelState.AddModelError(nameof(model.DischargeDate), "Discharge date cannot be earlier than admission date.");
        }

        var activePolicies = await _policyService.GetActiveCustomerPoliciesForClaimCreationAsync();
        var selectedPolicy = activePolicies.FirstOrDefault(p => p.PolicyId == model.PolicyId && (string.IsNullOrEmpty(model.CustomerId) || p.CustomerId == model.CustomerId));

        if (selectedPolicy == null)
        {
            ModelState.AddModelError(nameof(model.PolicyId), "Please select a valid active patient insurance policy.");
        }
        else
        {
            model.CustomerId = selectedPolicy.CustomerId;
            if (string.IsNullOrWhiteSpace(model.PatientName))
            {
                model.PatientName = selectedPolicy.CustomerName;
            }
        }

        if (!ModelState.IsValid)
        {
            var hospital = await _hospitalService.GetHospitalByIdAsync(hospitalId);
            ViewBag.CustomerPolicies = activePolicies;
            ViewBag.HospitalName = hospital?.HospitalName ?? "Network Hospital";
            ViewBag.HospitalId = hospitalId;
            return View(model);
        }

        try
        {
            var claimId = await _claimService.CreateDraftClaimAsync(model, model.CustomerId!, user.FullName);
            TempData["SuccessMessage"] = "Patient claim draft initiated successfully. Please itemize the medical expenses and upload the bills and discharge summary.";
            return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Error creating claim: " + ex.Message);
            var hospital = await _hospitalService.GetHospitalByIdAsync(hospitalId);
            ViewBag.CustomerPolicies = activePolicies;
            ViewBag.HospitalName = hospital?.HospitalName ?? "Network Hospital";
            ViewBag.HospitalId = hospitalId;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ClaimDetails(int id)
    {
        var (_, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var claim = await _claimService.GetClaimDetailsAsync(id, isStaff: false, hospitalId: hospitalId);

        if (claim == null)
        {
            return NotFound("Claim not found or you are not authorized to view claims from this hospital.");
        }

        return View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(AddExpenseViewModel model)
    {
        var (user, hospitalId) = await GetCurrentHospitalDeskInfoAsync();

        if (model.ClaimedAmount <= 0)
        {
            ModelState.AddModelError(nameof(model.ClaimedAmount), "Amount must be greater than ₹0.");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid expense item data. Please review the inputs.";
            return RedirectToAction(nameof(ClaimDetails), new { id = model.ClaimId });
        }

        var result = await _claimService.AddExpenseAsync(model, user.Id, hospitalId);
        if (result)
        {
            TempData["SuccessMessage"] = "Medical expense item added to quotation successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not add expense item. Ensure the claim is in Draft status.";
        }

        return RedirectToAction(nameof(ClaimDetails), new { id = model.ClaimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveExpense(int expenseId, int claimId)
    {
        var (user, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var result = await _claimService.RemoveExpenseAsync(expenseId, user.Id, hospitalId);

        if (result)
        {
            TempData["SuccessMessage"] = "Expense item removed.";
        }
        else
        {
            TempData["ErrorMessage"] = "Unable to delete expense item.";
        }

        return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(int claimId, DocumentType documentType, IFormFile file)
    {
        var (user, hospitalId) = await GetCurrentHospitalDeskInfoAsync();

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a document file to upload.";
            return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
        }

        var (success, message) = await _claimService.UploadDocumentAsync(claimId, documentType, file, user.Id, user.FullName, hospitalId);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int documentId, int claimId)
    {
        var (user, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var result = await _claimService.DeleteDocumentAsync(documentId, user.Id, hospitalId);

        if (result)
        {
            TempData["SuccessMessage"] = "Document deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not delete document. Only draft claims allow document removal.";
        }

        return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitClaim(int id)
    {
        var (user, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var (success, message) = await _workflowService.SubmitClaimAsync(id, user.Id, user.FullName, hospitalId);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(ClaimDetails), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Claims()
    {
        var (_, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var claims = await _claimService.GetHospitalClaimsAsync(hospitalId);
        var hospital = await _hospitalService.GetHospitalByIdAsync(hospitalId);
        ViewBag.HospitalName = hospital?.HospitalName ?? "Network Hospital";
        return View(claims);
    }

    [HttpGet]
    public async Task<IActionResult> RespondRequest(int id)
    {
        var (_, hospitalId) = await GetCurrentHospitalDeskInfoAsync();
        var claim = await _claimService.GetClaimDetailsAsync(id, isStaff: false, hospitalId: hospitalId);

        if (claim == null)
        {
            return NotFound("Claim not found.");
        }

        if (claim.ClaimStatus != ClaimStatus.InformationRequested)
        {
            TempData["ErrorMessage"] = "Claim is not in InformationRequested status.";
            return RedirectToAction(nameof(ClaimDetails), new { id });
        }

        var latestComment = claim.StatusHistories
            .FirstOrDefault(h => h.NewStatus == ClaimStatus.InformationRequested)?.Comment ?? "Additional medical details or revised bills requested.";

        ViewBag.OfficerQuery = latestComment;
        return View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondRequest(int claimId, string responseComment, IFormFile? additionalFile, DocumentType? documentType)
    {
        var (user, hospitalId) = await GetCurrentHospitalDeskInfoAsync();

        if (additionalFile != null && additionalFile.Length > 0 && documentType.HasValue)
        {
            var uploadResult = await _claimService.UploadDocumentAsync(
                claimId,
                documentType.Value,
                additionalFile,
                user.Id,
                user.FullName,
                hospitalId);

            if (!uploadResult.Success)
            {
                TempData["ErrorMessage"] = uploadResult.Message;
                return RedirectToAction(nameof(RespondRequest), new { id = claimId });
            }
        }

        var result = await _workflowService.ResubmitClaimAsync(
            claimId,
            user.Id,
            user.FullName,
            responseComment,
            hospitalId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Requested clarifications/documents submitted to Insurance Officer successfully.";
            return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(RespondRequest), new { id = claimId });
    }
}
