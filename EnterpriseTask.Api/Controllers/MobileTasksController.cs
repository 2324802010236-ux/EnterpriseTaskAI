using EnterpriseTask.Api.DTOs.Mobile.Tasks;
using EnterpriseTask.Api.Services;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/mobile/tasks")]
public class MobileTasksController(
    AppDbContext context,
    MobileWorkspaceAccessService accessService) : ControllerBase
{
    [HttpGet("")]
    public async Task<ActionResult<ApiResponse<List<MobileTaskListItemDto>>>> Index(
        string? search,
        WorkTaskStatus? status,
        WorkTaskPriority? priority,
        DateTime? dueFrom,
        DateTime? dueTo,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<List<MobileTaskListItemDto>>(access);
        }

        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            return BadRequest(ApiResponse<List<MobileTaskListItemDto>>.Failed(
                "Trạng thái công việc không hợp lệ."));
        }

        if (priority.HasValue && !Enum.IsDefined(priority.Value))
        {
            return BadRequest(ApiResponse<List<MobileTaskListItemDto>>.Failed(
                "Độ ưu tiên không hợp lệ."));
        }

        if (dueFrom.HasValue && dueTo.HasValue && dueFrom.Value.Date > dueTo.Value.Date)
        {
            return BadRequest(ApiResponse<List<MobileTaskListItemDto>>.Failed(
                "Ngày bắt đầu không được lớn hơn ngày kết thúc."));
        }

        var workspace = access.Workspace!;
        var query = BuildTaskScope(workspace, asNoTracking: true);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(task =>
                task.Title.Contains(keyword)
                || (task.Description != null && task.Description.Contains(keyword)));
        }

        if (status.HasValue)
        {
            query = query.Where(task => task.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(task => task.Priority == priority.Value);
        }

        if (dueFrom.HasValue)
        {
            var start = dueFrom.Value.Date;
            query = query.Where(task => task.DueDate.HasValue && task.DueDate.Value >= start);
        }

        if (dueTo.HasValue)
        {
            var endExclusive = dueTo.Value.Date.AddDays(1);
            query = query.Where(task => task.DueDate.HasValue && task.DueDate.Value < endExclusive);
        }

        var tasks = await query
            .OrderByDescending(task => task.CreatedAt)
            .ToListAsync(cancellationToken);
        var assignments = await GetLatestAssignmentsAsync(
            workspace.Company.Id,
            tasks.Select(task => task.Id),
            cancellationToken);
        var userNames = await GetUserNamesAsync(
            workspace.Company.Id,
            assignments.Values
                .Where(assignment => assignment.AssignedToUserId != null)
                .Select(assignment => assignment.AssignedToUserId!),
            cancellationToken);
        var departmentNames = await GetDepartmentNamesAsync(
            workspace.Company.Id,
            cancellationToken);
        var today = DateTime.UtcNow.Date;

        var response = tasks.Select(task =>
        {
            assignments.TryGetValue(task.Id, out var assignment);
            var assignmentInfo = ResolveAssignment(
                task,
                assignment,
                userNames,
                departmentNames);

            return new MobileTaskListItemDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                DueDate = task.DueDate,
                AssignmentTarget = assignmentInfo.Target,
                AssignedUserName = assignmentInfo.UserName,
                DepartmentName = assignmentInfo.DepartmentName,
                CreatedAt = task.CreatedAt,
                IsOverdue = IsOverdue(task, today)
            };
        }).ToList();

        return Ok(ApiResponse<List<MobileTaskListItemDto>>.Succeeded(
            response,
            "Đã tải danh sách công việc."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<MobileTaskDetailsDto>>> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileTaskDetailsDto>(access);
        }

        var response = await BuildDetailsAsync(access.Workspace!, id, cancellationToken);
        return response is null
            ? NotFound(ApiResponse<MobileTaskDetailsDto>.Failed("Không tìm thấy công việc."))
            : Ok(ApiResponse<MobileTaskDetailsDto>.Succeeded(response, "Đã tải chi tiết công việc."));
    }

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<MobileTaskDetailsDto>>> UpdateStatus(
        int id,
        UpdateMobileTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileTaskDetailsDto>(access);
        }

        if (!Enum.IsDefined(request.Status))
        {
            return BadRequest(ApiResponse<MobileTaskDetailsDto>.Failed(
                "Trạng thái công việc không hợp lệ."));
        }

        var workspace = access.Workspace!;
        var task = await BuildTaskScope(workspace, asNoTracking: false)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (task is null)
        {
            return NotFound(ApiResponse<MobileTaskDetailsDto>.Failed("Không tìm thấy công việc."));
        }

        if (task.Status != request.Status)
        {
            var now = DateTime.UtcNow;
            var previousStatus = task.Status;
            task.Status = request.Status;
            task.CompletedAt = request.Status == WorkTaskStatus.Done ? now : null;
            task.UpdatedAt = now;
            context.TaskStatusHistories.Add(new TaskStatusHistory
            {
                CompanyId = workspace.Company.Id,
                WorkTaskId = task.Id,
                ChangedByUserId = workspace.User.Id,
                FromStatus = previousStatus,
                ToStatus = request.Status,
                Note = NormalizeOptional(request.Note),
                ChangedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        var response = await BuildDetailsAsync(workspace, id, cancellationToken);
        return Ok(ApiResponse<MobileTaskDetailsDto>.Succeeded(
            response!,
            "Đã cập nhật trạng thái công việc."));
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<ApiResponse<MobileTaskCommentDto>>> CreateComment(
        int id,
        CreateMobileTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileTaskCommentDto>(access);
        }

        var content = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(ApiResponse<MobileTaskCommentDto>.Failed(
                "Nội dung bình luận không được để trống."));
        }

        var workspace = access.Workspace!;
        var canViewTask = await BuildTaskScope(workspace, asNoTracking: true)
            .AnyAsync(item => item.Id == id, cancellationToken);
        if (!canViewTask)
        {
            return NotFound(ApiResponse<MobileTaskCommentDto>.Failed("Không tìm thấy công việc."));
        }

        var comment = new TaskComment
        {
            CompanyId = workspace.Company.Id,
            WorkTaskId = id,
            UserId = workspace.User.Id,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        context.TaskComments.Add(comment);
        await context.SaveChangesAsync(cancellationToken);

        var response = new MobileTaskCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            AuthorName = workspace.User.FullName,
            CreatedAt = comment.CreatedAt
        };
        return Ok(ApiResponse<MobileTaskCommentDto>.Succeeded(
            response,
            "Đã thêm bình luận công việc."));
    }

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<ApiResponse<List<MobileTaskCommentDto>>>> Comments(
        int id,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<List<MobileTaskCommentDto>>(access);
        }

        var workspace = access.Workspace!;
        if (!await CanViewTaskAsync(workspace, id, cancellationToken))
        {
            return NotFound(ApiResponse<List<MobileTaskCommentDto>>.Failed(
                "Không tìm thấy công việc."));
        }

        var response = await BuildCommentsAsync(workspace.Company.Id, id, cancellationToken);
        return Ok(ApiResponse<List<MobileTaskCommentDto>>.Succeeded(
            response,
            "Đã tải bình luận công việc."));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<ApiResponse<List<MobileTaskStatusHistoryDto>>>> History(
        int id,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<List<MobileTaskStatusHistoryDto>>(access);
        }

        var workspace = access.Workspace!;
        if (!await CanViewTaskAsync(workspace, id, cancellationToken))
        {
            return NotFound(ApiResponse<List<MobileTaskStatusHistoryDto>>.Failed(
                "Không tìm thấy công việc."));
        }

        var response = await BuildHistoriesAsync(workspace.Company.Id, id, cancellationToken);
        return Ok(ApiResponse<List<MobileTaskStatusHistoryDto>>.Succeeded(
            response,
            "Đã tải lịch sử trạng thái công việc."));
    }

    private IQueryable<WorkTask> BuildTaskScope(
        MobileWorkspaceContext workspace,
        bool asNoTracking)
    {
        IQueryable<WorkTask> query = context.WorkTasks
            .Where(task => task.CompanyId == workspace.Company.Id);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return workspace.Role switch
        {
            AppRoles.Director or AppRoles.CompanyAdmin => query,
            AppRoles.DepartmentManager or AppRoles.Employee => query.Where(task =>
                task.Assignments.Any(assignment =>
                    assignment.CompanyId == workspace.Company.Id
                    && (assignment.AssignedToUserId == workspace.User.Id
                        || (workspace.Department != null
                            && assignment.AssignedToDepartmentId == workspace.Department.Id)))
                || (workspace.Department != null
                    && task.AssignedDepartmentId == workspace.Department.Id)),
            _ => query.Where(_ => false)
        };
    }

    private async Task<bool> CanViewTaskAsync(
        MobileWorkspaceContext workspace,
        int taskId,
        CancellationToken cancellationToken) =>
        await BuildTaskScope(workspace, asNoTracking: true)
            .AnyAsync(task => task.Id == taskId, cancellationToken);

    private async Task<MobileTaskDetailsDto?> BuildDetailsAsync(
        MobileWorkspaceContext workspace,
        int taskId,
        CancellationToken cancellationToken)
    {
        var task = await BuildTaskScope(workspace, asNoTracking: true)
            .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var assignment = await GetLatestAssignmentAsync(
            workspace.Company.Id,
            task.Id,
            cancellationToken);
        var userIds = new List<string> { task.CreatedByUserId };
        if (assignment?.AssignedToUserId is not null)
        {
            userIds.Add(assignment.AssignedToUserId);
        }

        var userNames = await GetUserNamesAsync(
            workspace.Company.Id,
            userIds,
            cancellationToken);
        var departmentNames = await GetDepartmentNamesAsync(
            workspace.Company.Id,
            cancellationToken);
        var assignmentInfo = ResolveAssignment(task, assignment, userNames, departmentNames);

        return new MobileTaskDetailsDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            AssignmentTarget = assignmentInfo.Target,
            AssignedUserName = assignmentInfo.UserName,
            DepartmentName = assignmentInfo.DepartmentName,
            CreatedByName = userNames.GetValueOrDefault(task.CreatedByUserId),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            Comments = await BuildCommentsAsync(workspace.Company.Id, task.Id, cancellationToken),
            StatusHistories = await BuildHistoriesAsync(
                workspace.Company.Id,
                task.Id,
                cancellationToken)
        };
    }

    private async Task<List<MobileTaskCommentDto>> BuildCommentsAsync(
        int companyId,
        int taskId,
        CancellationToken cancellationToken)
    {
        var comments = await context.TaskComments.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.WorkTaskId == taskId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var userNames = await GetUserNamesAsync(
            companyId,
            comments.Select(item => item.UserId),
            cancellationToken);

        return comments.Select(item => new MobileTaskCommentDto
        {
            Id = item.Id,
            Content = item.Content,
            AuthorName = userNames.GetValueOrDefault(item.UserId, "Người dùng"),
            CreatedAt = item.CreatedAt
        }).ToList();
    }

    private async Task<List<MobileTaskStatusHistoryDto>> BuildHistoriesAsync(
        int companyId,
        int taskId,
        CancellationToken cancellationToken)
    {
        var histories = await context.TaskStatusHistories.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.WorkTaskId == taskId)
            .OrderByDescending(item => item.ChangedAt)
            .ToListAsync(cancellationToken);
        var userNames = await GetUserNamesAsync(
            companyId,
            histories.Select(item => item.ChangedByUserId),
            cancellationToken);

        return histories.Select(item => new MobileTaskStatusHistoryDto
        {
            Id = item.Id,
            OldStatus = item.FromStatus?.ToString(),
            NewStatus = item.ToStatus.ToString(),
            ChangedByName = userNames.GetValueOrDefault(item.ChangedByUserId),
            Note = item.Note,
            CreatedAt = item.ChangedAt
        }).ToList();
    }

    private async Task<Dictionary<int, TaskAssignment>> GetLatestAssignmentsAsync(
        int companyId,
        IEnumerable<int> taskIds,
        CancellationToken cancellationToken)
    {
        var ids = taskIds.ToList();
        var assignments = await context.TaskAssignments.AsNoTracking()
            .Where(item => item.CompanyId == companyId && ids.Contains(item.WorkTaskId))
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(item => item.WorkTaskId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<TaskAssignment?> GetLatestAssignmentAsync(
        int companyId,
        int taskId,
        CancellationToken cancellationToken) =>
        await context.TaskAssignments.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.WorkTaskId == taskId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<Dictionary<string, string>> GetUserNamesAsync(
        int companyId,
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        return await context.Users.AsNoTracking()
            .Where(item => item.CompanyId == companyId && ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.FullName, cancellationToken);
    }

    private async Task<Dictionary<int, string>> GetDepartmentNamesAsync(
        int companyId,
        CancellationToken cancellationToken) =>
        await context.Departments.AsNoTracking()
            .Where(item => item.CompanyId == companyId)
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

    private static AssignmentInfo ResolveAssignment(
        WorkTask task,
        TaskAssignment? assignment,
        IReadOnlyDictionary<string, string> userNames,
        IReadOnlyDictionary<int, string> departmentNames)
    {
        var assignedUserName = assignment?.AssignedToUserId is not null
            && userNames.TryGetValue(assignment.AssignedToUserId, out var userName)
                ? userName
                : null;
        var departmentId = assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId;
        var departmentName = departmentId.HasValue
            && departmentNames.TryGetValue(departmentId.Value, out var name)
                ? name
                : null;
        var target = assignment?.TargetType.ToString()
            ?? (departmentId.HasValue ? AssignmentTargetType.Department.ToString() : "Unassigned");

        return new AssignmentInfo(target, assignedUserName, departmentName);
    }

    private ObjectResult Failure<T>(MobileWorkspaceAccessResult access) =>
        StatusCode(access.StatusCode, ApiResponse<T>.Failed(access.Message));

    private static bool IsOverdue(WorkTask task, DateTime today) =>
        task.Status == WorkTaskStatus.Overdue
        || (task.DueDate.HasValue
            && task.DueDate.Value < today
            && task.Status != WorkTaskStatus.Done
            && task.Status != WorkTaskStatus.Cancelled);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record AssignmentInfo(
        string Target,
        string? UserName,
        string? DepartmentName);
}
