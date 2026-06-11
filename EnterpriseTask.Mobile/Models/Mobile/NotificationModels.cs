namespace EnterpriseTask.Mobile.Models.Mobile;

public sealed class MobileNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public string CreatedAtText => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string ReadStateText => IsRead ? "Đã đọc" : "Chưa đọc";
}

public sealed class UnreadNotificationCountDto
{
    public int Count { get; set; }
}
