using System.Text.Json;
using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Application.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EnterpriseTask.Infrastructure.Messaging;

public class RabbitMqBackgroundJobPublisher(
    IOptions<RabbitMqSettings> options,
    ILogger<RabbitMqBackgroundJobPublisher> logger) : IBackgroundJobPublisher
{
    private readonly RabbitMqSettings settings = options.Value;

    public Task PublishEmailAsync(
        EmailJobMessage message,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            settings.EmailQueueName,
            message,
            message.CorrelationId,
            "email",
            cancellationToken);

    public Task PublishDeadlineReminderAsync(
        DeadlineReminderJobMessage message,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            settings.DeadlineReminderQueueName,
            message,
            message.CorrelationId,
            "deadline reminder",
            cancellationToken);

    private async Task PublishAsync<T>(
        string queueName,
        T message,
        string? correlationId,
        string jobType,
        CancellationToken cancellationToken)
    {
        if (!settings.Enabled)
        {
            throw new InvalidOperationException("RabbitMQ background jobs are disabled.");
        }

        try
        {
            var factory = RabbitMqConnectionFactory.Create(
                settings,
                $"EnterpriseTask.Publisher.{Environment.ProcessId}");
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                CorrelationId = correlationId,
                MessageId = Guid.NewGuid().ToString("N"),
                Type = typeof(T).Name
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(message);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Published {JobType} job to queue {QueueName} with correlation {CorrelationId}.",
                jobType,
                queueName,
                correlationId);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to publish {JobType} job to queue {QueueName}.",
                jobType,
                queueName);
            throw;
        }
    }
}
