using EnterpriseTask.Api.DTOs.Mobile.Chat;
using EnterpriseTask.Api.Services;
using EnterpriseTask.Application.Chat;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/mobile/chat")]
public class MobileChatController(
    MobileWorkspaceAccessService accessService,
    IChatService chatService) : ControllerBase
{
    [HttpGet("rooms")]
    public async Task<ActionResult<ApiResponse<List<MobileChatRoomDto>>>> Rooms(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<List<MobileChatRoomDto>>(access);
        }

        var workspace = access.Workspace!;
        if (workspace.Department is not null)
        {
            await chatService.EnsureDepartmentRoomAsync(
                workspace.Company.Id,
                workspace.Department.Id,
                workspace.User.Id,
                cancellationToken);
        }

        var rooms = await chatService.GetRoomsForUserAsync(
            workspace.User.Id,
            workspace.Company.Id,
            cancellationToken);
        return Ok(ApiResponse<List<MobileChatRoomDto>>.Succeeded(
            rooms.Select(ToRoomDto).ToList(),
            "Đã tải danh sách phòng chat."));
    }

    [HttpGet("rooms/{roomId:int}/messages")]
    public async Task<ActionResult<ApiResponse<List<MobileChatMessageDto>>>> Messages(
        int roomId,
        int page = 1,
        int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<List<MobileChatMessageDto>>(access);
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest(ApiResponse<List<MobileChatMessageDto>>.Failed(
                "Page phải lớn hơn 0 và pageSize phải từ 1 đến 100."));
        }

        var workspace = access.Workspace!;
        try
        {
            var messages = await chatService.GetMessagesAsync(
                roomId,
                workspace.User.Id,
                workspace.Company.Id,
                page,
                pageSize,
                cancellationToken);
            return Ok(ApiResponse<List<MobileChatMessageDto>>.Succeeded(
                messages.Select(item => ToMessageDto(item, workspace.User.Id)).ToList(),
                "Đã tải tin nhắn."));
        }
        catch (ChatAccessException exception)
        {
            return NotFound(ApiResponse<List<MobileChatMessageDto>>.Failed(exception.Message));
        }
    }

    [HttpPost("rooms/{roomId:int}/messages")]
    public async Task<ActionResult<ApiResponse<MobileChatMessageDto>>> SendMessage(
        int roomId,
        SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileChatMessageDto>(access);
        }

        var workspace = access.Workspace!;
        try
        {
            var message = await chatService.SendMessageAsync(
                roomId,
                workspace.User.Id,
                workspace.Company.Id,
                request.Content,
                cancellationToken);
            return Ok(ApiResponse<MobileChatMessageDto>.Succeeded(
                ToMessageDto(message, workspace.User.Id),
                "Đã gửi tin nhắn."));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<MobileChatMessageDto>.Failed(exception.Message));
        }
        catch (ChatAccessException exception)
        {
            return NotFound(ApiResponse<MobileChatMessageDto>.Failed(exception.Message));
        }
    }

    [HttpPost("department-room")]
    public async Task<ActionResult<ApiResponse<MobileChatRoomDto>>> DepartmentRoom(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileChatRoomDto>(access);
        }

        var workspace = access.Workspace!;
        if (workspace.Department is null)
        {
            return BadRequest(ApiResponse<MobileChatRoomDto>.Failed(
                "Tài khoản chưa được gán phòng ban."));
        }

        try
        {
            var room = await chatService.EnsureDepartmentRoomAsync(
                workspace.Company.Id,
                workspace.Department.Id,
                workspace.User.Id,
                cancellationToken);
            return Ok(ApiResponse<MobileChatRoomDto>.Succeeded(
                ToRoomDto(room),
                "Đã tạo hoặc tải phòng chat phòng ban."));
        }
        catch (ChatAccessException exception)
        {
            return NotFound(ApiResponse<MobileChatRoomDto>.Failed(exception.Message));
        }
    }

    [HttpPost("task-room/{taskId:int}")]
    public async Task<ActionResult<ApiResponse<MobileChatRoomDto>>> TaskRoom(
        int taskId,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileChatRoomDto>(access);
        }

        var workspace = access.Workspace!;
        try
        {
            var room = await chatService.EnsureTaskRoomAsync(
                workspace.Company.Id,
                taskId,
                workspace.User.Id,
                cancellationToken);
            return Ok(ApiResponse<MobileChatRoomDto>.Succeeded(
                ToRoomDto(room),
                "Đã tạo hoặc tải phòng chat công việc."));
        }
        catch (ChatAccessException exception)
        {
            return NotFound(ApiResponse<MobileChatRoomDto>.Failed(exception.Message));
        }
    }

    [HttpPost("direct-room/{userId}")]
    public async Task<ActionResult<ApiResponse<MobileChatRoomDto>>> DirectRoom(
        string userId,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileChatRoomDto>(access);
        }

        var workspace = access.Workspace!;
        try
        {
            var room = await chatService.EnsureDirectRoomAsync(
                workspace.Company.Id,
                workspace.User.Id,
                userId,
                cancellationToken);
            return Ok(ApiResponse<MobileChatRoomDto>.Succeeded(
                ToRoomDto(room),
                "Đã tạo hoặc tải phòng chat trực tiếp."));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse<MobileChatRoomDto>.Failed(exception.Message));
        }
        catch (ChatAccessException exception)
        {
            return NotFound(ApiResponse<MobileChatRoomDto>.Failed(exception.Message));
        }
    }

    private ObjectResult Failure<T>(MobileWorkspaceAccessResult access) =>
        StatusCode(access.StatusCode, ApiResponse<T>.Failed(access.Message));

    private static MobileChatRoomDto ToRoomDto(ChatRoomInfo room) =>
        new()
        {
            Id = room.Id,
            Name = room.Name,
            Type = room.Type,
            DepartmentId = room.DepartmentId,
            TaskId = room.TaskId,
            LastMessage = room.LastMessage,
            LastMessageAt = room.LastMessageAt,
            UnreadCount = room.UnreadCount
        };

    private static MobileChatMessageDto ToMessageDto(ChatMessageInfo message, string currentUserId) =>
        new()
        {
            Id = message.Id,
            RoomId = message.RoomId,
            Content = message.Content,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            CreatedAt = message.CreatedAt,
            IsMine = message.SenderId == currentUserId
        };
}
