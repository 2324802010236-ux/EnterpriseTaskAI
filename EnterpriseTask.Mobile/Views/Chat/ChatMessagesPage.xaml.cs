using EnterpriseTask.Mobile.ViewModels;

namespace EnterpriseTask.Mobile.Views.Chat;

public partial class ChatMessagesPage : ContentPage, IQueryAttributable
{
    private readonly ChatMessagesViewModel _viewModel;

    public ChatMessagesPage(ChatMessagesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("roomId", out var roomValue)
            && int.TryParse(roomValue?.ToString(), out var roomId))
        {
            _viewModel.RoomId = roomId;
        }

        if (query.TryGetValue("roomName", out var nameValue))
        {
            _viewModel.RoomName = nameValue?.ToString() ?? "Phòng chat";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
