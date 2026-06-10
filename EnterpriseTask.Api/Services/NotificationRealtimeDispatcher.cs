using EnterpriseTask.Api.Hubs;
using EnterpriseTask.Application.Notifications;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Api.Services;

public class NotificationRealtimeDispatcher(
    IServiceScopeFactory scopeFactory,
    IHubContext<NotificationHub> hubContext,
    NotificationRealtimeDeliveryTracker deliveryTracker,
    ILogger<NotificationRealtimeDispatcher> logger) : BackgroundService
{
    private int _lastObservedNotificationId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DispatchNewNotificationsAsync(stoppingToken);
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _lastObservedNotificationId = await context.Notifications.AsNoTracking()
            .MaxAsync(item => (int?)item.Id, cancellationToken)
            ?? 0;
    }

    private async Task DispatchNewNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifications = await context.Notifications.AsNoTracking()
                .Where(item => item.Id > _lastObservedNotificationId)
                .OrderBy(item => item.Id)
                .Take(200)
                .ToListAsync(cancellationToken);

            foreach (var notification in notifications)
            {
                _lastObservedNotificationId = notification.Id;
                if (!deliveryTracker.TryMarkDelivered(notification.Id))
                {
                    continue;
                }

                var unreadCount = await context.Notifications.AsNoTracking()
                    .CountAsync(
                        item =>
                            item.CompanyId == notification.CompanyId
                            && item.UserId == notification.UserId
                            && !item.IsRead,
                        cancellationToken);
                var client = hubContext.Clients.Group(NotificationHub.UserGroup(notification.UserId));
                await client.SendAsync(
                    "notification.created",
                    new NotificationRealtimePayload
                    {
                        Id = notification.Id,
                        Title = notification.Title,
                        Message = notification.Message,
                        Type = notification.Type.ToString(),
                        IsRead = notification.IsRead,
                        CreatedAt = notification.CreatedAt,
                        RelatedEntityType = notification.RelatedTaskId.HasValue ? "Task" : null,
                        RelatedEntityId = notification.RelatedTaskId
                    },
                    cancellationToken);
                await client.SendAsync(
                    "notification.unreadCountChanged",
                    new { count = unreadCount },
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to dispatch new database notifications through SignalR.");
        }
    }
}
