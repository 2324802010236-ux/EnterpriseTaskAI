using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Api.DTOs.Onboarding;

public class CompanyOnboardingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Subscription plan is required.")]
    public int SubscriptionPlanId { get; set; }

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [Required(ErrorMessage = "Company email is required.")]
    [EmailAddress(ErrorMessage = "Company email is invalid.")]
    [StringLength(256)]
    public string CompanyEmail { get; set; } = string.Empty;

    [StringLength(30)]
    public string? CompanyPhone { get; set; }

    [StringLength(500)]
    public string? CompanyAddress { get; set; }

    [StringLength(150)]
    public string? Industry { get; set; }

    [Required(ErrorMessage = "Admin full name is required.")]
    [StringLength(150)]
    public string AdminFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin email is required.")]
    [EmailAddress(ErrorMessage = "Admin email is invalid.")]
    [StringLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    [StringLength(30)]
    public string? AdminPhone { get; set; }
}
