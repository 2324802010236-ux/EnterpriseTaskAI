using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Admin.Services;

public class CompanyPortalAccessService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager)
{
    public async Task<CompanyPortalAccessResult> CheckAccessAsync(
        ApplicationUser? user,
        CancellationToken cancellationToken = default)
    {
        if (user is null || !user.IsActive)
        {
            return CompanyPortalAccessResult.Unauthenticated;
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.CompanyAdmin))
        {
            return CompanyPortalAccessResult.AccessDenied;
        }

        if (!user.CompanyId.HasValue)
        {
            return CompanyPortalAccessResult.AccessDenied;
        }

        var company = await context.Companies
            .FirstOrDefaultAsync(item => item.Id == user.CompanyId.Value, cancellationToken);
        if (company is null)
        {
            return CompanyPortalAccessResult.AccessDenied;
        }

        if (company.Status == CompanyStatus.Suspended)
        {
            return CompanyPortalAccessResult.Suspended;
        }

        if (company.Status == CompanyStatus.Expired)
        {
            return CompanyPortalAccessResult.SubscriptionExpired;
        }

        if (company.Status != CompanyStatus.Active)
        {
            return CompanyPortalAccessResult.SubscriptionRequired;
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
            return CompanyPortalAccessResult.SubscriptionRequired;
        }

        if (subscription.EndDate < DateTime.UtcNow.Date)
        {
            company.Status = CompanyStatus.Expired;
            company.UpdatedAt = DateTime.UtcNow;
            subscription.Status = SubscriptionStatus.Expired;
            subscription.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            return CompanyPortalAccessResult.SubscriptionExpired;
        }

        return CompanyPortalAccessResult.Allowed;
    }

    public async Task<CompanyPortalAccessResult> CheckCurrentUserAccessAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return CompanyPortalAccessResult.Unauthenticated;
        }

        var user = await userManager.GetUserAsync(principal);
        return await CheckAccessAsync(user, cancellationToken);
    }

    public static string GetRedirectPath(CompanyPortalAccessResult result) =>
        result switch
        {
            CompanyPortalAccessResult.Allowed => "/company/dashboard",
            CompanyPortalAccessResult.Suspended => "/company/suspended",
            CompanyPortalAccessResult.SubscriptionExpired => "/company/subscription-expired",
            CompanyPortalAccessResult.SubscriptionRequired => "/company/subscription-required",
            _ => "/Account/AccessDenied"
        };
}
