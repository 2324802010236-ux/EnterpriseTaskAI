namespace EnterpriseTask.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int ChatRoomId { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public ChatRoom ChatRoom { get; set; } = null!;
}
