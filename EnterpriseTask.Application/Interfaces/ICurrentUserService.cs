namespace EnterpriseTask.Application.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    int? CompanyId { get; }
    int? DepartmentId { get; }
}
