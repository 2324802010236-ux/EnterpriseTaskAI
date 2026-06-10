using EnterpriseTask.Application.Chat;
using EnterpriseTask.Application.Interfaces;

namespace EnterpriseTask.Infrastructure.Chat;

public class NullChatRealtimeSender : IChatRealtimeSender
{
    public Task SendMessageCreatedAsync(
        int roomId,
        ChatMessageInfo message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendRoomUpdatedAsync(
        int roomId,
        ChatMessageInfo message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
