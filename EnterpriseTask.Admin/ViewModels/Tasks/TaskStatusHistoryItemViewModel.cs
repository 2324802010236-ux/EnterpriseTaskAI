namespace EnterpriseTask.Admin.ViewModels.Tasks;

public class TaskStatusHistoryItemViewModel
{
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
