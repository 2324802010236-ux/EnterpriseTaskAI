using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Application.Messaging;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseTask.Worker.Services;

public class DeadlineReminderJobConsumerService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<DeadlineReminderJobConsumerService> logger)
    : RabbitMqConsumerService<DeadlineReminderJobMessage>(options, logger)
{
    protected override string QueueName => Settings.DeadlineReminderQueueName;
    protected override string JobName => "Deadline reminder";

    protected override async Task ProcessMessageAsync(
        DeadlineReminderJobMessage message,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alreadyCreated = await context.Notifications.AsNoTracking()
            .AnyAsync(
                item =>
                    item.CompanyId == message.CompanyId
                    && item.UserId == message.UserId
                    && item.RelatedTaskId == message.TaskId
                    && item.Type == NotificationType.DeadlineReminder,
                cancellationToken);
        if (alreadyCreated)
        {
            logger.LogInformation(
                "Skipped duplicate deadline reminder for task {TaskId} and user {UserId}.",
                message.TaskId,
                message.UserId);
            return;
        }

        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notificationService.CreateAsync(
            message.CompanyId,
            message.UserId,
            "Công việc đã quá hạn",
            $"{message.TaskTitle} đã quá hạn xử lý.",
            NotificationType.DeadlineReminder,
            message.TaskId,
            cancellationToken);
    }
}
