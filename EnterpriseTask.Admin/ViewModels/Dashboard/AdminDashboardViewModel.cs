namespace EnterpriseTask.Admin.ViewModels.Dashboard;

public class AdminDashboardViewModel
{
    public string DashboardTitle { get; set; } = string.Empty;
    public string DashboardSubtitle { get; set; } = string.Empty;
    public List<DashboardStatCardViewModel> StatCards { get; set; } = [];
    public List<RecentCompanyViewModel> RecentCompanies { get; set; } = [];
    public List<RecentUserViewModel> RecentUsers { get; set; } = [];
}
