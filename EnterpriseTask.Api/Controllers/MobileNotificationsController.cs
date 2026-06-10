using EnterpriseTask.Api.DTOs.Mobile.Notifications;
using EnterpriseTask.Api.Services;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/mobile/notifications")]
public class MobileNotificationsController(
    AppDbContext context,
    MobileWorkspaceAccessService accessService,
    INotificationService notificationService,
    INotificationRealtimeSender realtimeSender,
    ILogger<MobileNotificationsController> logger) : ControllerBase
{
    [HttpGet("")]
    public async Task<ActionResult<ApiResponse<List<MobileNotificationDto>>>> Index(
        bool? unreadOnly,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<List<MobileNotificationDto>>(access);
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest(ApiResponse<List<MobileNotificationDto>>.Failed(
                "Page phải lớn hơn 0 và pageSize phải từ 1 đến 100."));
        }

        var workspace = access.Workspace!;
        var query = context.Notifications.AsNoTracking()
            .Where(item =>
                item.CompanyId == workspace.Company.Id
                && item.UserId == workspace.User.Id);
        if (unreadOnly == true)
        {
            query = query.Where(item => !item.IsRead);
        }

        var notifications = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var response = notifications.Select(ToDto).ToList();

        return Ok(ApiResponse<List<MobileNotificationDto>>.Succeeded(
            response,
            "Đã tải danh sách thông báo."));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadNotificationCountDto>>> UnreadCount(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<UnreadNotificationCountDto>(access);
        }

        var workspace = access.Workspace!;
        var response = new UnreadNotificationCountDto
        {
            Count = await notificationService.GetUnreadCountAsync(
                workspace.User.Id,
                workspace.Company.Id,
                cancellationToken)
        };
        return Ok(ApiResponse<UnreadNotificationCountDto>.Succeeded(
            response,
            "Đã tải số thông báo chưa đọc."));
    }

    [HttpPost("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<MobileNotificationDto>>> Read(
        int id,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileNotificationDto>(access);
        }

        var workspace = access.Workspace!;
        var notification = await context.Notifications.FirstOrDefaultAsync(
            item =>
                item.Id == id
                && item.CompanyId == workspace.Company.Id
                && item.UserId == workspace.User.Id,
            cancellationToken);
        if (notification is null)
        {
            return NotFound(ApiResponse<MobileNotificationDto>.Failed(
                "Không tìm thấy thông báo."));
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            await TrySendUnreadCountAsync(workspace.User.Id, workspace.Company.Id, cancellationToken);
        }

        return Ok(ApiResponse<MobileNotificationDto>.Succeeded(
            ToDto(notification),
            "Đã đánh dấu thông báo là đã đọc."));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object?>>> ReadAll(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<object?>(access);
        }

        var workspace = access.Workspace!;
        var readAt = DateTime.UtcNow;
        await context.Notifications
            .Where(item =>
                item.CompanyId == workspace.Company.Id
                && item.UserId == workspace.User.Id
                && !item.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.IsRead, true)
                    .SetProperty(item => item.ReadAt, readAt),
                cancellationToken);
        await TrySendUnreadCountAsync(
            workspace.User.Id,
            workspace.Company.Id,
            cancellationToken,
            knownUnreadCount: 0);

        return Ok(ApiResponse<object?>.Succeeded(
            null,
            "Đã đánh dấu tất cả thông báo là đã đọc."));
    }

    private ObjectResult Failure<T>(MobileWorkspaceAccessResult access) =>
        StatusCode(access.StatusCode, ApiResponse<T>.Failed(access.Message));

    private async Task TrySendUnreadCountAsync(
        string userId,
        int companyId,
        CancellationToken cancellationToken,
        int? knownUnreadCount = null)
    {
        try
        {
            var unreadCount = knownUnreadCount
                ?? await notificationService.GetUnreadCountAsync(userId, companyId, cancellationToken);
            await realtimeSender.SendUnreadCountAsync(userId, unreadCount, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Notification read state was saved, but realtime unread count delivery failed for user {UserId}.",
                userId);
        }
    }

    private static MobileNotificationDto ToDto(Notification notification) =>
        new()
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            RelatedEntityType = notification.RelatedTaskId.HasValue ? "Task" : null,
            RelatedEntityId = notification.RelatedTaskId
        };
}
