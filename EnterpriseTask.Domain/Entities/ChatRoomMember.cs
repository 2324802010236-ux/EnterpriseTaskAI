namespace EnterpriseTask.Domain.Entities;

public class ChatRoomMember
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int ChatRoomId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadAt { get; set; }

    public ChatRoom ChatRoom { get; set; } = null!;
}
