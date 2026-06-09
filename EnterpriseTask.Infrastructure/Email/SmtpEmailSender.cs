using System.Net;
using System.Net.Mail;
using System.Text;
using EnterpriseTask.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseTask.Infrastructure.Email;

public class SmtpEmailSender(
    IOptions<EmailSettings> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailSettings settings = options.Value;

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        try
        {
            ValidateSettings();

            using var message = new MailMessage
            {
                From = new MailAddress(settings.FromEmail, settings.FromName),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = htmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(to));

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.UserName, settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await client.SendMailAsync(message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send email to {Recipient}.", to);
            throw;
        }
    }

    private void ValidateSettings()
    {
        var missingSettings = new List<string>();

        AddIfMissing(missingSettings, nameof(EmailSettings.Host), settings.Host);
        AddIfMissing(missingSettings, nameof(EmailSettings.UserName), settings.UserName);
        AddIfMissing(missingSettings, nameof(EmailSettings.Password), settings.Password);
        AddIfMissing(missingSettings, nameof(EmailSettings.FromEmail), settings.FromEmail);

        if (settings.Port is <= 0 or > 65535)
        {
            missingSettings.Add(nameof(EmailSettings.Port));
        }

        if (missingSettings.Count > 0)
        {
            throw new InvalidOperationException(
                $"SMTP configuration is missing or invalid: {string.Join(", ", missingSettings)}.");
        }
    }

    private static void AddIfMissing(List<string> missingSettings, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingSettings.Add(name);
        }
    }
}
