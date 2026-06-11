namespace EnterpriseTask.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public bool Enabled { get; set; } = true;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string EmailQueueName { get; set; } = "enterprisetask.email";
    public string DeadlineReminderQueueName { get; set; } = "enterprisetask.deadline-reminder";
}
