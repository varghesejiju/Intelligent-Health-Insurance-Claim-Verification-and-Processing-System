using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthInsuranceClaims.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IPolicyService _policyService;
    private readonly IClaimService _claimService;
    private readonly IClaimWorkflowService _workflowService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerController(
        IDashboardService dashboardService,
        IPolicyService policyService,
        IClaimService claimService,
        IClaimWorkflowService workflowService,
        UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _policyService = policyService;
        _claimService = claimService;
        _workflowService = workflowService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var customerId = _userManager.GetUserId(User)!;
        var dashboard = await _dashboardService.GetCustomerDashboardAsync(customerId);
        return View(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> Policies()
    {
        var customerId = _userManager.GetUserId(User)!;
        var policies = await _policyService.GetCustomerPoliciesAsync(customerId);
        return View(policies);
    }

    [HttpGet]
    public async Task<IActionResult> PolicyDetails(int id)
    {
        var customerId = _userManager.GetUserId(User)!;
        var policy = await _policyService.GetCustomerPolicyDetailsAsync(customerId, id);
        if (policy == null)
        {
            return NotFound("Policy not assigned to customer or does not exist.");
        }
        return View(policy);
    }

    [HttpGet]
    public async Task<IActionResult> Claims()
    {
        var customerId = _userManager.GetUserId(User)!;
        var claims = await _claimService.GetCustomerClaimsAsync(customerId);
        return View(claims);
    }

    [HttpGet]
    public async Task<IActionResult> Track(int id)
    {
        var customerId = _userManager.GetUserId(User)!;
        var claim = await _claimService.GetClaimDetailsAsync(id, customerId);
        if (claim == null)
        {
            return NotFound("Claim not found or you do not have permission to view this claim.");
        }
        return View(claim);
    }

    [HttpGet]
    public async Task<IActionResult> RespondRequest(int id)
    {
        var customerId = _userManager.GetUserId(User)!;
        var claim = await _claimService.GetClaimDetailsAsync(id, customerId);
        if (claim == null)
        {
            return NotFound("Claim not found.");
        }

        if (claim.ClaimStatus != ClaimStatus.InformationRequested)
        {
            TempData["ErrorMessage"] = "Claim is not in InformationRequested status.";
            return RedirectToAction(nameof(Track), new { id });
        }

        var latestComment = claim.StatusHistories
            .FirstOrDefault(h => h.NewStatus == ClaimStatus.InformationRequested)?.Comment ?? "Additional details requested.";

        var model = new RespondInformationViewModel
        {
            ClaimId = claim.ClaimId,
            ClaimReferenceNumber = claim.ClaimReferenceNumber,
            LatestOfficerRequest = latestComment
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondRequest(RespondInformationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customerId = _userManager.GetUserId(User)!;
        var customer = await _userManager.GetUserAsync(User);

        var result = await _workflowService.ResubmitClaimAsync(
            model.ClaimId,
            customerId,
            customer?.FullName ?? "Customer",
            model.ResponseComment);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Track), new { id = model.ClaimId });
    }
}
