using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class PaymentTransaction
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int? CompanySubscriptionId { get; set; }
    public CompanySubscription? CompanySubscription { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? TransactionCode { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
