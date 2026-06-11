using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Services.Auth;
using EnterpriseTask.Mobile.Views.Auth;
using EnterpriseTask.Mobile.Views.Dashboard;
using EnterpriseTask.Mobile.Views.Onboarding;

namespace EnterpriseTask.Mobile;

public partial class AppShell : Shell
{
    private readonly AuthService _authService;
    private bool _initialized;

    public AppShell(StartPage startPage, DashboardPage dashboardPage, AuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        Items.Add(new ShellContent
        {
            Route = AppConstants.StartRoute,
            Content = startPage
        });
        Items.Add(new ShellContent
        {
            Route = AppConstants.DashboardRoute,
            Content = dashboardPage
        });

        Routing.RegisterRoute(AppConstants.PlansRoute, typeof(CompanyPlansPage));
        Routing.RegisterRoute(AppConstants.LoginRoute, typeof(LoginPage));

        Loaded += OnShellLoaded;
    }

    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var route = AppConstants.StartRoute;

        try
        {
            if (await _authService.IsLoggedInAsync())
            {
                route = AppConstants.DashboardRoute;
            }
        }
        catch
        {
            // SecureStorage can be unavailable on a new or unsupported host; start signed out.
        }

        await GoToAsync($"//{route}", false);
    }
}
