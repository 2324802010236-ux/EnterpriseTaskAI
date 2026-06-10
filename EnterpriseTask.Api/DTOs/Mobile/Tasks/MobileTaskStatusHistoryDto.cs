namespace EnterpriseTask.Api.DTOs.Mobile.Tasks;

public class MobileTaskStatusHistoryDto
{
    public int Id { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
