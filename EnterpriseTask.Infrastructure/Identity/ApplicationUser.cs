using Microsoft.AspNetCore.Identity;

namespace EnterpriseTask.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
