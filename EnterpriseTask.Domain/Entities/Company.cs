using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public CompanyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<EmployeeProfile> EmployeeProfiles { get; set; } = [];
    public ICollection<WorkTask> WorkTasks { get; set; } = [];
    public ICollection<CompanySubscription> CompanySubscriptions { get; set; } = [];
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
}
