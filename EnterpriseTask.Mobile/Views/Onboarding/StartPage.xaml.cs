using EnterpriseTask.Mobile.ViewModels;

namespace EnterpriseTask.Mobile.Views.Onboarding;

public partial class StartPage : ContentPage
{
    public StartPage(StartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
