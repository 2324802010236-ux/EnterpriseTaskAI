using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Application.Messaging;
using EnterpriseTask.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseTask.Infrastructure.Email;

public class EmailDeliveryService(
    IBackgroundJobPublisher backgroundJobPublisher,
    IEmailSender emailSender,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    ILogger<EmailDeliveryService> logger) : IEmailDeliveryService
{
    private readonly RabbitMqSettings rabbitMqSettings = rabbitMqOptions.Value;

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        if (rabbitMqSettings.Enabled)
        {
            try
            {
                await backgroundJobPublisher.PublishEmailAsync(
                    new EmailJobMessage
                    {
                        To = to,
                        Subject = subject,
                        HtmlBody = htmlBody,
                        CorrelationId = Guid.NewGuid().ToString("N"),
                        CreatedAt = DateTime.UtcNow
                    },
                    cancellationToken);
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
                    "RabbitMQ email publish failed for {Recipient}; falling back to direct SMTP.",
                    to);
            }
        }
        else
        {
            logger.LogDebug(
                "RabbitMQ is disabled; sending email directly to {Recipient}.",
                to);
        }

        await emailSender.SendEmailAsync(to, subject, htmlBody);
    }
}
