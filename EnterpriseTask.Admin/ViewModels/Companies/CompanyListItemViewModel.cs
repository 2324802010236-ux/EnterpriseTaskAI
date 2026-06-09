namespace EnterpriseTask.Admin.ViewModels.Companies;

public class CompanyListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Industry { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DepartmentCount { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
