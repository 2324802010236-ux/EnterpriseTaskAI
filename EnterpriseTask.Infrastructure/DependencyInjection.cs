using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Email;
using EnterpriseTask.Infrastructure.Identity;
using EnterpriseTask.Infrastructure.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseTask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<EmailSettings>(
            configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificationRealtimeSender, NullNotificationRealtimeSender>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
