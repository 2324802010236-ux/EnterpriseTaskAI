using EnterpriseTask.Admin.ViewModels.Tasks;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Admin.Controllers;

[Authorize(Roles = AppRoles.CompanyAdmin)]
[Route("company/tasks")]
public class TasksController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager) : Controller
{
    private static readonly string[] AssignableRoles =
    [
        AppRoles.Director,
        AppRoles.DepartmentManager,
        AppRoles.Employee
    ];

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        WorkTaskStatus? status,
        WorkTaskPriority? priority,
        int? departmentId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var companyId = currentUser.CompanyId.Value;
        var query = context.WorkTasks.AsNoTracking()
            .Where(task => task.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(task =>
                task.Title.Contains(keyword)
                || (task.Description != null && task.Description.Contains(keyword)));
        }

        if (status.HasValue && Enum.IsDefined(status.Value))
        {
            query = query.Where(task => task.Status == status.Value);
        }

        if (priority.HasValue && Enum.IsDefined(priority.Value))
        {
            query = query.Where(task => task.Priority == priority.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(task =>
                task.AssignedDepartmentId == departmentId.Value
                || task.Assignments.Any(assignment =>
                    assignment.CompanyId == companyId
                    && assignment.AssignedToDepartmentId == departmentId.Value));
        }

        var tasks = await query
            .OrderByDescending(task => task.CreatedAt)
            .ToListAsync();
        var assignments = await GetLatestAssignmentsAsync(
            companyId,
            tasks.Select(task => task.Id));
        var userNames = await GetUserNamesAsync(
            companyId,
            assignments.Values
                .Where(assignment => assignment.AssignedToUserId != null)
                .Select(assignment => assignment.AssignedToUserId!));
        var departmentNames = await GetDepartmentNamesAsync(companyId);

        var model = tasks.Select(task =>
        {
            assignments.TryGetValue(task.Id, out var assignment);
            var assignedUserName = assignment?.AssignedToUserId is not null
                && userNames.TryGetValue(assignment.AssignedToUserId, out var userName)
                    ? userName
                    : null;
            var assignedDepartmentId = assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId;
            var departmentName = assignedDepartmentId.HasValue
                && departmentNames.TryGetValue(assignedDepartmentId.Value, out var name)
                    ? name
                    : null;

            return new TaskListItemViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                DueDate = task.DueDate,
                AssignmentTarget = assignment?.TargetType.ToString() ?? "Chưa phân công",
                AssignedToName = assignedUserName,
                DepartmentName = departmentName,
                CreatedAt = task.CreatedAt
            };
        }).ToList();

        ViewBag.Search = search?.Trim();
        ViewBag.Status = status;
        ViewBag.Priority = priority;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Departments = await BuildDepartmentOptionsAsync(companyId, departmentId);
        return View(model);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = new TaskFormViewModel
        {
            Priority = WorkTaskPriority.Medium,
            Status = WorkTaskStatus.Assigned,
            AssignmentTargetType = AssignmentTargetType.User
        };
        await PopulateOptionsAsync(model, currentUser.CompanyId.Value);
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskFormViewModel model)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var companyId = currentUser.CompanyId.Value;
        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateFormAsync(model, companyId);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, companyId);
            return View(model);
        }

        var now = DateTime.UtcNow;
        var task = new WorkTask
        {
            CompanyId = companyId,
            Title = model.Title,
            Description = model.Description,
            CreatedByUserId = currentUser.Id,
            AssignedDepartmentId = model.AssignmentTargetType == AssignmentTargetType.Department
                ? model.DepartmentId
                : null,
            Status = WorkTaskStatus.Assigned,
            Priority = model.Priority,
            DueDate = model.DueDate,
            CreatedAt = now
        };

        await using var transaction = await context.Database.BeginTransactionAsync();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync();

        context.TaskAssignments.Add(BuildAssignment(task.Id, companyId, currentUser.Id, model, now));
        context.TaskStatusHistories.Add(new TaskStatusHistory
        {
            CompanyId = companyId,
            WorkTaskId = task.Id,
            ChangedByUserId = currentUser.Id,
            FromStatus = null,
            ToStatus = task.Status,
            Note = "Khởi tạo và phân công công việc.",
            ChangedAt = now
        });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "Đã tạo và phân công công việc.";
        return RedirectToAction(nameof(Details), new { id = task.Id });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var companyId = currentUser.CompanyId.Value;
        var task = await context.WorkTasks.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId);
        if (task is null)
        {
            return NotFound();
        }

        var assignment = await GetLatestAssignmentAsync(id, companyId);
        var model = new TaskFormViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            Status = task.Status,
            DueDate = task.DueDate,
            AssignmentTargetType = assignment?.TargetType
                ?? (task.AssignedDepartmentId.HasValue
                    ? AssignmentTargetType.Department
                    : AssignmentTargetType.User),
            AssignedUserId = assignment?.AssignedToUserId,
            DepartmentId = assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId
        };
        await PopulateOptionsAsync(model, companyId);
        return View(model);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskFormViewModel model)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var companyId = currentUser.CompanyId.Value;
        var task = await context.WorkTasks
            .FirstOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId);
        if (task is null)
        {
            return NotFound();
        }

        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateFormAsync(model, companyId);
        if (!ModelState.IsValid)
        {
            model.Status = task.Status;
            await PopulateOptionsAsync(model, companyId);
            return View(model);
        }

        var now = DateTime.UtcNow;
        task.Title = model.Title;
        task.Description = model.Description;
        task.Priority = model.Priority;
        task.DueDate = model.DueDate;
        task.AssignedDepartmentId = model.AssignmentTargetType == AssignmentTargetType.Department
            ? model.DepartmentId
            : null;
        task.UpdatedAt = now;

        var assignment = await context.TaskAssignments
            .Where(item => item.WorkTaskId == id && item.CompanyId == companyId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync();
        if (assignment is null)
        {
            context.TaskAssignments.Add(BuildAssignment(id, companyId, currentUser.Id, model, now));
        }
        else
        {
            ApplyAssignment(assignment, currentUser.Id, model);
        }

        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật công việc.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = await BuildDetailsAsync(id, currentUser.CompanyId.Value);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("update-status/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, WorkTaskStatus status)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!Enum.IsDefined(status))
        {
            TempData["ErrorMessage"] = "Trạng thái công việc không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var task = await context.WorkTasks
            .FirstOrDefaultAsync(item =>
                item.Id == id
                && item.CompanyId == currentUser.CompanyId.Value);
        if (task is null)
        {
            return NotFound();
        }

        await ChangeStatusAsync(task, status, currentUser.Id, "Cập nhật trạng thái công việc.");
        TempData["SuccessMessage"] = "Đã cập nhật trạng thái công việc.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("cancel/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var task = await context.WorkTasks
            .FirstOrDefaultAsync(item =>
                item.Id == id
                && item.CompanyId == currentUser.CompanyId.Value);
        if (task is null)
        {
            return NotFound();
        }

        await ChangeStatusAsync(task, WorkTaskStatus.Cancelled, currentUser.Id, "Hủy công việc.");
        TempData["SuccessMessage"] = "Đã hủy công việc.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<TaskDetailsViewModel?> BuildDetailsAsync(int id, int companyId)
    {
        var task = await context.WorkTasks.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId);
        if (task is null)
        {
            return null;
        }

        var assignment = await GetLatestAssignmentAsync(id, companyId);
        var histories = await context.TaskStatusHistories.AsNoTracking()
            .Where(item => item.WorkTaskId == id && item.CompanyId == companyId)
            .OrderByDescending(item => item.ChangedAt)
            .ToListAsync();
        var comments = await context.TaskComments.AsNoTracking()
            .Where(item => item.WorkTaskId == id && item.CompanyId == companyId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
        var userIds = histories.Select(item => item.ChangedByUserId)
            .Concat(comments.Select(item => item.UserId))
            .Append(task.CreatedByUserId)
            .Concat(assignment?.AssignedToUserId is null ? [] : [assignment.AssignedToUserId])
            .Distinct()
            .ToList();
        var userNames = await GetUserNamesAsync(companyId, userIds);
        var departmentId = assignment?.AssignedToDepartmentId ?? task.AssignedDepartmentId;
        var departmentName = departmentId.HasValue
            ? await context.Departments.AsNoTracking()
                .Where(item => item.Id == departmentId.Value && item.CompanyId == companyId)
                .Select(item => item.Name)
                .FirstOrDefaultAsync()
            : null;

        return new TaskDetailsViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            DueDate = task.DueDate,
            AssignedToName = assignment?.AssignedToUserId is not null
                && userNames.TryGetValue(assignment.AssignedToUserId, out var assignedName)
                    ? assignedName
                    : null,
            DepartmentName = departmentName,
            CreatedByName = userNames.GetValueOrDefault(task.CreatedByUserId),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            StatusHistories = histories.Select(item => new TaskStatusHistoryItemViewModel
            {
                OldStatus = item.FromStatus?.ToString(),
                NewStatus = item.ToStatus.ToString(),
                ChangedByName = userNames.GetValueOrDefault(item.ChangedByUserId),
                CreatedAt = item.ChangedAt
            }).ToList(),
            Comments = comments.Select(item => new TaskCommentItemViewModel
            {
                Content = item.Content,
                AuthorName = userNames.GetValueOrDefault(item.UserId, "Người dùng"),
                CreatedAt = item.CreatedAt
            }).ToList()
        };
    }

    private async Task ValidateFormAsync(TaskFormViewModel model, int companyId)
    {
        if (!Enum.IsDefined(model.Priority))
        {
            ModelState.AddModelError(nameof(TaskFormViewModel.Priority), "Độ ưu tiên không hợp lệ.");
        }

        if (!Enum.IsDefined(model.AssignmentTargetType))
        {
            ModelState.AddModelError(
                nameof(TaskFormViewModel.AssignmentTargetType),
                "Kiểu phân công không hợp lệ.");
            return;
        }

        if (model.DueDate.HasValue && model.DueDate.Value.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(
                nameof(TaskFormViewModel.DueDate),
                "Hạn xử lý không được nhỏ hơn ngày hiện tại.");
        }

        if (model.AssignmentTargetType == AssignmentTargetType.User)
        {
            if (string.IsNullOrWhiteSpace(model.AssignedUserId)
                || !await IsAssignableUserAsync(model.AssignedUserId, companyId))
            {
                ModelState.AddModelError(
                    nameof(TaskFormViewModel.AssignedUserId),
                    "Nhân viên được chọn không hợp lệ hoặc không thuộc công ty hiện tại.");
            }
        }
        else if (!model.DepartmentId.HasValue
                 || !await context.Departments.AsNoTracking()
                     .AnyAsync(item =>
                         item.Id == model.DepartmentId.Value
                         && item.CompanyId == companyId
                         && item.IsActive))
        {
            ModelState.AddModelError(
                nameof(TaskFormViewModel.DepartmentId),
                "Phòng ban được chọn không hợp lệ hoặc không thuộc công ty hiện tại.");
        }
    }

    private async Task ChangeStatusAsync(
        WorkTask task,
        WorkTaskStatus status,
        string changedByUserId,
        string note)
    {
        if (task.Status == status)
        {
            return;
        }

        var previousStatus = task.Status;
        var now = DateTime.UtcNow;
        task.Status = status;
        task.CompletedAt = status == WorkTaskStatus.Done ? now : null;
        task.UpdatedAt = now;
        context.TaskStatusHistories.Add(new TaskStatusHistory
        {
            CompanyId = task.CompanyId,
            WorkTaskId = task.Id,
            ChangedByUserId = changedByUserId,
            FromStatus = previousStatus,
            ToStatus = status,
            Note = note,
            ChangedAt = now
        });
        await context.SaveChangesAsync();
    }

    private async Task PopulateOptionsAsync(TaskFormViewModel model, int companyId)
    {
        model.Departments = await BuildDepartmentOptionsAsync(companyId, model.DepartmentId);
        model.Users = await BuildUserOptionsAsync(companyId, model.AssignedUserId);
        model.Priorities = BuildEnumOptions<WorkTaskPriority>(model.Priority);
        model.Statuses = BuildEnumOptions<WorkTaskStatus>(model.Status);
        model.AssignmentTargetTypes = BuildEnumOptions<AssignmentTargetType>(model.AssignmentTargetType);
    }

    private async Task<List<SelectListItem>> BuildDepartmentOptionsAsync(int companyId, int? selectedId)
    {
        var departments = await context.Departments.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.IsActive)
            .OrderBy(item => item.Name)
            .ToListAsync();

        return departments.Select(item => new SelectListItem
        {
            Value = item.Id.ToString(),
            Text = item.Name,
            Selected = item.Id == selectedId
        }).ToList();
    }

    private async Task<List<SelectListItem>> BuildUserOptionsAsync(int companyId, string? selectedId)
    {
        var assignableUserIds = GetAssignableUserIds(companyId);
        var users = await context.Users.AsNoTracking()
            .Where(item => assignableUserIds.Contains(item.Id) && item.IsActive)
            .OrderBy(item => item.FullName)
            .ToListAsync();

        return users.Select(item => new SelectListItem
        {
            Value = item.Id,
            Text = $"{item.FullName} ({item.Email})",
            Selected = item.Id == selectedId
        }).ToList();
    }

    private IQueryable<string> GetAssignableUserIds(int companyId) =>
        context.UserRoles
            .Where(userRole =>
                context.Users.Any(user =>
                    user.Id == userRole.UserId
                    && user.CompanyId == companyId)
                && context.Roles.Any(role =>
                    role.Id == userRole.RoleId
                    && role.Name != null
                    && AssignableRoles.Contains(role.Name))
                && !context.UserRoles.Any(protectedUserRole =>
                    protectedUserRole.UserId == userRole.UserId
                    && context.Roles.Any(role =>
                        role.Id == protectedUserRole.RoleId
                        && (role.Name == AppRoles.SystemAdmin
                            || role.Name == AppRoles.CompanyAdmin))))
            .Select(userRole => userRole.UserId)
            .Distinct();

    private async Task<bool> IsAssignableUserAsync(string userId, int companyId) =>
        await context.Users.AsNoTracking()
            .AnyAsync(item =>
                item.Id == userId
                && item.CompanyId == companyId
                && item.IsActive
                && GetAssignableUserIds(companyId).Contains(item.Id));

    private async Task<Dictionary<int, TaskAssignment>> GetLatestAssignmentsAsync(
        int companyId,
        IEnumerable<int> taskIds)
    {
        var ids = taskIds.ToList();
        var assignments = await context.TaskAssignments.AsNoTracking()
            .Where(item => item.CompanyId == companyId && ids.Contains(item.WorkTaskId))
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync();

        return assignments
            .GroupBy(item => item.WorkTaskId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private async Task<TaskAssignment?> GetLatestAssignmentAsync(int taskId, int companyId) =>
        await context.TaskAssignments.AsNoTracking()
            .Where(item => item.WorkTaskId == taskId && item.CompanyId == companyId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync();

    private async Task<Dictionary<string, string>> GetUserNamesAsync(
        int companyId,
        IEnumerable<string> userIds)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        return await context.Users.AsNoTracking()
            .Where(item => item.CompanyId == companyId && ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.FullName);
    }

    private async Task<Dictionary<int, string>> GetDepartmentNamesAsync(int companyId) =>
        await context.Departments.AsNoTracking()
            .Where(item => item.CompanyId == companyId)
            .ToDictionaryAsync(item => item.Id, item => item.Name);

    private async Task<ApplicationUser?> GetCurrentUserAsync() => await userManager.GetUserAsync(User);

    private static TaskAssignment BuildAssignment(
        int taskId,
        int companyId,
        string assignedByUserId,
        TaskFormViewModel model,
        DateTime createdAt)
    {
        var assignment = new TaskAssignment
        {
            WorkTaskId = taskId,
            CompanyId = companyId,
            CreatedAt = createdAt
        };
        ApplyAssignment(assignment, assignedByUserId, model);
        return assignment;
    }

    private static void ApplyAssignment(
        TaskAssignment assignment,
        string assignedByUserId,
        TaskFormViewModel model)
    {
        assignment.TargetType = model.AssignmentTargetType;
        assignment.AssignedToUserId = model.AssignmentTargetType == AssignmentTargetType.User
            ? model.AssignedUserId
            : null;
        assignment.AssignedToDepartmentId = model.AssignmentTargetType == AssignmentTargetType.Department
            ? model.DepartmentId
            : null;
        assignment.AssignedByUserId = assignedByUserId;
        assignment.DueDate = model.DueDate;
    }

    private static List<SelectListItem> BuildEnumOptions<TEnum>(TEnum selected)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(value => new SelectListItem
        {
            Value = Convert.ToInt32(value).ToString(),
            Text = GetEnumLabel(value),
            Selected = EqualityComparer<TEnum>.Default.Equals(value, selected)
        }).ToList();

    private static string GetEnumLabel<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        value.ToString() switch
        {
            nameof(AssignmentTargetType.User) => "Giao cho nhân viên",
            nameof(AssignmentTargetType.Department) => "Giao cho phòng ban",
            nameof(WorkTaskPriority.Low) => "Thấp",
            nameof(WorkTaskPriority.Medium) => "Trung bình",
            nameof(WorkTaskPriority.High) => "Cao",
            nameof(WorkTaskPriority.Urgent) => "Khẩn cấp",
            nameof(WorkTaskStatus.New) => "Mới",
            nameof(WorkTaskStatus.Assigned) => "Đã phân công",
            nameof(WorkTaskStatus.InProgress) => "Đang thực hiện",
            nameof(WorkTaskStatus.Review) => "Chờ duyệt",
            nameof(WorkTaskStatus.Done) => "Hoàn thành",
            nameof(WorkTaskStatus.Cancelled) => "Đã hủy",
            nameof(WorkTaskStatus.Overdue) => "Quá hạn",
            _ => value.ToString()
        };

    private static void NormalizeForm(TaskFormViewModel model)
    {
        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
        model.AssignedUserId = string.IsNullOrWhiteSpace(model.AssignedUserId)
            ? null
            : model.AssignedUserId.Trim();
        model.DueDate = model.DueDate?.Date;

        if (model.AssignmentTargetType == AssignmentTargetType.User)
        {
            model.DepartmentId = null;
        }
        else
        {
            model.AssignedUserId = null;
        }
    }
}
