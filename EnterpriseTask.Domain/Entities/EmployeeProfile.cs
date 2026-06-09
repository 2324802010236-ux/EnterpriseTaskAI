namespace EnterpriseTask.Domain.Entities;

public class EmployeeProfile
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? EmployeeCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public string? Skills { get; set; }
    public string? CapacityNote { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Department? Department { get; set; }
}
