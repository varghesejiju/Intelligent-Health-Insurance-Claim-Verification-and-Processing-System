using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HealthInsuranceClaims.Web.Models;

namespace HealthInsuranceClaims.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Administrator"))
                return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("InsuranceOfficer"))
                return RedirectToAction("Dashboard", "Officer");
            if (User.IsInRole("Customer"))
                return RedirectToAction("Dashboard", "Customer");
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
