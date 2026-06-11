namespace EnterpriseTask.Application.AI;

public class AiCandidateUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? Position { get; set; }
    public int ActiveTaskCount { get; set; }
    public int DoneTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
}
