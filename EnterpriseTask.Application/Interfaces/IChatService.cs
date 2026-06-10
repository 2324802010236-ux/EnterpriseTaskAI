using EnterpriseTask.Application.Chat;

namespace EnterpriseTask.Application.Interfaces;

public interface IChatService
{
    Task<IReadOnlyList<ChatRoomInfo>> GetRoomsForUserAsync(
        string userId,
        int companyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessageInfo>> GetMessagesAsync(
        int roomId,
        string userId,
        int companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ChatMessageInfo> SendMessageAsync(
        int roomId,
        string userId,
        int companyId,
        string content,
        CancellationToken cancellationToken = default);

    Task<ChatRoomInfo> EnsureDepartmentRoomAsync(
        int companyId,
        int departmentId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ChatRoomInfo> EnsureTaskRoomAsync(
        int companyId,
        int taskId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ChatRoomInfo> EnsureDirectRoomAsync(
        int companyId,
        string userId1,
        string userId2,
        CancellationToken cancellationToken = default);
}
