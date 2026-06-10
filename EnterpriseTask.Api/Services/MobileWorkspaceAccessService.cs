using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Api.Services;

public class MobileWorkspaceAccessService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService)
{
    private static readonly string[] MobileRoles =
    [
        AppRoles.CompanyAdmin,
        AppRoles.Director,
        AppRoles.DepartmentManager,
        AppRoles.Employee
    ];

    public async Task<MobileWorkspaceAccessResult> CheckAccessAsync(
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.IsAuthenticated || string.IsNullOrWhiteSpace(currentUserService.UserId))
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status401Unauthorized,
                "Không thể xác định tài khoản hiện tại.");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(item => item.Id == currentUserService.UserId, cancellationToken);
        if (user is null)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status401Unauthorized,
                "Không thể xác định tài khoản hiện tại.");
        }

        if (!user.IsActive)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Tài khoản đã bị khóa.");
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.SystemAdmin))
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Bạn không có quyền sử dụng mobile workspace.");
        }

        var role = MobileRoles.FirstOrDefault(roles.Contains);
        if (role is null)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Bạn không có quyền sử dụng mobile workspace.");
        }

        if (!user.CompanyId.HasValue)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Tài khoản không thuộc công ty nào.");
        }

        var company = await context.Companies
            .FirstOrDefaultAsync(item => item.Id == user.CompanyId.Value, cancellationToken);
        if (company is null)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Tài khoản không thuộc công ty nào.");
        }

        if (company.Status == CompanyStatus.Suspended)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Công ty đang bị tạm khóa.");
        }

        if (company.Status == CompanyStatus.Expired)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Gói dịch vụ đã hết hạn.");
        }

        if (company.Status != CompanyStatus.Active)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Công ty chưa được kích hoạt.");
        }

        var subscription = await context.CompanySubscriptions
            .Where(item =>
                item.CompanyId == company.Id
                && item.Status == SubscriptionStatus.Active)
            .OrderByDescending(item => item.StartDate)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (subscription is null)
        {
            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Công ty chưa có gói dịch vụ đang hoạt động.");
        }

        if (subscription.EndDate < DateTime.UtcNow.Date)
        {
            company.Status = CompanyStatus.Expired;
            company.UpdatedAt = DateTime.UtcNow;
            subscription.Status = SubscriptionStatus.Expired;
            subscription.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            return MobileWorkspaceAccessResult.Denied(
                StatusCodes.Status403Forbidden,
                "Gói dịch vụ đã hết hạn.");
        }

        var department = user.DepartmentId.HasValue
            ? await context.Departments.AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == user.DepartmentId.Value
                        && item.CompanyId == company.Id,
                    cancellationToken)
            : null;

        return MobileWorkspaceAccessResult.Allowed(
            new MobileWorkspaceContext(user, company, department, subscription, role));
    }
}

public record MobileWorkspaceContext(
    ApplicationUser User,
    Company Company,
    Department? Department,
    CompanySubscription Subscription,
    string Role);

public class MobileWorkspaceAccessResult
{
    private MobileWorkspaceAccessResult(
        MobileWorkspaceContext? workspace,
        int statusCode,
        string message)
    {
        Workspace = workspace;
        StatusCode = statusCode;
        Message = message;
    }

    public bool IsAllowed => Workspace is not null;
    public MobileWorkspaceContext? Workspace { get; }
    public int StatusCode { get; }
    public string Message { get; }

    public static MobileWorkspaceAccessResult Allowed(MobileWorkspaceContext workspace) =>
        new(workspace, StatusCodes.Status200OK, string.Empty);

    public static MobileWorkspaceAccessResult Denied(int statusCode, string message) =>
        new(null, statusCode, message);
}
