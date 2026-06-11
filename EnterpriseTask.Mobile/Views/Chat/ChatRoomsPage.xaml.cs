using EnterpriseTask.Mobile.ViewModels;

namespace EnterpriseTask.Mobile.Views.Chat;

public partial class ChatRoomsPage : ContentPage
{
    private readonly ChatRoomsViewModel _viewModel;

    public ChatRoomsPage(ChatRoomsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
