namespace EnterpriseTask.Mobile.Constants;

public static class AppConstants
{
#if ANDROID
    // Android emulators reach the host machine through 10.0.2.2.
    public const string ApiBaseUrl = "http://10.0.2.2:5050";
#else
    public const string ApiBaseUrl = "http://localhost:5050";
#endif

    public static string SignalRNotificationHubUrl => $"{ApiBaseUrl}/hubs/notifications";

    public static string SignalRChatHubUrl => $"{ApiBaseUrl}/hubs/chat";

    public const string StartRoute = "start";
    public const string PlansRoute = "plans";
    public const string CompanyRegisterRoute = "company-register";
    public const string PurchaseResultRoute = "purchase-result";
    public const string LoginRoute = "login";
    public const string DashboardRoute = "dashboard";
    public const string TasksRoute = "tasks";
    public const string TaskDetailsRoute = "task-details";
    public const string NotificationsRoute = "notifications";
    public const string ChatRoute = "chat";
    public const string ChatMessagesRoute = "chat-messages";
    public const string ProfileRoute = "profile";
}
