using System.Collections.ObjectModel;
using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class ChatRoomsViewModel(
    MobileChatService chatService,
    WorkspaceSessionService sessionService) : WorkspaceViewModelBase(sessionService)
{
    public ObservableCollection<MobileChatRoomDto> Rooms { get; } = [];
    public bool HasRooms => Rooms.Count > 0;
    public ICommand RefreshCommand => new Command(async () => await LoadAsync());
    public ICommand OpenRoomCommand => new Command<MobileChatRoomDto>(async room =>
    {
        if (room is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(
            AppConstants.ChatMessagesRoute,
            new ShellNavigationQueryParameters
            {
                ["roomId"] = room.Id,
                ["roomName"] = room.Name
            });
    });

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var rooms = await chatService.GetRoomsAsync();
            Rooms.Clear();
            foreach (var room in rooms)
            {
                Rooms.Add(room);
            }

            OnPropertyChanged(nameof(HasRooms));
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể tải phòng chat.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
