using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthInsuranceClaims.Web.Controllers;

[Authorize(Roles = "InsuranceOfficer")]
public class OfficerController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IClaimService _claimService;
    private readonly IClaimWorkflowService _workflowService;
    private readonly IClaimAiService _claimAiService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OfficerController(
        IDashboardService dashboardService,
        IClaimService claimService,
        IClaimWorkflowService workflowService,
        IClaimAiService claimAiService,
        UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _claimService = claimService;
        _workflowService = workflowService;
        _claimAiService = claimAiService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var dashboard = await _dashboardService.GetOfficerDashboardAsync();
        return View(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> PendingClaims()
    {
        var pending = await _claimService.GetPendingOfficerClaimsAsync();
        return View(pending);
    }

    [HttpGet]
    public async Task<IActionResult> DecidedClaims(DecisionType? decision = null, string? search = null)
    {
        ViewBag.SelectedDecision = decision;
        ViewBag.Search = search;
        var decidedClaims = await _claimService.GetDecidedClaimsAsync(decision, search);
        return View(decidedClaims);
    }

    [HttpGet]
    public async Task<IActionResult> AllClaims(string? search = null, ClaimStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        var claims = await _claimService.GetAllClaimsAsync(search, status, fromDate, toDate);
        return View(claims);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var officerId = _userManager.GetUserId(User)!;
        var claim = await _claimService.GetClaimDetailsAsync(id, officerId, isStaff: true);
        if (claim == null)
        {
            return NotFound("Claim record not found.");
        }

        claim.AiAssessment = _claimAiService.AssessClaim(claim);

        // If the claim is currently Submitted or Resubmitted, automatically initiate UnderReview state
        if (claim.ClaimStatus == ClaimStatus.Submitted || claim.ClaimStatus == ClaimStatus.Resubmitted)
        {
            var officer = await _userManager.GetUserAsync(User);
            await _workflowService.MarkUnderReviewAsync(id, officerId, officer?.FullName ?? "Officer");
            claim.ClaimStatus = ClaimStatus.UnderReview;
        }

        var decisionModel = new OfficerDecisionViewModel
        {
            ClaimId = claim.ClaimId,
            ClaimReferenceNumber = claim.ClaimReferenceNumber,
            TotalClaimed = claim.TotalClaimedAmount,
            IndicativeEligible = claim.LatestVerificationRun?.TotalEligible ?? 0m,
            IndicativeNonEligible = claim.LatestVerificationRun?.TotalNonEligible ?? 0m,
            ApprovedAmount = claim.Decision?.ApprovedAmount ?? (claim.LatestVerificationRun?.TotalEligible ?? 0m),
            Decision = claim.Decision?.Decision ?? DecisionType.Approved,
            DecisionReason = claim.Decision?.DecisionReason ?? string.Empty
        };

        ViewBag.DecisionModel = decisionModel;
        return View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestInformation(RequestInformationViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Comment))
        {
            TempData["ErrorMessage"] = "You must provide clear details of what documents or information are needed.";
            return RedirectToAction(nameof(Review), new { id = model.ClaimId });
        }

        var officerId = _userManager.GetUserId(User)!;
        var officer = await _userManager.GetUserAsync(User);

        var result = await _workflowService.RequestInformationAsync(
            model.ClaimId,
            officerId,
            officer?.FullName ?? "Insurance Officer",
            model.Comment);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Review), new { id = model.ClaimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordDecision(OfficerDecisionViewModel model)
    {
        if ((model.Decision == DecisionType.PartiallyApproved || model.Decision == DecisionType.Rejected) && string.IsNullOrWhiteSpace(model.DecisionReason))
        {
            TempData["ErrorMessage"] = "A mandatory justification reason must be specified for Partial Approval or Rejection.";
            return RedirectToAction(nameof(Review), new { id = model.ClaimId });
        }

        if (model.Decision == DecisionType.Approved && model.ApprovedAmount <= 0)
        {
            TempData["ErrorMessage"] = "Approved amount must be greater than ₹0 for full approval.";
            return RedirectToAction(nameof(Review), new { id = model.ClaimId });
        }

        var officerId = _userManager.GetUserId(User)!;
        var officer = await _userManager.GetUserAsync(User);

        var result = await _workflowService.RecordDecisionAsync(
            model.ClaimId,
            officerId,
            officer?.FullName ?? "Insurance Officer",
            model.Decision,
            model.ApprovedAmount,
            model.DecisionReason);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Review), new { id = model.ClaimId });
    }
}
