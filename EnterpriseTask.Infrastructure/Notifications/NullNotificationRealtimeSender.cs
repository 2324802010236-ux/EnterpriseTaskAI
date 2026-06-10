using EnterpriseTask.Application.Interfaces;

namespace EnterpriseTask.Infrastructure.Notifications;

public class NullNotificationRealtimeSender : INotificationRealtimeSender
{
    public Task SendToUserAsync(
        string userId,
        object payload,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendUnreadCountAsync(
        string userId,
        int unreadCount,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendToUsersAsync(
        IEnumerable<string> userIds,
        object payload,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
