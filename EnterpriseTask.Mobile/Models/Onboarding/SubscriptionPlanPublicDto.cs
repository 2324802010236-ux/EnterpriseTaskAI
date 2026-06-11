using System.Globalization;

namespace EnterpriseTask.Mobile.Models.Onboarding;

public sealed class SubscriptionPlanPublicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int MaxEmployees { get; set; }
    public int MaxDepartments { get; set; }
    public bool EnableAI { get; set; }
    public bool EnableRealtimeChat { get; set; }

    public string PriceText => $"{Price.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} đ";
    public string AiText => EnableAI ? "Có AI hỗ trợ" : "Không gồm AI";
    public string RealtimeChatText => EnableRealtimeChat ? "Có chat realtime" : "Không gồm chat realtime";
}
