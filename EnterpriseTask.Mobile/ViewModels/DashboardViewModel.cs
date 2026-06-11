using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Services.Auth;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly Command _logoutCommand;

    public DashboardViewModel(AuthService authService)
    {
        _authService = authService;
        _logoutCommand = new Command(async () => await LogoutAsync(), () => !IsBusy);
    }

    public ICommand LogoutCommand => _logoutCommand;

    private async Task LogoutAsync()
    {
        try
        {
            IsBusy = true;
            _logoutCommand.ChangeCanExecute();
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync($"//{AppConstants.StartRoute}");
        }
        finally
        {
            IsBusy = false;
            _logoutCommand.ChangeCanExecute();
        }
    }
}
