using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Web.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DocumentsController(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var document = await _context.ClaimDocuments
            .Include(d => d.Claim)
            .FirstOrDefaultAsync(d => d.DocumentId == id);

        if (document == null || document.Claim == null)
        {
            return NotFound("Requested document record not found.");
        }

        var currentUserId = _userManager.GetUserId(User);
        bool isStaff = User.IsInRole("Administrator") || User.IsInRole("InsuranceOfficer");
        bool isOwner = document.Claim.CustomerId == currentUserId;

        // Resource authorization check
        if (!isStaff && !isOwner)
        {
            return Forbid();
        }

        var fileResult = await _fileStorageService.GetFileAsync(document.StoredFileName, document.OriginalFileName, document.ContentType);
        if (fileResult == null)
        {
            return NotFound("File not found on secure storage.");
        }

        return File(fileResult.Value.Bytes, fileResult.Value.ContentType, fileResult.Value.DownloadName);
    }
}
