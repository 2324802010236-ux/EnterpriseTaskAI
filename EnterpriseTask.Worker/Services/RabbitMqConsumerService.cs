using System.Text.Json;
using EnterpriseTask.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EnterpriseTask.Worker.Services;

public abstract class RabbitMqConsumerService<TMessage>(
    IOptions<RabbitMqSettings> options,
    ILogger logger) : BackgroundService
{
    protected RabbitMqSettings Settings { get; } = options.Value;

    protected abstract string QueueName { get; }
    protected abstract string JobName { get; }

    protected abstract Task ProcessMessageAsync(
        TMessage message,
        CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Settings.Enabled)
        {
            logger.LogInformation(
                "RabbitMQ is disabled; {JobName} consumer will not start.",
                JobName);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilDisconnectedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{JobName} consumer lost RabbitMQ connection. Retrying in 5 seconds.",
                    JobName);
            }

            await DelayBeforeRetryAsync(stoppingToken);
        }
    }

    private async Task ConsumeUntilDisconnectedAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqConnectionFactory.Create(
            Settings,
            $"EnterpriseTask.Worker.{JobName}.{Environment.ProcessId}");
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(eventArgs.Body.Span)
                    ?? throw new JsonException($"{JobName} message body is empty.");
                await ProcessMessageAsync(message, eventArgs.CancellationToken);
                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    eventArgs.CancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{JobName} job failed and will be discarded. CorrelationId: {CorrelationId}.",
                    JobName,
                    eventArgs.BasicProperties.CorrelationId);
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    eventArgs.CancellationToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "{JobName} consumer is listening on queue {QueueName}.",
            JobName,
            QueueName);

        while (!stoppingToken.IsCancellationRequested && connection.IsOpen && channel.IsOpen)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private static async Task DelayBeforeRetryAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
