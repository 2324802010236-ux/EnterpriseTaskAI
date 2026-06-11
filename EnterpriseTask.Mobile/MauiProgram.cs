using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Services.Api;
using EnterpriseTask.Mobile.Services.Auth;
using EnterpriseTask.Mobile.ViewModels;
using EnterpriseTask.Mobile.Views.Auth;
using EnterpriseTask.Mobile.Views.Dashboard;
using EnterpriseTask.Mobile.Views.Onboarding;
using Microsoft.Extensions.Logging;

namespace EnterpriseTask.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(AppConstants.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        });
        builder.Services.AddSingleton<TokenStorageService>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<AuthService>();

        builder.Services.AddTransient<StartViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        builder.Services.AddSingleton<StartPage>();
        builder.Services.AddTransient<CompanyPlansPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
