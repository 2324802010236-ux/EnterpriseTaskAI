using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Application.DTOs.Auth;

public class RegisterCompanyRequest
{
    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string CompanyEmail { get; set; } = string.Empty;

    [StringLength(30)]
    public string? CompanyPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(150)]
    public string? Industry { get; set; }

    [Required]
    [StringLength(150)]
    public string AdminFullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    public string AdminPassword { get; set; } = string.Empty;

    [StringLength(30)]
    public string? AdminPhone { get; set; }
}
