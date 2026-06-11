using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Services.Api;
using EnterpriseTask.Mobile.Services.Auth;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly Command _loginCommand;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        _loginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ICommand LoginCommand => _loginCommand;

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập email và mật khẩu.";
            return;
        }

        try
        {
            SetBusy(true);
            ErrorMessage = string.Empty;

            await _authService.LoginAsync(Email, Password);
            Password = string.Empty;
            await Shell.Current.GoToAsync($"//{AppConstants.DashboardRoute}");
        }
        catch (ApiException ex)
        {
            await ShowLoginErrorAsync(ex.Message);
        }
        catch (HttpRequestException)
        {
            await ShowLoginErrorAsync("Không thể kết nối API. Vui lòng kiểm tra địa chỉ máy chủ.");
        }
        catch (Exception)
        {
            await ShowLoginErrorAsync("Đăng nhập thất bại. Vui lòng thử lại.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ShowLoginErrorAsync(string message)
    {
        ErrorMessage = message;
        await Shell.Current.DisplayAlertAsync("Không thể đăng nhập", message, "Đóng");
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        _loginCommand.ChangeCanExecute();
    }
}
