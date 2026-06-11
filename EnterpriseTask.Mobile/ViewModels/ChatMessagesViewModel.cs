using System.Collections.ObjectModel;
using System.Windows.Input;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class ChatMessagesViewModel(
    MobileChatService chatService,
    WorkspaceSessionService sessionService) : WorkspaceViewModelBase(sessionService)
{
    private string _roomName = "Phòng chat";
    private string _messageText = string.Empty;

    public int RoomId { get; set; }
    public ObservableCollection<MobileChatMessageDto> Messages { get; } = [];
    public bool HasMessages => Messages.Count > 0;

    public string RoomName { get => _roomName; set => SetProperty(ref _roomName, value); }
    public string MessageText { get => _messageText; set => SetProperty(ref _messageText, value); }

    public ICommand RefreshCommand => new Command(async () => await LoadAsync());
    public ICommand SendCommand => new Command(async () => await SendAsync());

    public async Task LoadAsync()
    {
        if (RoomId <= 0 || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var messages = await chatService.GetMessagesAsync(RoomId);
            Messages.Clear();
            foreach (var message in messages.OrderBy(item => item.CreatedAt))
            {
                Messages.Add(message);
            }

            OnPropertyChanged(nameof(HasMessages));
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể tải tin nhắn.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
        {
            return;
        }

        try
        {
            IsBusy = true;
            await chatService.SendMessageAsync(RoomId, MessageText.Trim());
            MessageText = string.Empty;
            var messages = await chatService.GetMessagesAsync(RoomId);
            Messages.Clear();
            foreach (var message in messages.OrderBy(item => item.CreatedAt))
            {
                Messages.Add(message);
            }

            OnPropertyChanged(nameof(HasMessages));
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể gửi tin nhắn.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
