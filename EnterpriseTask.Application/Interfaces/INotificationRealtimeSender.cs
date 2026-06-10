namespace EnterpriseTask.Application.Interfaces;

public interface INotificationRealtimeSender
{
    Task SendToUserAsync(
        string userId,
        object payload,
        CancellationToken cancellationToken = default);

    Task SendUnreadCountAsync(
        string userId,
        int unreadCount,
        CancellationToken cancellationToken = default);

    Task SendToUsersAsync(
        IEnumerable<string> userIds,
        object payload,
        CancellationToken cancellationToken = default);
}
