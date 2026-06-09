namespace EnterpriseTask.Admin.ViewModels.SubscriptionPlans;

public class SubscriptionPlanListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int MaxEmployees { get; set; }
    public int MaxDepartments { get; set; }
    public bool EnableAI { get; set; }
    public bool EnableRealtimeChat { get; set; }
    public bool IsActive { get; set; }
}
