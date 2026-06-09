using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class ChatRoom
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChatRoomType Type { get; set; }
    public int? DepartmentId { get; set; }
    public int? WorkTaskId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Department? Department { get; set; }
    public WorkTask? WorkTask { get; set; }
    public ICollection<ChatRoomMember> Members { get; set; } = [];
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
