using EnterpriseTask.Api.Hubs;
using EnterpriseTask.Application.Chat;
using EnterpriseTask.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseTask.Api.Services;

public class SignalRChatRealtimeSender(
    IHubContext<ChatHub> hubContext,
    ILogger<SignalRChatRealtimeSender> logger) : IChatRealtimeSender
{
    public async Task SendMessageCreatedAsync(
        int roomId,
        ChatMessageInfo message,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(ChatHub.RoomGroup(roomId))
            .SendAsync("chat.message.created", message, cancellationToken);
        logger.LogDebug(
            "Sent realtime chat message {MessageId} to room {RoomId}.",
            message.Id,
            roomId);
    }

    public async Task SendRoomUpdatedAsync(
        int roomId,
        ChatMessageInfo message,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(ChatHub.RoomGroup(roomId))
            .SendAsync(
                "chat.room.updated",
                new
                {
                    roomId,
                    lastMessage = message.Content,
                    lastMessageAt = message.CreatedAt,
                    senderId = message.SenderId
                },
                cancellationToken);
        logger.LogDebug(
            "Sent realtime room update for room {RoomId}.",
            roomId);
    }
}
