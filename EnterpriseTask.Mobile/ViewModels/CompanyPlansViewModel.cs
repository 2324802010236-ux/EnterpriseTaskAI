using System.Collections.ObjectModel;
using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Onboarding;
using EnterpriseTask.Mobile.Services.Onboarding;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class CompanyPlansViewModel : ViewModelBase
{
    private readonly OnboardingService _onboardingService;
    private readonly OnboardingState _onboardingState;
    private readonly Command _retryCommand;
    private readonly Command<SubscriptionPlanPublicDto> _selectPlanCommand;
    private string _errorMessage = string.Empty;
    private bool _hasLoaded;

    public CompanyPlansViewModel(OnboardingService onboardingService, OnboardingState onboardingState)
    {
        _onboardingService = onboardingService;
        _onboardingState = onboardingState;
        _retryCommand = new Command(async () => await LoadPlansAsync(true), () => !IsBusy);
        _selectPlanCommand = new Command<SubscriptionPlanPublicDto>(
            async plan => await SelectPlanAsync(plan),
            _ => !IsBusy);
    }

    public ObservableCollection<SubscriptionPlanPublicDto> Plans { get; } = [];

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

    public bool HasPlans => Plans.Count > 0;

    public ICommand RetryCommand => _retryCommand;

    public ICommand SelectPlanCommand => _selectPlanCommand;

    public async Task LoadPlansAsync(bool forceRefresh = false)
    {
        if (IsBusy || (_hasLoaded && !forceRefresh))
        {
            return;
        }

        try
        {
            SetBusy(true);
            ErrorMessage = string.Empty;

            var plans = await _onboardingService.GetPlansAsync();
            Plans.Clear();
            foreach (var plan in plans)
            {
                Plans.Add(plan);
            }

            _hasLoaded = true;
            OnPropertyChanged(nameof(HasPlans));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SelectPlanAsync(SubscriptionPlanPublicDto? plan)
    {
        if (plan is null)
        {
            return;
        }

        _onboardingState.SelectedPlan = plan;
        _onboardingState.PurchaseResult = null;
        await Shell.Current.GoToAsync(AppConstants.CompanyRegisterRoute);
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        _retryCommand.ChangeCanExecute();
        _selectPlanCommand.ChangeCanExecute();
    }
}
