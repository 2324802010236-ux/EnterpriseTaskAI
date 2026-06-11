using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class DashboardViewModel(
    MobileWorkspaceService workspaceService,
    WorkspaceSessionService sessionService) : WorkspaceViewModelBase(sessionService)
{
    private MobileCurrentUserDto? _currentUser;
    private MobileDashboardDto? _dashboard;
    private readonly Command _refreshCommand = new(() => { });

    public MobileCurrentUserDto? CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }

    public MobileDashboardDto? Dashboard
    {
        get => _dashboard;
        private set => SetProperty(ref _dashboard, value);
    }

    public ICommand RefreshCommand => new Command(async () => await LoadAsync(true));
    public ICommand OpenTasksCommand => new Command(async () => await Shell.Current.GoToAsync($"//{AppConstants.TasksRoute}"));
    public ICommand OpenNotificationsCommand => new Command(async () => await Shell.Current.GoToAsync($"//{AppConstants.NotificationsRoute}"));
    public ICommand OpenChatCommand => new Command(async () => await Shell.Current.GoToAsync($"//{AppConstants.ChatRoute}"));
    public ICommand LogoutCommand => new Command(async () => await LogoutAsync());

    public async Task LoadAsync(bool forceRefresh = false)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            CurrentUser = await SessionService.GetCurrentUserAsync(forceRefresh);
            Dashboard = await workspaceService.GetDashboardAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể tải dashboard.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LogoutAsync()
    {
        await SessionService.LogoutAsync();
        await Shell.Current.GoToAsync($"//{AppConstants.StartRoute}");
    }
}
