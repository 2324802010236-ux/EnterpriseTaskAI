using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Application.Notifications;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseTask.Infrastructure.Notifications;

public class NotificationService(
    AppDbContext context,
    INotificationRealtimeSender realtimeSender,
    ILogger<NotificationService> logger) : INotificationService
{
    private static readonly string[] DepartmentRecipientRoles =
    [
        AppRoles.Director,
        AppRoles.DepartmentManager,
        AppRoles.Employee
    ];

    public async Task CreateAsync(
        int companyId,
        string userId,
        string title,
        string message,
        NotificationType type,
        int? relatedTaskId = null,
        CancellationToken cancellationToken = default)
    {
        await AddNotificationsAsync(
            companyId,
            [userId],
            title,
            message,
            type,
            relatedTaskId,
            cancellationToken);
    }

    public async Task CreateTaskAssignedNotificationAsync(
        int companyId,
        int taskId,
        string taskTitle,
        string? assignedUserId,
        int? assignedDepartmentId,
        string? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var recipientIds = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(assignedUserId))
        {
            recipientIds.Add(assignedUserId);
        }

        if (assignedDepartmentId.HasValue)
        {
            recipientIds.UnionWith(await GetActiveDepartmentRecipientIdsAsync(
                companyId,
                assignedDepartmentId.Value,
                cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(excludedUserId))
        {
            recipientIds.Remove(excludedUserId);
        }

        await AddNotificationsAsync(
            companyId,
            recipientIds,
            "Bạn có công việc mới",
            $"Bạn được giao công việc: {taskTitle}",
            NotificationType.TaskAssigned,
            taskId,
            cancellationToken);
    }

    public async Task CreateTaskStatusChangedNotificationAsync(
        int companyId,
        int taskId,
        string taskTitle,
        WorkTaskStatus status,
        string changedByUserId,
        CancellationToken cancellationToken = default)
    {
        var recipientIds = await GetRelatedUserIdsAsync(companyId, taskId, cancellationToken);
        recipientIds.Remove(changedByUserId);

        await AddNotificationsAsync(
            companyId,
            recipientIds,
            "Trạng thái công việc đã thay đổi",
            $"{taskTitle} đã chuyển sang {status}",
            NotificationType.TaskStatusChanged,
            taskId,
            cancellationToken);
    }

    public async Task CreateTaskCommentedNotificationAsync(
        int companyId,
        int taskId,
        string taskTitle,
        string authorName,
        string authorUserId,
        CancellationToken cancellationToken = default)
    {
        var recipientIds = await GetRelatedUserIdsAsync(companyId, taskId, cancellationToken);
        recipientIds.Remove(authorUserId);

        await AddNotificationsAsync(
            companyId,
            recipientIds,
            "Có bình luận mới trong công việc",
            $"{authorName} đã bình luận trong {taskTitle}",
            NotificationType.TaskCommented,
            taskId,
            cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(
        string userId,
        int companyId,
        CancellationToken cancellationToken = default) =>
        context.Notifications.AsNoTracking()
            .CountAsync(
                item =>
                    item.CompanyId == companyId
                    && item.UserId == userId
                    && !item.IsRead,
                cancellationToken);

    private async Task<HashSet<string>> GetRelatedUserIdsAsync(
        int companyId,
        int taskId,
        CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks.AsNoTracking()
            .Where(item => item.Id == taskId && item.CompanyId == companyId)
            .Select(item => new
            {
                item.CreatedByUserId,
                item.AssignedDepartmentId
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (task is null)
        {
            return [];
        }

        var assignment = await context.TaskAssignments.AsNoTracking()
            .Where(item => item.WorkTaskId == taskId && item.CompanyId == companyId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new
            {
                item.AssignedToUserId,
                item.AssignedToDepartmentId
            })
            .FirstOrDefaultAsync(cancellationToken);

        var recipientIds = new HashSet<string>(StringComparer.Ordinal)
        {
            task.CreatedByUserId
        };
        if (!string.IsNullOrWhiteSpace(assignment?.AssignedToUserId))
        {
            recipientIds.Add(assignment.AssignedToUserId);
        }

        var departmentId = assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId;
        if (departmentId.HasValue)
        {
            recipientIds.UnionWith(await GetActiveDepartmentRecipientIdsAsync(
                companyId,
                departmentId.Value,
                cancellationToken));
        }

        return recipientIds;
    }

    private async Task<List<string>> GetActiveDepartmentRecipientIdsAsync(
        int companyId,
        int departmentId,
        CancellationToken cancellationToken) =>
        await context.UserRoles.AsNoTracking()
            .Where(userRole =>
                context.Users.Any(user =>
                    user.Id == userRole.UserId
                    && user.CompanyId == companyId
                    && user.DepartmentId == departmentId
                    && user.IsActive)
                && context.Roles.Any(role =>
                    role.Id == userRole.RoleId
                    && role.Name != null
                    && DepartmentRecipientRoles.Contains(role.Name))
                && !context.UserRoles.Any(protectedUserRole =>
                    protectedUserRole.UserId == userRole.UserId
                    && context.Roles.Any(role =>
                        role.Id == protectedUserRole.RoleId
                        && (role.Name == AppRoles.SystemAdmin
                            || role.Name == AppRoles.CompanyAdmin))))
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    private async Task AddNotificationsAsync(
        int companyId,
        IEnumerable<string> userIds,
        string title,
        string message,
        NotificationType type,
        int? relatedTaskId,
        CancellationToken cancellationToken)
    {
        var requestedUserIds = userIds
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (requestedUserIds.Count == 0)
        {
            return;
        }

        var validUserIds = await context.Users.AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId
                && item.IsActive
                && requestedUserIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (validUserIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var notifications = validUserIds.Select(userId => new Notification
        {
            CompanyId = companyId,
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedTaskId = relatedTaskId,
            IsRead = false,
            CreatedAt = now
        }).ToList();
        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync(cancellationToken);

        await TrySendRealtimeAsync(companyId, notifications, cancellationToken);
    }

    private async Task TrySendRealtimeAsync(
        int companyId,
        IReadOnlyCollection<Notification> notifications,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIds = notifications.Select(item => item.UserId).Distinct().ToList();
            var unreadCounts = await context.Notifications.AsNoTracking()
                .Where(item =>
                    item.CompanyId == companyId
                    && !item.IsRead
                    && userIds.Contains(item.UserId))
                .GroupBy(item => item.UserId)
                .Select(group => new { UserId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.UserId, item => item.Count, cancellationToken);

            foreach (var notification in notifications)
            {
                await realtimeSender.SendToUserAsync(
                    notification.UserId,
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
                await realtimeSender.SendUnreadCountAsync(
                    notification.UserId,
                    unreadCounts.GetValueOrDefault(notification.UserId),
                    cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Notifications were saved, but realtime delivery failed for company {CompanyId}.",
                companyId);
        }
    }
}
