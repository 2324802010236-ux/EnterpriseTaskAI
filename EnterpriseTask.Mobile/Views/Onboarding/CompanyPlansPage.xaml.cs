using EnterpriseTask.Mobile.ViewModels;

namespace EnterpriseTask.Mobile.Views.Onboarding;

public partial class CompanyPlansPage : ContentPage
{
    private readonly CompanyPlansViewModel _viewModel;

    public CompanyPlansPage(CompanyPlansViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPlansAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
