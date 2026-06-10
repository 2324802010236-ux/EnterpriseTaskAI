using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Application.Interfaces;

public interface INotificationService
{
    Task CreateAsync(
        int companyId,
        string userId,
        string title,
        string message,
        NotificationType type,
        int? relatedTaskId = null,
        CancellationToken cancellationToken = default);

    Task CreateTaskAssignedNotificationAsync(
        int companyId,
        int taskId,
        string taskTitle,
        string? assignedUserId,
        int? assignedDepartmentId,
        string? excludedUserId = null,
        CancellationToken cancellationToken = default);

    Task CreateTaskStatusChangedNotificationAsync(
        int companyId,
        int taskId,
        string taskTitle,
        WorkTaskStatus status,
        string changedByUserId,
        CancellationToken cancellationToken = default);

    Task CreateTaskCommentedNotificationAsync(
        int companyId,
        int taskId,
        string taskTitle,
        string authorName,
        string authorUserId,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        string userId,
        int companyId,
        CancellationToken cancellationToken = default);
}
