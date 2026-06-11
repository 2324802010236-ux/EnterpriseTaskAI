using System.Net.Mail;
using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Onboarding;
using EnterpriseTask.Mobile.Services.Api;
using EnterpriseTask.Mobile.Services.Onboarding;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class CompanyRegisterViewModel : ViewModelBase
{
    private readonly OnboardingService _onboardingService;
    private readonly OnboardingState _onboardingState;
    private readonly Command _purchaseCommand;
    private string _companyName = string.Empty;
    private string _taxCode = string.Empty;
    private string _companyEmail = string.Empty;
    private string _companyPhone = string.Empty;
    private string _companyAddress = string.Empty;
    private string _industry = string.Empty;
    private string _adminFullName = string.Empty;
    private string _adminEmail = string.Empty;
    private string _adminPhone = string.Empty;
    private string _errorMessage = string.Empty;

    public CompanyRegisterViewModel(OnboardingService onboardingService, OnboardingState onboardingState)
    {
        _onboardingService = onboardingService;
        _onboardingState = onboardingState;
        _purchaseCommand = new Command(async () => await PurchaseAsync(), () => !IsBusy);
    }

    public SubscriptionPlanPublicDto? SelectedPlan => _onboardingState.SelectedPlan;

    public bool HasSelectedPlan => SelectedPlan is not null;

    public string CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }
    public string TaxCode { get => _taxCode; set => SetProperty(ref _taxCode, value); }
    public string CompanyEmail { get => _companyEmail; set => SetProperty(ref _companyEmail, value); }
    public string CompanyPhone { get => _companyPhone; set => SetProperty(ref _companyPhone, value); }
    public string CompanyAddress { get => _companyAddress; set => SetProperty(ref _companyAddress, value); }
    public string Industry { get => _industry; set => SetProperty(ref _industry, value); }
    public string AdminFullName { get => _adminFullName; set => SetProperty(ref _adminFullName, value); }
    public string AdminEmail { get => _adminEmail; set => SetProperty(ref _adminEmail, value); }
    public string AdminPhone { get => _adminPhone; set => SetProperty(ref _adminPhone, value); }

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

    public ICommand PurchaseCommand => _purchaseCommand;

    public void RefreshSelectedPlan()
    {
        OnPropertyChanged(nameof(SelectedPlan));
        OnPropertyChanged(nameof(HasSelectedPlan));
    }

    private async Task PurchaseAsync()
    {
        var validationMessage = Validate();
        if (validationMessage is not null)
        {
            await ShowErrorAsync(validationMessage);
            return;
        }

        try
        {
            SetBusy(true);
            ErrorMessage = string.Empty;

            var result = await _onboardingService.PurchaseAsync(new CompanyOnboardingRequest
            {
                SubscriptionPlanId = SelectedPlan!.Id,
                CompanyName = CompanyName.Trim(),
                TaxCode = NormalizeOptional(TaxCode),
                CompanyEmail = CompanyEmail.Trim(),
                CompanyPhone = NormalizeOptional(CompanyPhone),
                CompanyAddress = NormalizeOptional(CompanyAddress),
                Industry = NormalizeOptional(Industry),
                AdminFullName = AdminFullName.Trim(),
                AdminEmail = AdminEmail.Trim(),
                AdminPhone = NormalizeOptional(AdminPhone)
            });

            _onboardingState.PurchaseResult = result;
            await Shell.Current.GoToAsync(AppConstants.PurchaseResultRoute);
        }
        catch (ApiException ex)
        {
            await ShowErrorAsync(ex.Message);
        }
        catch (HttpRequestException)
        {
            await ShowErrorAsync("Không thể kết nối API. Vui lòng kiểm tra máy chủ.");
        }
        catch (Exception)
        {
            await ShowErrorAsync("Không thể hoàn tất đăng ký. Vui lòng thử lại.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private string? Validate()
    {
        if (SelectedPlan is null)
        {
            return "Vui lòng quay lại và chọn một gói dịch vụ.";
        }

        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            return "Tên công ty không được để trống.";
        }

        if (!IsValidEmail(CompanyEmail))
        {
            return "Email công ty không hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(AdminFullName))
        {
            return "Họ tên người quản trị không được để trống.";
        }

        return IsValidEmail(AdminEmail) ? null : "Email người quản trị không hợp lệ.";
    }

    private async Task ShowErrorAsync(string message)
    {
        ErrorMessage = message;
        await Shell.Current.DisplayAlertAsync("Không thể mua gói", message, "Đóng");
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        _purchaseCommand.ChangeCanExecute();
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(value) && new MailAddress(value.Trim()).Address == value.Trim();
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? NormalizeOptional(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
