namespace EnterpriseTask.Api.DTOs.Onboarding;

public class CompanyOnboardingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public int? SubscriptionId { get; set; }
    public string? AdminEmail { get; set; }
    public string? TemporaryPassword { get; set; }
    public string? WebAdminUrl { get; set; }
}
