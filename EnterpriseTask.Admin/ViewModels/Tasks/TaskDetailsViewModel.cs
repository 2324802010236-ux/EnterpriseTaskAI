namespace EnterpriseTask.Admin.ViewModels.Tasks;

public class TaskDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? AssignedToName { get; set; }
    public string? DepartmentName { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TaskStatusHistoryItemViewModel> StatusHistories { get; set; } = [];
    public List<TaskCommentItemViewModel> Comments { get; set; } = [];
}
