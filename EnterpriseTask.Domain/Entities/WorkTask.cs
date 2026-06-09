using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class WorkTask
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AiSummary { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public int? AssignedDepartmentId { get; set; }
    public WorkTaskStatus Status { get; set; }
    public WorkTaskPriority Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Department? AssignedDepartment { get; set; }
    public ICollection<TaskAssignment> Assignments { get; set; } = [];
    public ICollection<TaskComment> Comments { get; set; } = [];
    public ICollection<TaskStatusHistory> StatusHistories { get; set; } = [];
}
