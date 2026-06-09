using EnterpriseTask.Admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EnterpriseTask.Admin.Filters;

public class CompanyPortalAccessFilter(CompanyPortalAccessService accessService)
    : IAsyncAuthorizationFilter
{
    private static readonly PathString[] ExcludedPaths =
    [
        new("/company/subscription-required"),
        new("/company/subscription-expired"),
        new("/company/suspended")
    ];

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var path = context.HttpContext.Request.Path;
        if (!path.StartsWithSegments("/company")
            || ExcludedPaths.Any(excluded => path.StartsWithSegments(excluded)))
        {
            return;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var result = await accessService.CheckCurrentUserAccessAsync(
            context.HttpContext.User,
            context.HttpContext.RequestAborted);

        if (result != CompanyPortalAccessResult.Allowed)
        {
            context.Result = new RedirectResult(CompanyPortalAccessService.GetRedirectPath(result));
        }
    }
}
