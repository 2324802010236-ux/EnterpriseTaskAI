using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class StartViewModel
{
    public StartViewModel()
    {
        BuyPlanCommand = new Command(async () => await Shell.Current.GoToAsync(AppConstants.PlansRoute));
        JoinCompanyCommand = new Command(async () => await Shell.Current.GoToAsync(AppConstants.LoginRoute));
    }

    public ICommand BuyPlanCommand { get; }

    public ICommand JoinCompanyCommand { get; }
}
