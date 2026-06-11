using EnterpriseTask.Api.DTOs.Mobile.AI;
using EnterpriseTask.Api.Services;
using EnterpriseTask.Application.AI;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Interfaces;
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
[Route("api/mobile/ai")]
public class MobileAiController(
    AppDbContext context,
    MobileWorkspaceAccessService accessService,
    IAiTaskService aiTaskService) : ControllerBase
{
    private static readonly string[] CandidateRoles =
    [
        AppRoles.Director,
        AppRoles.DepartmentManager,
        AppRoles.Employee
    ];

    [HttpPost("tasks/suggest-assignment")]
    public async Task<ActionResult<ApiResponse<AiAssignmentSuggestionResult>>> SuggestAssignment(
        SuggestAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<AiAssignmentSuggestionResult>(access);
        }

        Normalize(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ApiResponse<AiAssignmentSuggestionResult>.Failed(
                "Tiêu đề công việc không được để trống."));
        }

        var workspace = access.Workspace!;
        var candidates = await BuildAssignmentCandidatesAsync(
            workspace.Company.Id,
            cancellationToken);
        var result = await aiTaskService.SuggestAssignmentAsync(
            new AiAssignmentSuggestionRequest
            {
                CompanyId = workspace.Company.Id,
                TaskTitle = request.Title,
                TaskDescription = request.Description,
                DueDate = request.DueDate,
                CandidateUsers = candidates.Users,
                CandidateDepartments = candidates.Departments
            },
            cancellationToken);

        await SaveSuggestionAsync(
            workspace,
            workTaskId: null,
            $"Assignment | {request.Title}\n{request.Description}",
            summary: $"{result.SuggestedTargetType}: {result.SuggestedName}",
            result.Reason,
            result.ConfidenceScore,
            result.SuggestedUserId,
            result.SuggestedDepartmentId,
            cancellationToken);

        return Ok(ApiResponse<AiAssignmentSuggestionResult>.Succeeded(
            result,
            "Đã tạo gợi ý giao công việc."));
    }

    [HttpPost("tasks/summarize")]
    public async Task<ActionResult<ApiResponse<AiTaskSummaryResult>>> Summarize(
        SummarizeTaskRequest request,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<AiTaskSummaryResult>(access);
        }

        var workspace = access.Workspace!;
        WorkTask? task = null;
        var title = NormalizeOptional(request.Title);
        var description = NormalizeOptional(request.Description);
        var comments = (request.Comments ?? [])
            .Select(NormalizeOptional)
            .Where(item => item is not null)
            .Cast<string>()
            .Take(50)
            .ToList();

        if (request.TaskId.HasValue)
        {
            task = await BuildTaskScope(workspace)
                .FirstOrDefaultAsync(item => item.Id == request.TaskId.Value, cancellationToken);
            if (task is null)
            {
                return NotFound(ApiResponse<AiTaskSummaryResult>.Failed(
                    "Không tìm thấy công việc."));
            }

            title = task.Title;
            description = task.Description;
            comments = await context.TaskComments.AsNoTracking()
                .Where(item =>
                    item.CompanyId == workspace.Company.Id
                    && item.WorkTaskId == task.Id)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => item.Content)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(ApiResponse<AiTaskSummaryResult>.Failed(
                "Cần cung cấp taskId hợp lệ hoặc tiêu đề công việc."));
        }

        var result = await aiTaskService.SummarizeTaskAsync(
            new AiTaskSummaryRequest
            {
                Title = title,
                Description = description,
                Comments = comments
            },
            cancellationToken);

        await SaveSuggestionAsync(
            workspace,
            task?.Id,
            $"Summary | {title}\n{description}",
            result.Summary,
            $"Key points: {string.Join("; ", result.KeyPoints)}",
            score: null,
            suggestedUserId: null,
            suggestedDepartmentId: null,
            cancellationToken);

        return Ok(ApiResponse<AiTaskSummaryResult>.Succeeded(
            result,
            "Đã tóm tắt công việc."));
    }

    [HttpPost("tasks/suggest-priority")]
    public async Task<ActionResult<ApiResponse<AiPrioritySuggestionResult>>> SuggestPriority(
        SuggestPriorityRequest request,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<AiPrioritySuggestionResult>(access);
        }

        Normalize(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ApiResponse<AiPrioritySuggestionResult>.Failed(
                "Tiêu đề công việc không được để trống."));
        }

        var workspace = access.Workspace!;
        var result = await aiTaskService.SuggestPriorityAsync(
            new AiPrioritySuggestionRequest
            {
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate
            },
            cancellationToken);

        await SaveSuggestionAsync(
            workspace,
            workTaskId: null,
            $"Priority | {request.Title}\n{request.Description}",
            $"Suggested priority: {result.SuggestedPriority}",
            result.Reason,
            score: null,
            suggestedUserId: null,
            suggestedDepartmentId: null,
            cancellationToken);

        return Ok(ApiResponse<AiPrioritySuggestionResult>.Succeeded(
            result,
            "Đã gợi ý mức độ ưu tiên."));
    }

    [HttpPost("tasks/{taskId:int}/evaluate-progress")]
    public async Task<ActionResult<ApiResponse<AiProgressEvaluationResult>>> EvaluateProgress(
        int taskId,
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<AiProgressEvaluationResult>(access);
        }

        var workspace = access.Workspace!;
        var task = await BuildTaskScope(workspace)
            .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        if (task is null)
        {
            return NotFound(ApiResponse<AiProgressEvaluationResult>.Failed(
                "Không tìm thấy công việc."));
        }

        var histories = await context.TaskStatusHistories.AsNoTracking()
            .Where(item =>
                item.CompanyId == workspace.Company.Id
                && item.WorkTaskId == task.Id)
            .OrderBy(item => item.ChangedAt)
            .Select(item => $"{item.FromStatus} -> {item.ToStatus}: {item.Note}")
            .Take(100)
            .ToListAsync(cancellationToken);
        var comments = await context.TaskComments.AsNoTracking()
            .Where(item =>
                item.CompanyId == workspace.Company.Id
                && item.WorkTaskId == task.Id)
            .OrderBy(item => item.CreatedAt)
            .Select(item => item.Content)
            .Take(100)
            .ToListAsync(cancellationToken);

        var result = await aiTaskService.EvaluateProgressAsync(
            new AiProgressEvaluationRequest
            {
                Title = task.Title,
                Status = task.Status.ToString(),
                DueDate = task.DueDate,
                StatusHistories = histories,
                Comments = comments
            },
            cancellationToken);

        await SaveSuggestionAsync(
            workspace,
            task.Id,
            $"Progress | {task.Title} | {task.Status} | Due: {task.DueDate:O}",
            result.Evaluation,
            result.Recommendation,
            result.IsAtRisk ? 1.00m : 0.00m,
            suggestedUserId: null,
            suggestedDepartmentId: null,
            cancellationToken);

        return Ok(ApiResponse<AiProgressEvaluationResult>.Succeeded(
            result,
            "Đã đánh giá tiến độ công việc."));
    }

    private async Task<AssignmentCandidates> BuildAssignmentCandidatesAsync(
        int companyId,
        CancellationToken cancellationToken)
    {
        var candidateUserIds = await context.UserRoles.AsNoTracking()
            .Where(userRole =>
                context.Users.Any(user =>
                    user.Id == userRole.UserId
                    && user.CompanyId == companyId
                    && user.IsActive)
                && context.Roles.Any(role =>
                    role.Id == userRole.RoleId
                    && role.Name != null
                    && CandidateRoles.Contains(role.Name))
                && !context.UserRoles.Any(protectedRole =>
                    protectedRole.UserId == userRole.UserId
                    && context.Roles.Any(role =>
                        role.Id == protectedRole.RoleId
                        && (role.Name == AppRoles.SystemAdmin
                            || role.Name == AppRoles.CompanyAdmin))))
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var users = await context.Users.AsNoTracking()
            .Where(user =>
                user.CompanyId == companyId
                && user.IsActive
                && candidateUserIds.Contains(user.Id))
            .ToListAsync(cancellationToken);
        var departments = await context.Departments.AsNoTracking()
            .Where(department => department.CompanyId == companyId && department.IsActive)
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);
        var departmentNames = departments.ToDictionary(item => item.Id, item => item.Name);
        var roleRows = await context.UserRoles.AsNoTracking()
            .Where(userRole =>
                candidateUserIds.Contains(userRole.UserId)
                && context.Roles.Any(role =>
                    role.Id == userRole.RoleId
                    && role.Name != null
                    && CandidateRoles.Contains(role.Name)))
            .Select(userRole => new
            {
                userRole.UserId,
                Role = context.Roles
                    .Where(role => role.Id == userRole.RoleId)
                    .Select(role => role.Name!)
                    .First()
            })
            .ToListAsync(cancellationToken);
        var rolesByUser = roleRows
            .GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => group.First().Role);

        var tasks = await context.WorkTasks.AsNoTracking()
            .Where(task => task.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        var taskIds = tasks.Select(item => item.Id).ToList();
        var assignments = await context.TaskAssignments.AsNoTracking()
            .Where(item => item.CompanyId == companyId && taskIds.Contains(item.WorkTaskId))
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        var latestAssignments = assignments
            .GroupBy(item => item.WorkTaskId)
            .ToDictionary(group => group.Key, group => group.First());
        var loads = tasks.Select(task =>
        {
            latestAssignments.TryGetValue(task.Id, out var assignment);
            return new TaskLoad(
                task,
                assignment?.AssignedToUserId,
                assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId);
        }).ToList();
        var now = DateTime.UtcNow;

        var candidateUsers = users.Select(user =>
        {
            var assigned = loads.Where(load => load.UserId == user.Id).ToList();
            return new AiCandidateUserDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = rolesByUser.GetValueOrDefault(user.Id, AppRoles.Employee),
                DepartmentName = user.DepartmentId.HasValue
                    ? departmentNames.GetValueOrDefault(user.DepartmentId.Value)
                    : null,
                Position = user.Position,
                ActiveTaskCount = assigned.Count(load => IsActive(load.Task)),
                DoneTaskCount = assigned.Count(load => load.Task.Status == WorkTaskStatus.Done),
                OverdueTaskCount = assigned.Count(load => IsOverdue(load.Task, now))
            };
        }).ToList();
        var candidateDepartments = departments.Select(department =>
        {
            var assigned = loads.Where(load => load.DepartmentId == department.Id).ToList();
            return new AiCandidateDepartmentDto
            {
                DepartmentId = department.Id,
                DepartmentName = department.Name,
                MemberCount = users.Count(user => user.DepartmentId == department.Id),
                ActiveTaskCount = assigned.Count(load => IsActive(load.Task)),
                DoneTaskCount = assigned.Count(load => load.Task.Status == WorkTaskStatus.Done),
                OverdueTaskCount = assigned.Count(load => IsOverdue(load.Task, now))
            };
        }).ToList();

        return new AssignmentCandidates(candidateUsers, candidateDepartments);
    }

    private IQueryable<WorkTask> BuildTaskScope(MobileWorkspaceContext workspace)
    {
        var query = context.WorkTasks.AsNoTracking()
            .Where(task => task.CompanyId == workspace.Company.Id);

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

    private async Task SaveSuggestionAsync(
        MobileWorkspaceContext workspace,
        int? workTaskId,
        string inputText,
        string? summary,
        string? reason,
        decimal? score,
        string? suggestedUserId,
        int? suggestedDepartmentId,
        CancellationToken cancellationToken)
    {
        context.AiTaskSuggestions.Add(new AiTaskSuggestion
        {
            CompanyId = workspace.Company.Id,
            WorkTaskId = workTaskId,
            RequestedByUserId = workspace.User.Id,
            InputText = Truncate(inputText, 4000),
            SuggestedDepartmentId = suggestedDepartmentId,
            SuggestedUserId = suggestedUserId,
            Summary = TruncateOptional(summary, 2000),
            Reason = TruncateOptional(reason, 2000),
            Score = score,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private ObjectResult Failure<T>(MobileWorkspaceAccessResult access) =>
        StatusCode(access.StatusCode, ApiResponse<T>.Failed(access.Message));

    private static bool IsActive(WorkTask task) =>
        task.Status != WorkTaskStatus.Done && task.Status != WorkTaskStatus.Cancelled;

    private static bool IsOverdue(WorkTask task, DateTime now) =>
        task.Status == WorkTaskStatus.Overdue
        || (task.DueDate.HasValue && task.DueDate.Value < now && IsActive(task));

    private static void Normalize(SuggestAssignmentRequest request)
    {
        request.Title = request.Title.Trim();
        request.Description = NormalizeOptional(request.Description);
    }

    private static void Normalize(SuggestPriorityRequest request)
    {
        request.Title = request.Title.Trim();
        request.Description = NormalizeOptional(request.Description);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? TruncateOptional(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Truncate(value, maxLength);

    private sealed record TaskLoad(WorkTask Task, string? UserId, int? DepartmentId);

    private sealed record AssignmentCandidates(
        List<AiCandidateUserDto> Users,
        List<AiCandidateDepartmentDto> Departments);
}
