using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Application.Messaging;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseTask.Worker.Services;

public class DeadlineReminderScannerService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<DeadlineReminderScannerService> logger) : BackgroundService
{
    private static readonly string[] DepartmentRecipientRoles =
    [
        AppRoles.Director,
        AppRoles.DepartmentManager,
        AppRoles.Employee
    ];

    private readonly RabbitMqSettings rabbitMqSettings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ScanSafelyAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ScanSafelyAsync(stoppingToken);
        }
    }

    private async Task ScanSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ScanAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deadline reminder scan failed.");
        }
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IBackgroundJobPublisher>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        var tasks = await context.WorkTasks
            .Where(task =>
                task.DueDate.HasValue
                && task.DueDate.Value < now
                && task.Status != WorkTaskStatus.Done
                && task.Status != WorkTaskStatus.Cancelled
                && task.Status != WorkTaskStatus.Overdue
                && task.Company.Status == CompanyStatus.Active
                && task.Company.CompanySubscriptions.Any(subscription =>
                    subscription.Status == SubscriptionStatus.Active
                    && subscription.EndDate >= now.Date))
            .OrderBy(task => task.DueDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            var recipientIds = await GetRecipientIdsAsync(context, task, cancellationToken);
            var previousStatus = task.Status;
            task.Status = WorkTaskStatus.Overdue;
            task.UpdatedAt = now;
            context.TaskStatusHistories.Add(new TaskStatusHistory
            {
                CompanyId = task.CompanyId,
                WorkTaskId = task.Id,
                ChangedByUserId = "BACKGROUND_WORKER",
                FromStatus = previousStatus,
                ToStatus = WorkTaskStatus.Overdue,
                Note = "Tự động chuyển quá hạn bởi deadline reminder worker.",
                ChangedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);

            foreach (var userId in recipientIds)
            {
                try
                {
                    await DispatchReminderAsync(
                        publisher,
                        notificationService,
                        task,
                        userId,
                        now,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Deadline reminder delivery failed for task {TaskId} and user {UserId}.",
                        task.Id,
                        userId);
                }
            }

            logger.LogInformation(
                "Task {TaskId} was marked Overdue and dispatched to {RecipientCount} recipients.",
                task.Id,
                recipientIds.Count);
        }
    }

    private async Task DispatchReminderAsync(
        IBackgroundJobPublisher publisher,
        INotificationService notificationService,
        WorkTask task,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var message = new DeadlineReminderJobMessage
        {
            CompanyId = task.CompanyId,
            TaskId = task.Id,
            UserId = userId,
            TaskTitle = task.Title,
            DueDate = task.DueDate,
            CorrelationId = $"deadline:{task.Id}:{userId}:{now:yyyyMMddHHmmss}",
            CreatedAt = now
        };

        if (rabbitMqSettings.Enabled)
        {
            try
            {
                await publisher.PublishDeadlineReminderAsync(message, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "RabbitMQ deadline publish failed for task {TaskId}; creating notification directly.",
                    task.Id);
            }
        }

        await notificationService.CreateAsync(
            task.CompanyId,
            userId,
            "Công việc đã quá hạn",
            $"{task.Title} đã quá hạn xử lý.",
            NotificationType.DeadlineReminder,
            task.Id,
            cancellationToken);
    }

    private static async Task<HashSet<string>> GetRecipientIdsAsync(
        AppDbContext context,
        WorkTask task,
        CancellationToken cancellationToken)
    {
        var assignment = await context.TaskAssignments.AsNoTracking()
            .Where(item => item.CompanyId == task.CompanyId && item.WorkTaskId == task.Id)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var requestedIds = new HashSet<string>(StringComparer.Ordinal)
        {
            task.CreatedByUserId
        };
        if (!string.IsNullOrWhiteSpace(assignment?.AssignedToUserId))
        {
            requestedIds.Add(assignment.AssignedToUserId);
        }

        var departmentId = assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId;
        if (departmentId.HasValue)
        {
            requestedIds.UnionWith(await context.UserRoles.AsNoTracking()
                .Where(userRole =>
                    context.Users.Any(user =>
                        user.Id == userRole.UserId
                        && user.CompanyId == task.CompanyId
                        && user.DepartmentId == departmentId.Value
                        && user.IsActive)
                    && context.Roles.Any(role =>
                        role.Id == userRole.RoleId
                        && role.Name != null
                        && DepartmentRecipientRoles.Contains(role.Name)))
                .Select(userRole => userRole.UserId)
                .Distinct()
                .ToListAsync(cancellationToken));
        }

        return (await context.Users.AsNoTracking()
                .Where(user =>
                    user.CompanyId == task.CompanyId
                    && user.IsActive
                    && requestedIds.Contains(user.Id))
                .Select(user => user.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
    }
}
