using System.ComponentModel.DataAnnotations;

namespace HealthInsuranceClaims.Application.ViewModels.Hospitals;

public class HospitalViewModel
{
    public int HospitalId { get; set; }

    [Required(ErrorMessage = "Hospital Name is required")]
    [StringLength(200, ErrorMessage = "Hospital name cannot exceed 200 characters")]
    [Display(Name = "Hospital Name")]
    public string HospitalName { get; set; } = string.Empty;

    [Display(Name = "Hospital Address")]
    public string? Address { get; set; }

    [Display(Name = "Contact Number")]
    public string? ContactNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Official Email")]
    public string? Email { get; set; }

    [Display(Name = "Is Active Network Hospital")]
    public bool IsActive { get; set; } = true;

    public int TotalClaimsTreated { get; set; }
}
