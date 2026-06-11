namespace EnterpriseTask.Application.Interfaces;

public interface IEmailDeliveryService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
