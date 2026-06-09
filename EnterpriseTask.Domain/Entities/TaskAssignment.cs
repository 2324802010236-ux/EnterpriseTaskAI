using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class TaskAssignment
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int WorkTaskId { get; set; }
    public AssignmentTargetType TargetType { get; set; }
    public string? AssignedToUserId { get; set; }
    public int? AssignedToDepartmentId { get; set; }
    public string AssignedByUserId { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public WorkTask WorkTask { get; set; } = null!;
    public Department? AssignedToDepartment { get; set; }
}
