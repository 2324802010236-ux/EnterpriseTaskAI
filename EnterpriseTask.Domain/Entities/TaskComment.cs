namespace EnterpriseTask.Domain.Entities;

public class TaskComment
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int WorkTaskId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public WorkTask WorkTask { get; set; } = null!;
}
