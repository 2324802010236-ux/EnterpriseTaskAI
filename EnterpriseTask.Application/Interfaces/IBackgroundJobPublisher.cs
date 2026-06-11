using EnterpriseTask.Application.Messaging;

namespace EnterpriseTask.Application.Interfaces;

public interface IBackgroundJobPublisher
{
    Task PublishEmailAsync(
        EmailJobMessage message,
        CancellationToken cancellationToken = default);

    Task PublishDeadlineReminderAsync(
        DeadlineReminderJobMessage message,
        CancellationToken cancellationToken = default);
}
