namespace EnterpriseTask.Mobile.Models.Mobile;

public sealed class MobileDashboardDto
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

public sealed class MobileCompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class MobileDepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
