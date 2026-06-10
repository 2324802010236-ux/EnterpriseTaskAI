namespace EnterpriseTask.Application.Chat;

public class ChatMessageInfo
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
