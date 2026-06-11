using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Services.Auth;
using EnterpriseTask.Mobile.Views.Auth;
using EnterpriseTask.Mobile.Views.Chat;
using EnterpriseTask.Mobile.Views.Dashboard;
using EnterpriseTask.Mobile.Views.Notifications;
using EnterpriseTask.Mobile.Views.Onboarding;
using EnterpriseTask.Mobile.Views.Profile;
using EnterpriseTask.Mobile.Views.Tasks;

namespace EnterpriseTask.Mobile;

public partial class AppShell : Shell
{
    private readonly AuthService _authService;
    private bool _initialized;

    public AppShell(
        StartPage startPage,
        DashboardPage dashboardPage,
        TasksPage tasksPage,
        NotificationsPage notificationsPage,
        ChatRoomsPage chatRoomsPage,
        ProfilePage profilePage,
        AuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        Items.Add(new ShellContent
        {
            Route = AppConstants.StartRoute,
            Content = startPage
        });

        var workspaceTabs = new TabBar { Route = "workspace" };
        workspaceTabs.Items.Add(new ShellContent
        {
            Title = "Dashboard",
            Route = AppConstants.DashboardRoute,
            Content = dashboardPage
        });
        workspaceTabs.Items.Add(new ShellContent
        {
            Title = "Công việc",
            Route = AppConstants.TasksRoute,
            Content = tasksPage
        });
        workspaceTabs.Items.Add(new ShellContent
        {
            Title = "Thông báo",
            Route = AppConstants.NotificationsRoute,
            Content = notificationsPage
        });
        workspaceTabs.Items.Add(new ShellContent
        {
            Title = "Chat",
            Route = AppConstants.ChatRoute,
            Content = chatRoomsPage
        });
        workspaceTabs.Items.Add(new ShellContent
        {
            Title = "Hồ sơ",
            Route = AppConstants.ProfileRoute,
            Content = profilePage
        });
        Items.Add(workspaceTabs);

        Routing.RegisterRoute(AppConstants.PlansRoute, typeof(CompanyPlansPage));
        Routing.RegisterRoute(AppConstants.CompanyRegisterRoute, typeof(CompanyRegisterPage));
        Routing.RegisterRoute(AppConstants.PurchaseResultRoute, typeof(CompanyPurchaseResultPage));
        Routing.RegisterRoute(AppConstants.LoginRoute, typeof(LoginPage));
        Routing.RegisterRoute(AppConstants.TaskDetailsRoute, typeof(TaskDetailsPage));
        Routing.RegisterRoute(AppConstants.ChatMessagesRoute, typeof(ChatMessagesPage));

        Loaded += OnShellLoaded;
    }

    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            if (await _authService.IsLoggedInAsync())
            {
                await GoToAsync($"//{AppConstants.DashboardRoute}", false);
            }
        }
        catch
        {
            // SecureStorage or startup navigation can be unavailable on a new host.
            // The first ShellContent remains the safe signed-out start page.
        }
    }
}
