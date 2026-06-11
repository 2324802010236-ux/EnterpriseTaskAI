namespace EnterpriseTask.Mobile.Models.Onboarding;

public sealed class CompanyOnboardingRequest
{
    public int SubscriptionPlanId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyPhone { get; set; }
    public string? CompanyAddress { get; set; }
    public string? Industry { get; set; }
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string? AdminPhone { get; set; }
}
