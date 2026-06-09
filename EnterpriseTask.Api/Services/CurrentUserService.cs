using System.Security.Claims;
using EnterpriseTask.Application.Interfaces;

namespace EnterpriseTask.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public string? UserId => GetClaim(ClaimTypes.NameIdentifier) ?? GetClaim("sub");
    public string? Email => GetClaim(ClaimTypes.Email);
    public string? Role => GetClaim(ClaimTypes.Role);
    public int? CompanyId => ParseNullableIntClaim("companyId");
    public int? DepartmentId => ParseNullableIntClaim("departmentId");

    private string? GetClaim(string claimType) => User?.FindFirst(claimType)?.Value;

    private int? ParseNullableIntClaim(string claimType) =>
        int.TryParse(GetClaim(claimType), out var value) ? value : null;
}
