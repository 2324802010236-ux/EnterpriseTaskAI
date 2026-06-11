using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Services.Api;
using EnterpriseTask.Mobile.Services.Auth;
using EnterpriseTask.Mobile.Services.Onboarding;
using EnterpriseTask.Mobile.Services.Mobile;
using EnterpriseTask.Mobile.ViewModels;
using EnterpriseTask.Mobile.Views.Auth;
using EnterpriseTask.Mobile.Views.Chat;
using EnterpriseTask.Mobile.Views.Dashboard;
using EnterpriseTask.Mobile.Views.Notifications;
using EnterpriseTask.Mobile.Views.Onboarding;
using EnterpriseTask.Mobile.Views.Profile;
using EnterpriseTask.Mobile.Views.Tasks;
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
        builder.Services.AddSingleton<OnboardingService>();
        builder.Services.AddSingleton<OnboardingState>();
        builder.Services.AddSingleton<MobileWorkspaceService>();
        builder.Services.AddSingleton<MobileTaskService>();
        builder.Services.AddSingleton<MobileNotificationService>();
        builder.Services.AddSingleton<MobileChatService>();
        builder.Services.AddSingleton<WorkspaceSessionService>();

        builder.Services.AddTransient<StartViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<CompanyPlansViewModel>();
        builder.Services.AddTransient<CompanyRegisterViewModel>();
        builder.Services.AddTransient<CompanyPurchaseResultViewModel>();
        builder.Services.AddTransient<TasksViewModel>();
        builder.Services.AddTransient<TaskDetailsViewModel>();
        builder.Services.AddTransient<NotificationsViewModel>();
        builder.Services.AddTransient<ChatRoomsViewModel>();
        builder.Services.AddTransient<ChatMessagesViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        builder.Services.AddSingleton<StartPage>();
        builder.Services.AddTransient<CompanyPlansPage>();
        builder.Services.AddTransient<CompanyRegisterPage>();
        builder.Services.AddTransient<CompanyPurchaseResultPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<DashboardPage>();
        builder.Services.AddSingleton<TasksPage>();
        builder.Services.AddTransient<TaskDetailsPage>();
        builder.Services.AddSingleton<NotificationsPage>();
        builder.Services.AddSingleton<ChatRoomsPage>();
        builder.Services.AddTransient<ChatMessagesPage>();
        builder.Services.AddSingleton<ProfilePage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
