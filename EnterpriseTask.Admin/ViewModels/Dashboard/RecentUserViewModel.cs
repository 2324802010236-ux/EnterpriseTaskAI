namespace EnterpriseTask.Admin.ViewModels.Dashboard;

public class RecentUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
}
