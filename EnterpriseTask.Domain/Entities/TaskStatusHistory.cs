using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class TaskStatusHistory
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int WorkTaskId { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public WorkTaskStatus? FromStatus { get; set; }
    public WorkTaskStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }

    public WorkTask WorkTask { get; set; } = null!;
}
