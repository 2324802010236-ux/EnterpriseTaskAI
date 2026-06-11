using RabbitMQ.Client;

namespace EnterpriseTask.Infrastructure.Messaging;

public static class RabbitMqConnectionFactory
{
    public static ConnectionFactory Create(RabbitMqSettings settings, string clientName)
    {
        Validate(settings);

        return new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            ClientProvidedName = clientName,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
    }

    private static void Validate(RabbitMqSettings settings)
    {
        var missing = new List<string>();
        AddIfMissing(missing, nameof(RabbitMqSettings.HostName), settings.HostName);
        AddIfMissing(missing, nameof(RabbitMqSettings.UserName), settings.UserName);
        AddIfMissing(missing, nameof(RabbitMqSettings.Password), settings.Password);
        AddIfMissing(missing, nameof(RabbitMqSettings.VirtualHost), settings.VirtualHost);
        AddIfMissing(missing, nameof(RabbitMqSettings.EmailQueueName), settings.EmailQueueName);
        AddIfMissing(
            missing,
            nameof(RabbitMqSettings.DeadlineReminderQueueName),
            settings.DeadlineReminderQueueName);

        if (settings.Port is <= 0 or > 65535)
        {
            missing.Add(nameof(RabbitMqSettings.Port));
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"RabbitMQ configuration is missing or invalid: {string.Join(", ", missing)}.");
        }
    }

    private static void AddIfMissing(List<string> missing, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(name);
        }
    }
}
