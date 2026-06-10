namespace EnterpriseTask.Api.DTOs.Mobile;

public class MobileDashboardDto
{
    public string Role { get; set; } = string.Empty;
    public int MyTaskCount { get; set; }
    public int InProgressTaskCount { get; set; }
    public int DoneTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int NotificationCount { get; set; }
    public int DepartmentMemberCount { get; set; }
    public string WelcomeMessage { get; set; } = string.Empty;
}
