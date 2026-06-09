using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Admin.ViewModels.Companies;

public class CompanyDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public CompanyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DepartmentCount { get; set; }
    public int UserCount { get; set; }
    public int TaskCount { get; set; }
}
