using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public int? RelatedTaskId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
