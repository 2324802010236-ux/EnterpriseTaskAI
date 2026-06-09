namespace EnterpriseTask.Domain.Entities;

public class Department
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FunctionDescription { get; set; }
    public string? ManagerUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<EmployeeProfile> EmployeeProfiles { get; set; } = [];
    public ICollection<WorkTask> WorkTasks { get; set; } = [];
}
