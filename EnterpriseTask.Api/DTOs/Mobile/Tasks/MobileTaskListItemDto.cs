namespace EnterpriseTask.Api.DTOs.Mobile.Tasks;

public class MobileTaskListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string AssignmentTarget { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue { get; set; }
}
