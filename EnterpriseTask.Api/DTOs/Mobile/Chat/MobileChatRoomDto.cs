namespace EnterpriseTask.Api.DTOs.Mobile.Chat;

public class MobileChatRoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? TaskId { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
