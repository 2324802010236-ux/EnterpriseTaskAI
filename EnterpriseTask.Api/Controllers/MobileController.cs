using EnterpriseTask.Api.DTOs.Mobile;
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
[Route("api/mobile")]
public class MobileController(
    AppDbContext context,
    MobileWorkspaceAccessService accessService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<MobileCurrentUserDto>>> Me(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileCurrentUserDto>(access);
        }

        var workspace = access.Workspace!;
        var response = new MobileCurrentUserDto
        {
            UserId = workspace.User.Id,
            FullName = workspace.User.FullName,
            Email = workspace.User.Email ?? string.Empty,
            Role = workspace.Role,
            CompanyId = workspace.Company.Id,
            CompanyName = workspace.Company.Name,
            DepartmentId = workspace.Department?.Id,
            DepartmentName = workspace.Department?.Name,
            Position = workspace.User.Position,
            IsActive = workspace.User.IsActive
        };

        return Ok(ApiResponse<MobileCurrentUserDto>.Succeeded(
            response,
            "Đã tải thông tin người dùng hiện tại."));
    }

    [HttpGet("company")]
    public async Task<ActionResult<ApiResponse<MobileCompanyDto>>> Company(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileCompanyDto>(access);
        }

        var company = access.Workspace!.Company;
        var response = new MobileCompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Email = company.Email,
            Phone = company.Phone,
            Address = company.Address,
            Industry = company.Industry,
            Status = company.Status.ToString()
        };

        return Ok(ApiResponse<MobileCompanyDto>.Succeeded(
            response,
            "Đã tải thông tin công ty."));
    }

    [HttpGet("my-department")]
    public async Task<ActionResult<ApiResponse<MobileDepartmentDto?>>> MyDepartment(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileDepartmentDto?>(access);
        }

        var department = access.Workspace!.Department;
        if (department is null)
        {
            return Ok(ApiResponse<MobileDepartmentDto?>.Succeeded(
                null,
                "Tài khoản chưa được gán phòng ban."));
        }

        var response = new MobileDepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Code = null,
            Description = department.Description,
            IsActive = department.IsActive
        };

        return Ok(ApiResponse<MobileDepartmentDto?>.Succeeded(
            response,
            "Đã tải thông tin phòng ban."));
    }

    [HttpGet("features")]
    public async Task<ActionResult<ApiResponse<List<RoleFeatureDto>>>> Features(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<List<RoleFeatureDto>>(access);
        }

        var response = GetFeatures(access.Workspace!.Role);
        return Ok(ApiResponse<List<RoleFeatureDto>>.Succeeded(
            response,
            "Đã tải danh sách chức năng theo vai trò."));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<MobileDashboardDto>>> Dashboard(
        CancellationToken cancellationToken)
    {
        var access = await accessService.CheckAccessAsync(cancellationToken);
        if (!access.IsAllowed)
        {
            return Failure<MobileDashboardDto>(access);
        }

        var workspace = access.Workspace!;
        var tasks = BuildTaskScope(workspace);
        var today = DateTime.UtcNow.Date;
        var counts = await tasks
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                InProgress = group.Count(task => task.Status == WorkTaskStatus.InProgress),
                Done = group.Count(task => task.Status == WorkTaskStatus.Done),
                Overdue = group.Count(task =>
                    task.Status == WorkTaskStatus.Overdue
                    || (task.DueDate.HasValue
                        && task.DueDate.Value < today
                        && task.Status != WorkTaskStatus.Done
                        && task.Status != WorkTaskStatus.Cancelled))
            })
            .FirstOrDefaultAsync(cancellationToken);

        var notificationCount = await context.Notifications.AsNoTracking()
            .CountAsync(
                item =>
                    item.CompanyId == workspace.Company.Id
                    && item.UserId == workspace.User.Id
                    && !item.IsRead,
                cancellationToken);
        var departmentMemberCount = workspace.Department is null
            ? 0
            : await context.Users.AsNoTracking()
                .CountAsync(
                    item =>
                        item.CompanyId == workspace.Company.Id
                        && item.DepartmentId == workspace.Department.Id
                        && item.IsActive,
                    cancellationToken);

        var response = new MobileDashboardDto
        {
            Role = workspace.Role,
            MyTaskCount = counts?.Total ?? 0,
            InProgressTaskCount = counts?.InProgress ?? 0,
            DoneTaskCount = counts?.Done ?? 0,
            OverdueTaskCount = counts?.Overdue ?? 0,
            NotificationCount = notificationCount,
            DepartmentMemberCount = departmentMemberCount,
            WelcomeMessage = GetWelcomeMessage(workspace.Role)
        };

        return Ok(ApiResponse<MobileDashboardDto>.Succeeded(
            response,
            "Đã tải dashboard mobile."));
    }

    private IQueryable<WorkTask> BuildTaskScope(MobileWorkspaceContext workspace)
    {
        var query = context.WorkTasks.AsNoTracking()
            .Where(task => task.CompanyId == workspace.Company.Id);

        return workspace.Role switch
        {
            AppRoles.Director or AppRoles.CompanyAdmin => query,
            AppRoles.DepartmentManager when workspace.Department is not null =>
                query.Where(task =>
                    task.AssignedDepartmentId == workspace.Department.Id
                    || task.Assignments.Any(assignment =>
                        assignment.CompanyId == workspace.Company.Id
                        && assignment.AssignedToDepartmentId == workspace.Department.Id)),
            AppRoles.Employee =>
                query.Where(task =>
                    task.Assignments.Any(assignment =>
                        assignment.CompanyId == workspace.Company.Id
                        && assignment.AssignedToUserId == workspace.User.Id)),
            _ => query.Where(_ => false)
        };
    }

    private ObjectResult Failure<T>(MobileWorkspaceAccessResult access) =>
        StatusCode(access.StatusCode, ApiResponse<T>.Failed(access.Message));

    private static List<RoleFeatureDto> GetFeatures(string role) =>
        role switch
        {
            AppRoles.Director =>
            [
                Feature("ASSIGN_TASK", "Giao công việc", "Tạo và phân công công việc trong công ty."),
                Feature("VIEW_COMPANY_PROGRESS", "Tiến độ công ty", "Theo dõi tiến độ công việc toàn công ty."),
                Feature("CHAT_MANAGERS", "Trao đổi với quản lý", "Trao đổi với các trưởng phòng."),
                Feature("AI_SUGGEST_ASSIGNMENT", "Gợi ý phân công AI", "Nhận gợi ý phân công phù hợp từ AI.")
            ],
            AppRoles.DepartmentManager =>
            [
                Feature("VIEW_DEPARTMENT_TASKS", "Công việc phòng ban", "Theo dõi công việc thuộc phòng ban."),
                Feature("ASSIGN_EMPLOYEE_TASK", "Giao việc nhân viên", "Phân công công việc cho nhân viên phòng ban."),
                Feature("CHAT_DEPARTMENT", "Trao đổi phòng ban", "Trao đổi với thành viên trong phòng ban."),
                Feature("TRACK_EMPLOYEE_PROGRESS", "Tiến độ nhân viên", "Theo dõi tiến độ của nhân viên phòng ban.")
            ],
            AppRoles.Employee =>
            [
                Feature("VIEW_MY_TASKS", "Công việc của tôi", "Theo dõi các công việc được giao."),
                Feature("UPDATE_TASK_STATUS", "Cập nhật trạng thái", "Cập nhật tiến độ và trạng thái công việc."),
                Feature("COMMENT_TASK", "Bình luận công việc", "Trao đổi trực tiếp trong công việc."),
                Feature("CHAT_DEPARTMENT", "Trao đổi phòng ban", "Trao đổi với thành viên trong phòng ban.")
            ],
            AppRoles.CompanyAdmin =>
            [
                Feature("ADMIN_VIEW_ONLY", "Xem thông tin quản trị", "Xem nhanh thông tin workspace công ty.")
            ],
            _ => []
        };

    private static RoleFeatureDto Feature(string code, string name, string description) =>
        new()
        {
            Code = code,
            Name = name,
            Description = description
        };

    private static string GetWelcomeMessage(string role) =>
        role switch
        {
            AppRoles.Director => "Chào mừng bạn quay lại. Bạn có thể theo dõi tiến độ toàn công ty.",
            AppRoles.DepartmentManager => "Chào mừng bạn quay lại. Bạn có thể quản lý công việc của phòng ban.",
            AppRoles.Employee => "Chào mừng bạn quay lại. Hãy kiểm tra các công việc được giao hôm nay.",
            AppRoles.CompanyAdmin => "Chào mừng bạn quay lại. Bạn có thể xem thông tin quản trị công ty.",
            _ => "Chào mừng bạn quay lại."
        };
}
