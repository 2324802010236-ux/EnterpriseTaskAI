namespace EnterpriseTask.Mobile.Views.Onboarding;

public partial class CompanyPlansPage : ContentPage
{
    public CompanyPlansPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
