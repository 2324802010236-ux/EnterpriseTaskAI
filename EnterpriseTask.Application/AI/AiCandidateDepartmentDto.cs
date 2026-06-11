namespace EnterpriseTask.Application.AI;

public class AiCandidateDepartmentDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ActiveTaskCount { get; set; }
    public int DoneTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
}
