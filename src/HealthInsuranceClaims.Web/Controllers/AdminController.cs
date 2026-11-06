using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Application.ViewModels.Hospitals;
using HealthInsuranceClaims.Application.ViewModels.Policies;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using HealthInsuranceClaims.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IPolicyService _policyService;
    private readonly IHospitalService _hospitalService;
    private readonly IClaimService _claimService;
    private readonly IAuditService _auditService;
    private readonly IClaimAiService _claimAiService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminController(
        IDashboardService dashboardService,
        IPolicyService policyService,
        IHospitalService hospitalService,
        IClaimService claimService,
        IAuditService auditService,
        IClaimAiService claimAiService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _dashboardService = dashboardService;
        _policyService = policyService;
        _hospitalService = hospitalService;
        _claimService = claimService;
        _auditService = auditService;
        _claimAiService = claimAiService;
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var dashboard = await _dashboardService.GetAdminDashboardAsync();
        return View(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> AiRiskAnalytics()
    {
        var analytics = await _claimAiService.GetAiRiskAnalyticsAsync();
        return View(analytics);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetrainAiModel()
    {
        await _claimAiService.TrainOrRetrainModelAsync();
        TempData["SuccessMessage"] = "Anomaly & Fraud Risk model retrained successfully.";
        return RedirectToAction(nameof(AiRiskAnalytics));
    }

    #region Policy Management

    [HttpGet]
    public async Task<IActionResult> Policies()
    {
        var policies = await _policyService.GetAllPoliciesAsync();
        return View(policies);
    }

    [HttpGet]
    public IActionResult CreatePolicy()
    {
        return View(new PolicyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePolicy(PolicyFormViewModel model)
    {
        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "End date must be later than start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var policyId = await _policyService.CreatePolicyAsync(model);
        var admin = await _userManager.GetUserAsync(User);
        await _auditService.LogAsync(
            admin?.Id,
            admin?.FullName ?? "Admin",
            "PolicyCreated",
            "InsurancePolicy",
            policyId.ToString(),
            $"Created policy {model.PolicyNumber} - {model.PolicyName}");

        TempData["SuccessMessage"] = $"Policy '{model.PolicyName}' created successfully. You can now configure category coverages and exclusions.";
        return RedirectToAction(nameof(ManagePolicy), new { id = policyId });
    }

    [HttpGet]
    public async Task<IActionResult> EditPolicy(int id)
    {
        var policy = await _policyService.GetPolicyByIdAsync(id);
        if (policy == null) return NotFound();

        var model = new PolicyFormViewModel
        {
            PolicyId = policy.PolicyId,
            PolicyNumber = policy.PolicyNumber,
            PolicyName = policy.PolicyName,
            PolicyDescription = policy.PolicyDescription,
            PolicyType = policy.PolicyType,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            OverallCoverageLimit = policy.OverallCoverageLimit,
            IsActive = policy.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPolicy(PolicyFormViewModel model)
    {
        if (model.EndDate <= model.StartDate)
        {
            ModelState.AddModelError(nameof(model.EndDate), "End date must be later than start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _policyService.UpdatePolicyAsync(model);
        if (success)
        {
            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                admin?.Id,
                admin?.FullName ?? "Admin",
                "PolicyUpdated",
                "InsurancePolicy",
                model.PolicyId.ToString(),
                $"Updated policy {model.PolicyNumber}");

            TempData["SuccessMessage"] = "Policy updated successfully.";
            return RedirectToAction(nameof(Policies));
        }

        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePolicyStatus(int id)
    {
        var success = await _policyService.TogglePolicyStatusAsync(id);
        if (success)
        {
            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                admin?.Id,
                admin?.FullName ?? "Admin",
                "PolicyStatusToggled",
                "InsurancePolicy",
                id.ToString(),
                $"Toggled active status for policy {id}");

            TempData["SuccessMessage"] = "Policy status updated.";
        }
        return RedirectToAction(nameof(Policies));
    }

    [HttpGet]
    public async Task<IActionResult> ManagePolicy(int id)
    {
        var details = await _policyService.GetPolicyDetailsAsync(id);
        if (details == null) return NotFound();

        ViewBag.CoverageModel = new CoverageFormViewModel { PolicyId = id };
        ViewBag.ExclusionModel = new ExclusionFormViewModel { PolicyId = id };

        return View(details);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCoverage(CoverageFormViewModel model)
    {
        if (model.MaxCoverageAmount <= 0)
        {
            TempData["ErrorMessage"] = "Coverage amount must be greater than 0.";
            return RedirectToAction(nameof(ManagePolicy), new { id = model.PolicyId });
        }

        var success = await _policyService.AddCoverageAsync(model);
        if (success)
        {
            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                admin?.Id,
                admin?.FullName ?? "Admin",
                "CoverageAdded",
                "PolicyCoverage",
                model.PolicyId.ToString(),
                $"Added coverage cap ₹{model.MaxCoverageAmount:N2} for {model.ExpenseCategory}");

            TempData["SuccessMessage"] = "Coverage category limit added.";
        }
        else
        {
            TempData["ErrorMessage"] = "A coverage rule already exists for this expense category in this policy.";
        }

        return RedirectToAction(nameof(ManagePolicy), new { id = model.PolicyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCoverage(int coverageId, int policyId)
    {
        var success = await _policyService.DeleteCoverageAsync(coverageId);
        if (success)
        {
            TempData["SuccessMessage"] = "Coverage limit removed.";
        }
        return RedirectToAction(nameof(ManagePolicy), new { id = policyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExclusion(ExclusionFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ExclusionDescription))
        {
            TempData["ErrorMessage"] = "Exclusion description is required.";
            return RedirectToAction(nameof(ManagePolicy), new { id = model.PolicyId });
        }

        var success = await _policyService.AddExclusionAsync(model);
        if (success)
        {
            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                admin?.Id,
                admin?.FullName ?? "Admin",
                "ExclusionAdded",
                "PolicyExclusion",
                model.PolicyId.ToString(),
                $"Added exclusion for category {model.ExcludedCategory}: {model.ExclusionDescription}");

            TempData["SuccessMessage"] = "Policy exclusion rule added.";
        }
        else
        {
            TempData["ErrorMessage"] = "An exclusion rule already exists for this expense category.";
        }

        return RedirectToAction(nameof(ManagePolicy), new { id = model.PolicyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExclusion(int exclusionId, int policyId)
    {
        var success = await _policyService.DeleteExclusionAsync(exclusionId);
        if (success)
        {
            TempData["SuccessMessage"] = "Exclusion removed.";
        }
        return RedirectToAction(nameof(ManagePolicy), new { id = policyId });
    }

    #endregion

    #region Hospital Management

    [HttpGet]
    public async Task<IActionResult> Hospitals()
    {
        var hospitals = await _hospitalService.GetAllHospitalsAsync();
        return View(hospitals);
    }

    [HttpGet]
    public IActionResult CreateHospital()
    {
        return View(new HospitalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHospital(HospitalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hospitalId = await _hospitalService.CreateHospitalAsync(model);
        var admin = await _userManager.GetUserAsync(User);
        await _auditService.LogAsync(
            admin?.Id,
            admin?.FullName ?? "Admin",
            "HospitalCreated",
            "Hospital",
            hospitalId.ToString(),
            $"Created hospital {model.HospitalName}");

        TempData["SuccessMessage"] = $"Hospital '{model.HospitalName}' added to network.";
        return RedirectToAction(nameof(Hospitals));
    }

    [HttpGet]
    public async Task<IActionResult> EditHospital(int id)
    {
        var hospital = await _hospitalService.GetHospitalByIdAsync(id);
        if (hospital == null) return NotFound();

        var model = new HospitalViewModel
        {
            HospitalId = hospital.HospitalId,
            HospitalName = hospital.HospitalName,
            Address = hospital.Address,
            ContactNumber = hospital.ContactNumber,
            Email = hospital.Email,
            IsActive = hospital.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHospital(HospitalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _hospitalService.UpdateHospitalAsync(model);
        if (success)
        {
            TempData["SuccessMessage"] = "Hospital details updated.";
            return RedirectToAction(nameof(Hospitals));
        }

        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHospitalStatus(int id)
    {
        await _hospitalService.ToggleHospitalStatusAsync(id);
        TempData["SuccessMessage"] = "Hospital network status updated.";
        return RedirectToAction(nameof(Hospitals));
    }

    #endregion

    #region User Management & Policy Assignment

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = new List<(ApplicationUser User, string Role)>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            userList.Add((u, roles.FirstOrDefault() ?? "None"));
        }

        return View(userList);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            user.AccountStatus = user.AccountStatus == "Active" ? "Suspended" : "Active";
            await _userManager.UpdateAsync(user);

            var admin = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                admin?.Id,
                admin?.FullName ?? "Admin",
                "UserStatusChanged",
                "ApplicationUser",
                user.Id,
                $"Changed status of {user.Email} to {user.AccountStatus}");

            TempData["SuccessMessage"] = $"Account status for {user.FullName} updated to {user.AccountStatus}.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> AssignPolicy()
    {
        var customers = await _userManager.GetUsersInRoleAsync("Customer");
        var policies = await _policyService.GetActivePoliciesAsync();

        ViewBag.Customers = new SelectList(customers, "Id", "FullName");
        ViewBag.Policies = new SelectList(policies, "PolicyId", "PolicyName");

        var assignments = await _context.CustomerPolicies
            .Include(cp => cp.Customer)
            .Include(cp => cp.Policy)
            .OrderByDescending(cp => cp.AssignedDate)
            .ToListAsync();

        ViewBag.Assignments = assignments;

        return View(new AssignPolicyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPolicy(AssignPolicyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _userManager.GetUsersInRoleAsync("Customer");
            var policies = await _policyService.GetActivePoliciesAsync();
            ViewBag.Customers = new SelectList(customers, "Id", "FullName", model.CustomerId);
            ViewBag.Policies = new SelectList(policies, "PolicyId", "PolicyName", model.PolicyId);
            ViewBag.Assignments = await _context.CustomerPolicies
                .Include(cp => cp.Customer)
                .Include(cp => cp.Policy)
                .OrderByDescending(cp => cp.AssignedDate)
                .ToListAsync();

            return View(model);
        }

        await _policyService.AssignPolicyToCustomerAsync(model);
        var admin = await _userManager.GetUserAsync(User);
        await _auditService.LogAsync(
            admin?.Id,
            admin?.FullName ?? "Admin",
            "PolicyAssigned",
            "CustomerPolicy",
            model.PolicyId.ToString(),
            $"Assigned policy {model.PolicyId} to customer {model.CustomerId}");

        TempData["SuccessMessage"] = "Policy successfully assigned to customer.";
        return RedirectToAction(nameof(AssignPolicy));
    }

    #endregion

    #region Claims Overview & Audit Logs

    [HttpGet]
    public async Task<IActionResult> Claims(string? search = null, ClaimStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        var claims = await _claimService.GetAllClaimsAsync(search, status, fromDate, toDate);
        return View(claims);
    }

    [HttpGet]
    public async Task<IActionResult> AuditLogs(string? search = null, string? actionType = null)
    {
        ViewBag.Search = search;
        ViewBag.ActionType = actionType;

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a => a.UserName.Contains(s) || (a.Details != null && a.Details.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(a => a.Action == actionType);
        }

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
            .ToListAsync();

        return View(logs);
    }

    #endregion
}
