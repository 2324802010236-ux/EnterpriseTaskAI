using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Admin.ViewModels.CompanyProfile;

public class CompanyProfileViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public CompanyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DepartmentCount { get; set; }
    public int EmployeeCount { get; set; }
    public int TaskCount { get; set; }
}
