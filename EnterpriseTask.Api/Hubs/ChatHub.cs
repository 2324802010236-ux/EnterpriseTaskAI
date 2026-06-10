using EnterpriseTask.Api.Services;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Api.Hubs;

[Authorize]
public class ChatHub(
    MobileWorkspaceAccessService accessService,
    AppDbContext context,
    ILogger<ChatHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var access = await accessService.CheckAccessAsync(Context.ConnectionAborted);
        if (!access.IsAllowed)
        {
            logger.LogWarning(
                "Rejected chat hub connection {ConnectionId}: {Reason}",
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

        var roomIds = await context.ChatRoomMembers.AsNoTracking()
            .Where(item =>
                item.CompanyId == workspace.Company.Id
                && item.UserId == workspace.User.Id
                && item.ChatRoom.CompanyId == workspace.Company.Id
                && item.ChatRoom.IsActive
                && (item.ChatRoom.Type == ChatRoomType.Direct
                    || (item.ChatRoom.Type == ChatRoomType.Department
                        && workspace.Department != null
                        && item.ChatRoom.DepartmentId == workspace.Department.Id)
                    || (item.ChatRoom.Type == ChatRoomType.Task
                        && item.ChatRoom.WorkTaskId.HasValue
                        && (workspace.Role == AppRoles.Director
                            || workspace.Role == AppRoles.CompanyAdmin
                            || item.ChatRoom.WorkTask!.CreatedByUserId == workspace.User.Id
                            || item.ChatRoom.WorkTask.Assignments.Any(assignment =>
                                assignment.CompanyId == workspace.Company.Id
                                && (assignment.AssignedToUserId == workspace.User.Id
                                    || (workspace.Department != null
                                        && assignment.AssignedToDepartmentId
                                            == workspace.Department.Id)))
                            || (workspace.Department != null
                                && item.ChatRoom.WorkTask.AssignedDepartmentId
                                    == workspace.Department.Id)))))
            .Select(item => item.ChatRoomId)
            .ToListAsync(Context.ConnectionAborted);
        foreach (var roomId in roomIds)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RoomGroup(roomId),
                Context.ConnectionAborted);
        }

        logger.LogInformation(
            "Chat hub connected for user {UserId} in company {CompanyId} with {RoomCount} rooms.",
            workspace.User.Id,
            workspace.Company.Id,
            roomIds.Count);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            exception,
            "Chat hub disconnected for connection {ConnectionId}.",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public static string UserGroup(string userId) => $"user:{userId}";

    public static string RoomGroup(int roomId) => $"chatroom:{roomId}";
}
