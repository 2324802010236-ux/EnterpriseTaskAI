using EnterpriseTask.Application.Chat;

namespace EnterpriseTask.Application.Interfaces;

public interface IChatRealtimeSender
{
    Task SendMessageCreatedAsync(
        int roomId,
        ChatMessageInfo message,
        CancellationToken cancellationToken = default);

    Task SendRoomUpdatedAsync(
        int roomId,
        ChatMessageInfo message,
        CancellationToken cancellationToken = default);
}
