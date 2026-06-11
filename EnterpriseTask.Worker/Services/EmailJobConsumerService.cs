using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Application.Messaging;
using EnterpriseTask.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace EnterpriseTask.Worker.Services;

public class EmailJobConsumerService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<EmailJobConsumerService> logger)
    : RabbitMqConsumerService<EmailJobMessage>(options, logger)
{
    protected override string QueueName => Settings.EmailQueueName;
    protected override string JobName => "Email";

    protected override async Task ProcessMessageAsync(
        EmailJobMessage message,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        await emailSender.SendEmailAsync(message.To, message.Subject, message.HtmlBody);

        logger.LogInformation(
            "Email job delivered to {Recipient}. CorrelationId: {CorrelationId}.",
            message.To,
            message.CorrelationId);
    }
}
