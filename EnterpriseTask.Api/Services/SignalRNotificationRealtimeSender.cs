using EnterpriseTask.Api.Hubs;
using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseTask.Api.Services;

public class SignalRNotificationRealtimeSender(
    IHubContext<NotificationHub> hubContext,
    NotificationRealtimeDeliveryTracker deliveryTracker) : INotificationRealtimeSender
{
    public async Task SendToUserAsync(
        string userId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (payload is NotificationRealtimePayload notification
            && !deliveryTracker.TryMarkDelivered(notification.Id))
        {
            return;
        }

        await hubContext.Clients
            .Group(NotificationHub.UserGroup(userId))
            .SendAsync("notification.created", payload, cancellationToken);
    }

    public Task SendUnreadCountAsync(
        string userId,
        int unreadCount,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(NotificationHub.UserGroup(userId))
            .SendAsync(
                "notification.unreadCountChanged",
                new { count = unreadCount },
                cancellationToken);

    public Task SendToUsersAsync(
        IEnumerable<string> userIds,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var groups = userIds
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.Ordinal)
            .Select(NotificationHub.UserGroup)
            .ToList();

        return groups.Count == 0
            ? Task.CompletedTask
            : hubContext.Clients
                .Groups(groups)
                .SendAsync("notification.created", payload, cancellationToken);
    }
}
