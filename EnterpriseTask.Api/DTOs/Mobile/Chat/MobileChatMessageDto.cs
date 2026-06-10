namespace EnterpriseTask.Api.DTOs.Mobile.Chat;

public class MobileChatMessageDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
}
