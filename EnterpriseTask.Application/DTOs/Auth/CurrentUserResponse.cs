namespace EnterpriseTask.Application.DTOs.Auth;

public class CurrentUserResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public string? CompanyName { get; set; }
    public string? DepartmentName { get; set; }
}
