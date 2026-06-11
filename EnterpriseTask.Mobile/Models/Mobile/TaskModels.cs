namespace EnterpriseTask.Mobile.Models.Mobile;

public sealed class MobileTaskListItemDto
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

    public string DueDateText => DueDate.HasValue ? DueDate.Value.ToLocalTime().ToString("dd/MM/yyyy") : "Không có hạn";
    public string AssignedToText => AssignedUserName ?? DepartmentName ?? "Chưa phân công";
}

public sealed class MobileTaskDetailsDto
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
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MobileTaskCommentDto> Comments { get; set; } = [];
    public List<MobileTaskStatusHistoryDto> StatusHistories { get; set; } = [];

    public string DueDateText => DueDate.HasValue ? DueDate.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Không có hạn";
    public string AssignedToText => AssignedUserName ?? DepartmentName ?? "Chưa phân công";
}

public sealed class MobileTaskCommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedAtText => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public sealed class MobileTaskStatusHistoryDto
{
    public int Id { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedByName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedAtText => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public sealed class UpdateMobileTaskStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class CreateMobileTaskCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
