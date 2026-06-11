using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class ProfileViewModel(WorkspaceSessionService sessionService) : WorkspaceViewModelBase(sessionService)
{
    private MobileCurrentUserDto? _currentUser;

    public MobileCurrentUserDto? CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }

    public ICommand RefreshCommand => new Command(async () => await LoadAsync());
    public ICommand LogoutCommand => new Command(async () => await LogoutAsync());

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            CurrentUser = await SessionService.GetCurrentUserAsync(true);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể tải hồ sơ.");
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
