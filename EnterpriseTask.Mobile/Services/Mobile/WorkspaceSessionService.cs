using System.Net;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Api;
using EnterpriseTask.Mobile.Services.Auth;

namespace EnterpriseTask.Mobile.Services.Mobile;

public sealed class WorkspaceSessionService(AuthService authService, MobileWorkspaceService workspaceService)
{
    public MobileCurrentUserDto? CurrentUser { get; private set; }

    public async Task<MobileCurrentUserDto> GetCurrentUserAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (CurrentUser is null || forceRefresh)
        {
            CurrentUser = await workspaceService.GetMeAsync(cancellationToken);
        }

        return CurrentUser;
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        await authService.LogoutAsync();
    }

    public async Task<bool> HandleAccessFailureAsync(Exception exception)
    {
        if (exception is not ApiException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden })
        {
            return false;
        }

        await LogoutAsync();
        await Shell.Current.DisplayAlertAsync(
            "Phiên truy cập không hợp lệ",
            exception.Message,
            "Về màn hình chính");
        await Shell.Current.GoToAsync($"//{Constants.AppConstants.StartRoute}");
        return true;
    }
}
