namespace EnterpriseTask.Mobile.Models.Mobile;

public sealed class MobileChatRoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? TaskId { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public string LastMessageText => string.IsNullOrWhiteSpace(LastMessage) ? "Chưa có tin nhắn" : LastMessage;
    public string LastMessageAtText => LastMessageAt?.ToLocalTime().ToString("dd/MM HH:mm") ?? string.Empty;
}

public sealed class MobileChatMessageDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
    public string CreatedAtText => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public sealed class SendChatMessageRequest
{
    public string Content { get; set; } = string.Empty;
}
