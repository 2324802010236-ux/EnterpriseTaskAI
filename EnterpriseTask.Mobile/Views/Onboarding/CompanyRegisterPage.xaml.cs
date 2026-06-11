using EnterpriseTask.Mobile.ViewModels;

namespace EnterpriseTask.Mobile.Views.Onboarding;

public partial class CompanyRegisterPage : ContentPage
{
    private readonly CompanyRegisterViewModel _viewModel;

    public CompanyRegisterPage(CompanyRegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshSelectedPlan();

        if (!_viewModel.HasSelectedPlan)
        {
            await DisplayAlertAsync("Chưa chọn gói", "Vui lòng chọn một gói dịch vụ trước.", "Quay lại");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
