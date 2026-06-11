using System.Collections.ObjectModel;
using System.Windows.Input;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class NotificationsViewModel(
    MobileNotificationService notificationService,
    WorkspaceSessionService sessionService) : WorkspaceViewModelBase(sessionService)
{
    private int _unreadCount;

    public ObservableCollection<MobileNotificationDto> Notifications { get; } = [];
    public bool HasNotifications => Notifications.Count > 0;

    public int UnreadCount
    {
        get => _unreadCount;
        private set => SetProperty(ref _unreadCount, value);
    }

    public ICommand RefreshCommand => new Command(async () => await LoadAsync());
    public ICommand ReadAllCommand => new Command(async () => await ReadAllAsync());
    public ICommand MarkReadCommand => new Command<MobileNotificationDto>(async item => await MarkReadAsync(item));

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
            var notifications = await notificationService.GetNotificationsAsync();
            UnreadCount = await notificationService.GetUnreadCountAsync();
            Notifications.Clear();
            foreach (var item in notifications)
            {
                Notifications.Add(item);
            }

            OnPropertyChanged(nameof(HasNotifications));
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể tải thông báo.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task MarkReadAsync(MobileNotificationDto? item)
    {
        if (item is null || item.IsRead)
        {
            return;
        }

        try
        {
            await notificationService.MarkReadAsync(item.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể đánh dấu thông báo.");
        }
    }

    private async Task ReadAllAsync()
    {
        try
        {
            await notificationService.ReadAllAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể đánh dấu tất cả thông báo.");
        }
    }
}
