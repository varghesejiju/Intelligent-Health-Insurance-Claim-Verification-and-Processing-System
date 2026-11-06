using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthInsuranceClaims.Web.Controllers;

[Authorize]
public class ClaimsController : Controller
{
    private readonly IClaimService _claimService;
    private readonly IClaimWorkflowService _workflowService;
    private readonly IPolicyService _policyService;
    private readonly IHospitalService _hospitalService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClaimsController(
        IClaimService claimService,
        IClaimWorkflowService workflowService,
        IPolicyService policyService,
        IHospitalService hospitalService,
        UserManager<ApplicationUser> userManager)
    {
        _claimService = claimService;
        _workflowService = workflowService;
        _policyService = policyService;
        _hospitalService = hospitalService;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create()
    {
        var customerId = _userManager.GetUserId(User)!;
        var customerPolicies = await _policyService.GetCustomerPoliciesAsync(customerId);
        var hospitals = await _hospitalService.GetActiveHospitalsAsync();

        if (!customerPolicies.Any())
        {
            TempData["ErrorMessage"] = "You do not have any active insurance policies assigned. Please contact the administrator.";
            return RedirectToAction("Dashboard", "Customer");
        }

        ViewBag.Policies = new SelectList(customerPolicies, "PolicyId", "PolicyName");
        ViewBag.Hospitals = new SelectList(hospitals, "HospitalId", "HospitalName");

        var user = await _userManager.GetUserAsync(User);
        var model = new CreateClaimViewModel
        {
            PatientName = user?.FullName ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClaimViewModel model)
    {
        var customerId = _userManager.GetUserId(User)!;
        var user = await _userManager.GetUserAsync(User);

        if (model.DischargeDate < model.AdmissionDate)
        {
            ModelState.AddModelError(nameof(model.DischargeDate), "Discharge date cannot precede admission date.");
        }

        if (!ModelState.IsValid)
        {
            var customerPolicies = await _policyService.GetCustomerPoliciesAsync(customerId);
            var hospitals = await _hospitalService.GetActiveHospitalsAsync();
            ViewBag.Policies = new SelectList(customerPolicies, "PolicyId", "PolicyName", model.PolicyId);
            ViewBag.Hospitals = new SelectList(hospitals, "HospitalId", "HospitalName", model.HospitalId);
            return View(model);
        }

        var claimId = await _claimService.CreateDraftClaimAsync(model, customerId, user?.FullName ?? "Customer");
        TempData["SuccessMessage"] = "Draft claim created successfully! You can now itemize medical expenses and attach supporting bills.";
        return RedirectToAction(nameof(Details), new { id = claimId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = _userManager.GetUserId(User);
        bool isStaff = User.IsInRole("Administrator") || User.IsInRole("InsuranceOfficer");

        var claim = await _claimService.GetClaimDetailsAsync(id, currentUserId, isStaff);
        if (claim == null)
        {
            return NotFound("Claim record not found or access is restricted.");
        }

        return View(claim);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(AddExpenseViewModel model)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid expense item data submitted.";
            return RedirectToAction(nameof(Details), new { id = model.ClaimId });
        }

        var success = await _claimService.AddExpenseAsync(model, currentUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Medical expense added to claim draft.";
        }
        else
        {
            TempData["ErrorMessage"] = "Unable to add expense. Ensure the claim is in Draft state and belongs to you.";
        }

        return RedirectToAction(nameof(Details), new { id = model.ClaimId });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveExpense(int expenseId, int claimId)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var success = await _claimService.RemoveExpenseAsync(expenseId, currentUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Expense item removed.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not remove expense.";
        }

        return RedirectToAction(nameof(Details), new { id = claimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(UploadDocumentViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid document file.";
            return RedirectToAction(nameof(Details), new { id = model.ClaimId });
        }

        var currentUserId = _userManager.GetUserId(User)!;
        var user = await _userManager.GetUserAsync(User);

        var result = await _claimService.UploadDocumentAsync(
            model.ClaimId,
            model.DocumentType,
            model.File,
            currentUserId,
            user?.FullName ?? "User");

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id = model.ClaimId });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int documentId, int claimId)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var success = await _claimService.DeleteDocumentAsync(documentId, currentUserId);
        if (success)
        {
            TempData["SuccessMessage"] = "Document removed.";
        }
        else
        {
            TempData["ErrorMessage"] = "Cannot delete document. Only draft claim documents can be deleted by customer.";
        }

        return RedirectToAction(nameof(Details), new { id = claimId });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var user = await _userManager.GetUserAsync(User);

        var result = await _workflowService.SubmitClaimAsync(id, currentUserId, user?.FullName ?? "Customer");
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Track", "Customer", new { id });
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
