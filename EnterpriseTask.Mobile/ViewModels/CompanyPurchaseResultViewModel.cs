using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Onboarding;
using EnterpriseTask.Mobile.Services.Onboarding;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class CompanyPurchaseResultViewModel : ViewModelBase
{
    private readonly OnboardingState _onboardingState;

    public CompanyPurchaseResultViewModel(OnboardingState onboardingState)
    {
        _onboardingState = onboardingState;
        GoHomeCommand = new Command(async () => await GoHomeAsync());
        GoToLoginCommand = new Command(async () => await Shell.Current.GoToAsync(AppConstants.LoginRoute));
    }

    public CompanyOnboardingResponse? Result => _onboardingState.PurchaseResult;

    public bool HasResult => Result is not null;

    public bool HasTemporaryPassword => !string.IsNullOrWhiteSpace(Result?.TemporaryPassword);

    public ICommand GoHomeCommand { get; }

    public ICommand GoToLoginCommand { get; }

    public void RefreshResult()
    {
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(HasResult));
        OnPropertyChanged(nameof(HasTemporaryPassword));
    }

    private async Task GoHomeAsync()
    {
        _onboardingState.Reset();
        await Shell.Current.GoToAsync($"//{AppConstants.StartRoute}");
    }
}
