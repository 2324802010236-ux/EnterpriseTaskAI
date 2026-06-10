using EnterpriseTask.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseTask.Api.Hubs;

[Authorize]
public class NotificationHub(
    MobileWorkspaceAccessService accessService,
    ILogger<NotificationHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var access = await accessService.CheckAccessAsync(Context.ConnectionAborted);
        if (!access.IsAllowed)
        {
            logger.LogWarning(
                "Rejected notification hub connection {ConnectionId}: {Reason}",
                Context.ConnectionId,
                access.Message);
            Context.Abort();
            return;
        }

        var workspace = access.Workspace!;
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            UserGroup(workspace.User.Id),
            Context.ConnectionAborted);
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            CompanyGroup(workspace.Company.Id),
            Context.ConnectionAborted);

        logger.LogInformation(
            "Notification hub connected for user {UserId} in company {CompanyId}.",
            workspace.User.Id,
            workspace.Company.Id);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            exception,
            "Notification hub disconnected for connection {ConnectionId}.",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public static string UserGroup(string userId) => $"user:{userId}";

    public static string CompanyGroup(int companyId) => $"company:{companyId}";
}
