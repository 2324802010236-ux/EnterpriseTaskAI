using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class CompanySubscription
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxEmployees { get; set; }
    public int MaxDepartments { get; set; }
    public bool EnableAI { get; set; }
    public bool EnableRealtimeChat { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
}
