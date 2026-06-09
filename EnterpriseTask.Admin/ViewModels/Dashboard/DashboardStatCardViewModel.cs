namespace EnterpriseTask.Admin.ViewModels.Dashboard;

public class DashboardStatCardViewModel
{
    public string Title { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string? Description { get; set; }
}
