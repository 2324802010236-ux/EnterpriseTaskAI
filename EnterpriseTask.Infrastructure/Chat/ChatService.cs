using EnterpriseTask.Application.Chat;
using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseTask.Infrastructure.Chat;

public class ChatService(
    AppDbContext context,
    IChatRealtimeSender realtimeSender,
    ILogger<ChatService> logger) : IChatService
{
    public async Task<IReadOnlyList<ChatRoomInfo>> GetRoomsForUserAsync(
        string userId,
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var departmentId = await context.Users.AsNoTracking()
            .Where(item => item.Id == userId && item.CompanyId == companyId && item.IsActive)
            .Select(item => item.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        var hasBroadTaskAccess = await context.UserRoles.AsNoTracking()
            .AnyAsync(
                userRole =>
                    userRole.UserId == userId
                    && context.Roles.Any(role =>
                        role.Id == userRole.RoleId
                        && (role.Name == AppRoles.Director || role.Name == AppRoles.CompanyAdmin)),
                cancellationToken);

        return await context.ChatRoomMembers.AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId
                && item.UserId == userId
                && item.ChatRoom.CompanyId == companyId
                && item.ChatRoom.IsActive
                && (item.ChatRoom.Type != ChatRoomType.Department
                    || context.Users.Any(user =>
                        user.Id == userId
                        && user.CompanyId == companyId
                        && user.IsActive
                        && user.DepartmentId == item.ChatRoom.DepartmentId))
                && (item.ChatRoom.Type != ChatRoomType.Task
                    || (item.ChatRoom.WorkTask != null
                        && (hasBroadTaskAccess
                            || item.ChatRoom.WorkTask.CreatedByUserId == userId
                            || item.ChatRoom.WorkTask.Assignments.Any(assignment =>
                                assignment.CompanyId == companyId
                                && (assignment.AssignedToUserId == userId
                                    || (departmentId.HasValue
                                        && assignment.AssignedToDepartmentId == departmentId.Value)))
                            || (departmentId.HasValue
                                && item.ChatRoom.WorkTask.AssignedDepartmentId
                                    == departmentId.Value)))))
            .Select(item => new ChatRoomInfo
            {
                Id = item.ChatRoomId,
                Name = item.ChatRoom.Name,
                Type = item.ChatRoom.Type.ToString(),
                DepartmentId = item.ChatRoom.DepartmentId,
                TaskId = item.ChatRoom.WorkTaskId,
                LastMessage = item.ChatRoom.Messages
                    .Where(message => !message.IsDeleted)
                    .OrderByDescending(message => message.CreatedAt)
                    .ThenByDescending(message => message.Id)
                    .Select(message => message.Content)
                    .FirstOrDefault(),
                LastMessageAt = item.ChatRoom.Messages
                    .Where(message => !message.IsDeleted)
                    .OrderByDescending(message => message.CreatedAt)
                    .ThenByDescending(message => message.Id)
                    .Select(message => (DateTime?)message.CreatedAt)
                    .FirstOrDefault(),
                UnreadCount = item.ChatRoom.Messages.Count(message =>
                    !message.IsDeleted
                    && message.SenderUserId != userId
                    && message.CreatedAt > (item.LastReadAt ?? item.JoinedAt))
            })
            .OrderByDescending(item => item.LastMessageAt ?? DateTime.MinValue)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessageInfo>> GetMessagesAsync(
        int roomId,
        string userId,
        int companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipAsync(roomId, userId, companyId, cancellationToken);
        var messages = await context.ChatMessages.AsNoTracking()
            .Where(item =>
                item.ChatRoomId == roomId
                && item.CompanyId == companyId
                && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var senderNames = await GetUserNamesAsync(
            companyId,
            messages.Select(item => item.SenderUserId),
            cancellationToken);

        if (page == 1)
        {
            membership.LastReadAt = messages.Count > 0
                ? messages.Max(item => item.CreatedAt)
                : DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        return messages
            .Select(item => ToMessageInfo(item, senderNames.GetValueOrDefault(item.SenderUserId)))
            .Reverse()
            .ToList();
    }

    public async Task<ChatMessageInfo> SendMessageAsync(
        int roomId,
        string userId,
        int companyId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var normalizedContent = content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            throw new ArgumentException("Nội dung tin nhắn không được để trống.", nameof(content));
        }

        if (normalizedContent.Length > 2000)
        {
            throw new ArgumentException("Tin nhắn không được vượt quá 2000 ký tự.", nameof(content));
        }

        var membership = await GetMembershipAsync(roomId, userId, companyId, cancellationToken);
        var senderName = await context.Users.AsNoTracking()
            .Where(item => item.Id == userId && item.CompanyId == companyId && item.IsActive)
            .Select(item => item.FullName)
            .FirstOrDefaultAsync(cancellationToken);
        if (senderName is null)
        {
            throw new ChatAccessException("Bạn không có quyền gửi tin nhắn trong phòng chat này.");
        }

        var message = new ChatMessage
        {
            CompanyId = companyId,
            ChatRoomId = roomId,
            SenderUserId = userId,
            Content = normalizedContent,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        context.ChatMessages.Add(message);
        membership.LastReadAt = message.CreatedAt;
        await context.SaveChangesAsync(cancellationToken);

        var result = ToMessageInfo(message, senderName);
        try
        {
            await realtimeSender.SendMessageCreatedAsync(roomId, result, cancellationToken);
            await realtimeSender.SendRoomUpdatedAsync(roomId, result, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Chat message {MessageId} was saved, but realtime delivery failed.",
                message.Id);
        }

        return result;
    }

    public async Task<ChatRoomInfo> EnsureDepartmentRoomAsync(
        int companyId,
        int departmentId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var department = await context.Departments.AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == departmentId
                    && item.CompanyId == companyId
                    && item.IsActive,
                cancellationToken);
        var requesterIsDepartmentMember = await context.Users.AsNoTracking()
            .AnyAsync(
                item =>
                    item.Id == userId
                    && item.CompanyId == companyId
                    && item.DepartmentId == departmentId
                    && item.IsActive,
                cancellationToken);
        if (department is null || !requesterIsDepartmentMember)
        {
            throw new ChatAccessException("Bạn không thuộc phòng ban này.");
        }

        var room = await context.ChatRooms
            .FirstOrDefaultAsync(
                item =>
                    item.CompanyId == companyId
                    && item.Type == ChatRoomType.Department
                    && item.DepartmentId == departmentId,
                cancellationToken);
        if (room is null)
        {
            room = new ChatRoom
            {
                CompanyId = companyId,
                Name = $"Phòng ban: {department.Name}",
                Type = ChatRoomType.Department,
                DepartmentId = departmentId,
                CreatedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.ChatRooms.Add(room);
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (!room.IsActive)
        {
            room.IsActive = true;
        }

        var memberIds = await context.Users.AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId
                && item.DepartmentId == departmentId
                && item.IsActive)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        await SyncMembersAsync(room.Id, companyId, memberIds, removeMissing: true, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await GetRoomInfoAsync(room.Id, userId, companyId, cancellationToken);
    }

    public async Task<ChatRoomInfo> EnsureTaskRoomAsync(
        int companyId,
        int taskId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var task = await context.WorkTasks.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == taskId && item.CompanyId == companyId,
                cancellationToken);
        if (task is null || !await CanAccessTaskAsync(task, userId, companyId, cancellationToken))
        {
            throw new ChatAccessException("Bạn không có quyền truy cập phòng chat của công việc này.");
        }

        var assignment = await context.TaskAssignments.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.WorkTaskId == taskId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var memberIds = new HashSet<string>(StringComparer.Ordinal)
        {
            task.CreatedByUserId,
            userId
        };
        if (!string.IsNullOrWhiteSpace(assignment?.AssignedToUserId))
        {
            memberIds.Add(assignment.AssignedToUserId);
        }

        var departmentId = assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId;
        if (departmentId.HasValue)
        {
            memberIds.UnionWith(await context.Users.AsNoTracking()
                .Where(item =>
                    item.CompanyId == companyId
                    && item.DepartmentId == departmentId.Value
                    && item.IsActive)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken));
        }

        memberIds.UnionWith(await context.UserRoles.AsNoTracking()
            .Where(userRole =>
                context.Users.Any(user =>
                    user.Id == userRole.UserId
                    && user.CompanyId == companyId
                    && user.IsActive)
                && context.Roles.Any(role =>
                    role.Id == userRole.RoleId
                    && (role.Name == AppRoles.Director || role.Name == AppRoles.CompanyAdmin)))
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync(cancellationToken));

        var activeMemberIds = await context.Users.AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId
                && item.IsActive
                && memberIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var room = await context.ChatRooms
            .FirstOrDefaultAsync(
                item =>
                    item.CompanyId == companyId
                    && item.Type == ChatRoomType.Task
                    && item.WorkTaskId == taskId,
                cancellationToken);
        if (room is null)
        {
            room = new ChatRoom
            {
                CompanyId = companyId,
                Name = $"Công việc: {task.Title}",
                Type = ChatRoomType.Task,
                WorkTaskId = taskId,
                CreatedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.ChatRooms.Add(room);
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (!room.IsActive)
        {
            room.IsActive = true;
        }

        await SyncMembersAsync(room.Id, companyId, activeMemberIds, removeMissing: true, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await GetRoomInfoAsync(room.Id, userId, companyId, cancellationToken);
    }

    public async Task<ChatRoomInfo> EnsureDirectRoomAsync(
        int companyId,
        string userId1,
        string userId2,
        CancellationToken cancellationToken = default)
    {
        if (userId1 == userId2)
        {
            throw new ArgumentException("Không thể tạo phòng chat trực tiếp với chính mình.");
        }

        var users = await context.Users.AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId
                && item.IsActive
                && (item.Id == userId1 || item.Id == userId2))
            .Select(item => new { item.Id, item.FullName })
            .ToListAsync(cancellationToken);
        if (users.Count != 2)
        {
            throw new ChatAccessException("Người dùng chat trực tiếp không hợp lệ.");
        }

        var room = await context.ChatRooms
            .Where(item =>
                item.CompanyId == companyId
                && item.Type == ChatRoomType.Direct
                && item.Members.Any(member => member.UserId == userId1)
                && item.Members.Any(member => member.UserId == userId2))
            .FirstOrDefaultAsync(
                item => item.Members.Count == 2,
                cancellationToken);
        if (room is null)
        {
            room = new ChatRoom
            {
                CompanyId = companyId,
                Name = string.Join(" - ", users.OrderBy(item => item.FullName).Select(item => item.FullName)),
                Type = ChatRoomType.Direct,
                CreatedByUserId = userId1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.ChatRooms.Add(room);
            await context.SaveChangesAsync(cancellationToken);
        }

        await SyncMembersAsync(room.Id, companyId, [userId1, userId2], removeMissing: true, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return await GetRoomInfoAsync(room.Id, userId1, companyId, cancellationToken);
    }

    private async Task<bool> CanAccessTaskAsync(
        WorkTask task,
        string userId,
        int companyId,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == userId && item.CompanyId == companyId && item.IsActive,
                cancellationToken);
        if (user is null)
        {
            return false;
        }

        var hasBroadRole = await context.UserRoles.AsNoTracking()
            .AnyAsync(
                userRole =>
                    userRole.UserId == userId
                    && context.Roles.Any(role =>
                        role.Id == userRole.RoleId
                        && (role.Name == AppRoles.Director || role.Name == AppRoles.CompanyAdmin)),
                cancellationToken);
        if (hasBroadRole || task.CreatedByUserId == userId)
        {
            return true;
        }

        return await context.TaskAssignments.AsNoTracking()
            .AnyAsync(
                item =>
                    item.CompanyId == companyId
                    && item.WorkTaskId == task.Id
                    && (item.AssignedToUserId == userId
                        || (user.DepartmentId.HasValue
                            && item.AssignedToDepartmentId == user.DepartmentId.Value)),
                cancellationToken)
            || (user.DepartmentId.HasValue && task.AssignedDepartmentId == user.DepartmentId.Value);
    }

    private async Task<ChatRoomMember> GetMembershipAsync(
        int roomId,
        string userId,
        int companyId,
        CancellationToken cancellationToken)
    {
        var membership = await context.ChatRoomMembers
            .Include(item => item.ChatRoom)
            .FirstOrDefaultAsync(
                item =>
                    item.ChatRoomId == roomId
                    && item.CompanyId == companyId
                    && item.UserId == userId
                    && item.ChatRoom.CompanyId == companyId
                    && item.ChatRoom.IsActive,
                cancellationToken);

        if (membership is null)
        {
            throw new ChatAccessException("Bạn không có quyền truy cập phòng chat này.");
        }

        if (membership.ChatRoom.Type == ChatRoomType.Department)
        {
            var stillInDepartment = await context.Users.AsNoTracking()
                .AnyAsync(
                    item =>
                        item.Id == userId
                        && item.CompanyId == companyId
                        && item.IsActive
                        && item.DepartmentId == membership.ChatRoom.DepartmentId,
                    cancellationToken);
            if (!stillInDepartment)
            {
                throw new ChatAccessException("Bạn không có quyền truy cập phòng chat này.");
            }
        }
        else if (membership.ChatRoom.Type == ChatRoomType.Task
                 && membership.ChatRoom.WorkTaskId.HasValue)
        {
            var task = await context.WorkTasks.AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == membership.ChatRoom.WorkTaskId.Value
                        && item.CompanyId == companyId,
                    cancellationToken);
            if (task is null || !await CanAccessTaskAsync(task, userId, companyId, cancellationToken))
            {
                throw new ChatAccessException("Bạn không có quyền truy cập phòng chat này.");
            }
        }

        return membership;
    }

    private async Task SyncMembersAsync(
        int roomId,
        int companyId,
        IEnumerable<string> expectedUserIds,
        bool removeMissing,
        CancellationToken cancellationToken)
    {
        var expected = expectedUserIds.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var existing = await context.ChatRoomMembers
            .Where(item => item.ChatRoomId == roomId && item.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        var existingIds = existing.Select(item => item.UserId).ToHashSet(StringComparer.Ordinal);
        var now = DateTime.UtcNow;

        context.ChatRoomMembers.AddRange(expected
            .Where(userId => !existingIds.Contains(userId))
            .Select(userId => new ChatRoomMember
            {
                CompanyId = companyId,
                ChatRoomId = roomId,
                UserId = userId,
                JoinedAt = now,
                LastReadAt = null
            }));
        if (removeMissing)
        {
            context.ChatRoomMembers.RemoveRange(existing.Where(item => !expected.Contains(item.UserId)));
        }
    }

    private async Task<ChatRoomInfo> GetRoomInfoAsync(
        int roomId,
        string userId,
        int companyId,
        CancellationToken cancellationToken)
    {
        var rooms = await GetRoomsForUserAsync(userId, companyId, cancellationToken);
        return rooms.FirstOrDefault(item => item.Id == roomId)
            ?? throw new ChatAccessException("Bạn không có quyền truy cập phòng chat này.");
    }

    private async Task<Dictionary<string, string>> GetUserNamesAsync(
        int companyId,
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct(StringComparer.Ordinal).ToList();
        return await context.Users.AsNoTracking()
            .Where(item => item.CompanyId == companyId && ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.FullName, cancellationToken);
    }

    private static ChatMessageInfo ToMessageInfo(ChatMessage message, string? senderName) =>
        new()
        {
            Id = message.Id,
            RoomId = message.ChatRoomId,
            Content = message.Content,
            SenderId = message.SenderUserId,
            SenderName = senderName ?? "Người dùng",
            CreatedAt = message.CreatedAt
        };
}
