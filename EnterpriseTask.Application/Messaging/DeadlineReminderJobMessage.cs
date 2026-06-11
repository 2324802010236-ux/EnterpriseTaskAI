namespace EnterpriseTask.Application.Messaging;

public class DeadlineReminderJobMessage
{
    public int CompanyId { get; set; }
    public int TaskId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TaskTitle { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
