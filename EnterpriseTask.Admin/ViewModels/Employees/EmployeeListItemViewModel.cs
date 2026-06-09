namespace EnterpriseTask.Admin.ViewModels.Employees;

public class EmployeeListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? DepartmentName { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
