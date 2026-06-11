using EnterpriseTask.Mobile.ViewModels;

namespace EnterpriseTask.Mobile.Views.Onboarding;

public partial class CompanyPurchaseResultPage : ContentPage
{
    private readonly CompanyPurchaseResultViewModel _viewModel;

    public CompanyPurchaseResultPage(CompanyPurchaseResultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshResult();

        if (!_viewModel.HasResult)
        {
            await Shell.Current.GoToAsync($"//{Constants.AppConstants.StartRoute}");
        }
    }
}
