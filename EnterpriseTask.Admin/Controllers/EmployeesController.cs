using System.Security.Cryptography;
using EnterpriseTask.Admin.Services;
using EnterpriseTask.Admin.ViewModels.Employees;
using EnterpriseTask.Application.Interfaces;
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
[Route("company/employees")]
public class EmployeesController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    ILogger<EmployeesController> logger) : Controller
{
    private static readonly string[] ManagedRoles =
    [
        AppRoles.Director,
        AppRoles.DepartmentManager,
        AppRoles.Employee
    ];

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int? departmentId, bool? isActive)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var managedUserIds = GetManagedUserIds(companyId.Value);
        var query = context.Users.AsNoTracking()
            .Where(user => managedUserIds.Contains(user.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(user =>
                user.FullName.Contains(keyword)
                || (user.Email != null && user.Email.Contains(keyword))
                || (user.PhoneNumber != null && user.PhoneNumber.Contains(keyword)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(user => user.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync();
        var rolesByUser = await GetManagedRolesByUserAsync(users.Select(user => user.Id));
        var departmentNames = await GetDepartmentNamesAsync(companyId.Value);

        var model = users.Select(user => new EmployeeListItemViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            DepartmentName = user.DepartmentId.HasValue
                && departmentNames.TryGetValue(user.DepartmentId.Value, out var departmentName)
                    ? departmentName
                    : null,
            Role = rolesByUser.GetValueOrDefault(user.Id, AppRoles.Employee),
            Position = user.Position,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }).ToList();

        ViewBag.Search = search?.Trim();
        ViewBag.DepartmentId = departmentId;
        ViewBag.IsActive = isActive;
        ViewBag.Departments = await BuildDepartmentOptionsAsync(companyId.Value, departmentId);

        return View(model);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = new EmployeeFormViewModel { IsActive = true, Role = AppRoles.Employee };
        await PopulateOptionsAsync(model, companyId.Value);
        await PopulateEmployeeLimitAsync(companyId.Value);
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateFormAsync(model, companyId.Value, null);

        var limit = await GetEmployeeLimitAsync(companyId.Value);
        ViewBag.EmployeeCount = limit.CurrentCount;
        ViewBag.MaxEmployees = limit.MaxEmployees;
        if (!limit.MaxEmployees.HasValue || limit.CurrentCount >= limit.MaxEmployees.Value)
        {
            var message = limit.MaxEmployees.HasValue
                ? $"Gói hiện tại chỉ cho phép tối đa {limit.MaxEmployees.Value} nhân viên."
                : "Không tìm thấy giới hạn nhân viên của gói đang hoạt động.";
            ModelState.AddModelError(string.Empty, message);
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, companyId.Value);
            return View(model);
        }

        var temporaryPassword = GenerateTemporaryPassword();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            CompanyId = companyId.Value,
            DepartmentId = model.DepartmentId,
            Position = model.Position,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createUserResult = await userManager.CreateAsync(user, temporaryPassword);
        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, FormatIdentityErrors(createUserResult));
            await PopulateOptionsAsync(model, companyId.Value);
            return View(model);
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, model.Role);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, FormatIdentityErrors(addRoleResult));
            await PopulateOptionsAsync(model, companyId.Value);
            return View(model);
        }

        context.EmployeeProfiles.Add(new EmployeeProfile
        {
            CompanyId = companyId.Value,
            UserId = user.Id,
            DepartmentId = model.DepartmentId,
            EmployeeCode = GenerateEmployeeCode(companyId.Value),
            FullName = model.FullName,
            Phone = model.PhoneNumber,
            Position = model.Position,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        var companyName = await context.Companies.AsNoTracking()
            .Where(company => company.Id == companyId.Value)
            .Select(company => company.Name)
            .FirstAsync();
        var emailSent = await TrySendEmailAsync(
            model.Email,
            "Tài khoản WorkFlow AI của bạn đã được tạo",
            EmployeeEmailTemplates.BuildEmployeeAccountEmail(
                model.FullName,
                companyName,
                model.Email,
                temporaryPassword));

        // Demo only. In production, use set-password link instead of temporary password.
        TempData["SuccessMessage"] = "Đã tạo tài khoản nhân viên.";
        if (!emailSent)
        {
            TempData["WarningMessage"] =
                "Tài khoản đã được tạo nhưng gửi email thất bại. Vui lòng kiểm tra cấu hình SMTP.";
        }

        TempData["TemporaryPassword"] = temporaryPassword;
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var user = await FindManagedUserAsync(id, companyId.Value);
        if (user is null)
        {
            return NotFound();
        }

        var model = new EmployeeFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            Role = await GetManagedRoleAsync(user),
            Position = user.Position,
            IsActive = user.IsActive
        };
        await PopulateOptionsAsync(model, companyId.Value);
        return View(model);
    }

    [HttpPost("edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EmployeeFormViewModel model)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var user = await FindManagedUserAsync(id, companyId.Value);
        if (user is null)
        {
            return NotFound();
        }

        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateFormAsync(model, companyId.Value, id);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, companyId.Value);
            return View(model);
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        var setEmailResult = await userManager.SetEmailAsync(user, model.Email);
        var setUserNameResult = await userManager.SetUserNameAsync(user, model.Email);
        if (!setEmailResult.Succeeded || !setUserNameResult.Succeeded)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                FormatIdentityErrors(!setEmailResult.Succeeded ? setEmailResult : setUserNameResult));
            await PopulateOptionsAsync(model, companyId.Value);
            return View(model);
        }

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.DepartmentId = model.DepartmentId;
        user.Position = model.Position;
        user.IsActive = model.IsActive;
        var updateUserResult = await userManager.UpdateAsync(user);
        if (!updateUserResult.Succeeded)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, FormatIdentityErrors(updateUserResult));
            await PopulateOptionsAsync(model, companyId.Value);
            return View(model);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var currentManagedRoles = currentRoles.Where(IsManagedRole).ToList();
        if (currentManagedRoles.Count > 0)
        {
            var removeRolesResult = await userManager.RemoveFromRolesAsync(user, currentManagedRoles);
            if (!removeRolesResult.Succeeded)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, FormatIdentityErrors(removeRolesResult));
                await PopulateOptionsAsync(model, companyId.Value);
                return View(model);
            }
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, model.Role);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, FormatIdentityErrors(addRoleResult));
            await PopulateOptionsAsync(model, companyId.Value);
            return View(model);
        }

        await SyncEmployeeProfileAsync(user, companyId.Value);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "Đã cập nhật thông tin nhân viên.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var user = await FindManagedUserAsync(id, companyId.Value);
        if (user is null)
        {
            return NotFound();
        }

        var departmentName = user.DepartmentId.HasValue
            ? await context.Departments.AsNoTracking()
                .Where(department =>
                    department.Id == user.DepartmentId.Value
                    && department.CompanyId == companyId.Value)
                .Select(department => department.Name)
                .FirstOrDefaultAsync()
            : null;

        return View(new EmployeeDetailsViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            DepartmentName = departmentName,
            Role = await GetManagedRoleAsync(user),
            Position = user.Position,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPost("toggle-status/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (currentUser.Id == id)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của mình.";
            return RedirectToAction(nameof(Index));
        }

        var user = await FindManagedUserAsync(id, currentUser.CompanyId.Value);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = FormatIdentityErrors(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        var profile = await context.EmployeeProfiles
            .FirstOrDefaultAsync(item =>
                item.CompanyId == currentUser.CompanyId.Value
                && item.UserId == user.Id);
        if (profile is not null)
        {
            profile.IsActive = user.IsActive;
            profile.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = user.IsActive
            ? "Đã kích hoạt tài khoản nhân viên."
            : "Đã khóa tài khoản nhân viên.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("reset-password/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var user = await FindManagedUserAsync(id, companyId.Value);
        if (user is null)
        {
            return NotFound();
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = FormatIdentityErrors(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        var email = user.Email ?? user.UserName ?? string.Empty;
        var emailSent = await TrySendEmailAsync(
            email,
            "Mật khẩu WorkFlow AI của bạn đã được đặt lại",
            EmployeeEmailTemplates.BuildPasswordResetEmail(
                user.FullName,
                email,
                temporaryPassword));

        // Demo only. In production, use set-password link instead of temporary password.
        TempData["SuccessMessage"] = "Đã đặt lại mật khẩu nhân viên.";
        if (!emailSent)
        {
            TempData["WarningMessage"] =
                "Mật khẩu đã được đặt lại nhưng gửi email thất bại. Vui lòng kiểm tra cấu hình SMTP.";
        }

        TempData["TemporaryPassword"] = temporaryPassword;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> TrySendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            await emailSender.SendEmailAsync(to, subject, htmlBody);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Employee operation succeeded, but email delivery to {Recipient} failed.",
                to);
            return false;
        }
    }

    private IQueryable<string> GetManagedUserIds(int companyId) =>
        context.UserRoles
            .Where(userRole =>
                context.Roles.Any(role =>
                    role.Id == userRole.RoleId
                    && role.Name != null
                    && ManagedRoles.Contains(role.Name))
                && context.Users.Any(user =>
                    user.Id == userRole.UserId
                    && user.CompanyId == companyId)
                && !context.UserRoles.Any(protectedUserRole =>
                    protectedUserRole.UserId == userRole.UserId
                    && context.Roles.Any(role =>
                        role.Id == protectedUserRole.RoleId
                        && (role.Name == AppRoles.SystemAdmin
                            || role.Name == AppRoles.CompanyAdmin))))
            .Select(userRole => userRole.UserId);

    private async Task<ApplicationUser?> FindManagedUserAsync(string id, int companyId)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return roles.Any(IsManagedRole)
            && !roles.Contains(AppRoles.SystemAdmin)
            && !roles.Contains(AppRoles.CompanyAdmin)
            ? user
            : null;
    }

    private async Task ValidateFormAsync(EmployeeFormViewModel model, int companyId, string? excludedUserId)
    {
        if (!IsManagedRole(model.Role))
        {
            ModelState.AddModelError(nameof(EmployeeFormViewModel.Role), "Vai trò nhân viên không hợp lệ.");
        }

        if ((model.Role == AppRoles.DepartmentManager || model.Role == AppRoles.Employee)
            && !model.DepartmentId.HasValue)
        {
            ModelState.AddModelError(
                nameof(EmployeeFormViewModel.DepartmentId),
                "Vai trò này cần được gán vào một phòng ban.");
        }

        if (model.DepartmentId.HasValue)
        {
            var departmentExists = await context.Departments.AsNoTracking()
                .AnyAsync(department =>
                    department.Id == model.DepartmentId.Value
                    && department.CompanyId == companyId);
            if (!departmentExists)
            {
                ModelState.AddModelError(
                    nameof(EmployeeFormViewModel.DepartmentId),
                    "Phòng ban không thuộc công ty hiện tại.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var normalizedEmail = userManager.NormalizeEmail(model.Email);
            var emailExists = await context.Users.AsNoTracking()
                .AnyAsync(user =>
                    user.NormalizedEmail == normalizedEmail
                    && (excludedUserId == null || user.Id != excludedUserId));
            if (emailExists)
            {
                ModelState.AddModelError(nameof(EmployeeFormViewModel.Email), "Email đã được sử dụng.");
            }
        }
    }

    private async Task PopulateOptionsAsync(EmployeeFormViewModel model, int companyId)
    {
        model.Departments = await BuildDepartmentOptionsAsync(companyId, model.DepartmentId);
        model.Roles = ManagedRoles.Select(role => new SelectListItem
        {
            Value = role,
            Text = GetRoleLabel(role),
            Selected = role == model.Role
        }).ToList();
    }

    private async Task<List<SelectListItem>> BuildDepartmentOptionsAsync(int companyId, int? selectedId)
    {
        var departments = await context.Departments.AsNoTracking()
            .Where(department => department.CompanyId == companyId)
            .OrderBy(department => department.Name)
            .ToListAsync();

        return departments.Select(department => new SelectListItem
        {
            Value = department.Id.ToString(),
            Text = department.Name,
            Selected = department.Id == selectedId
        }).ToList();
    }

    private async Task PopulateEmployeeLimitAsync(int companyId)
    {
        var limit = await GetEmployeeLimitAsync(companyId);
        ViewBag.EmployeeCount = limit.CurrentCount;
        ViewBag.MaxEmployees = limit.MaxEmployees;
    }

    private async Task<(int CurrentCount, int? MaxEmployees)> GetEmployeeLimitAsync(int companyId)
    {
        var companyAdminRoleId = await context.Roles.AsNoTracking()
            .Where(role => role.Name == AppRoles.CompanyAdmin)
            .Select(role => role.Id)
            .FirstOrDefaultAsync();
        var currentCount = await context.Users.AsNoTracking()
            .CountAsync(user =>
                user.CompanyId == companyId
                && user.IsActive
                && !context.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id
                    && userRole.RoleId == companyAdminRoleId));
        var maxEmployees = await context.CompanySubscriptions.AsNoTracking()
            .Where(subscription =>
                subscription.CompanyId == companyId
                && subscription.Status == SubscriptionStatus.Active
                && subscription.EndDate >= DateTime.UtcNow.Date)
            .OrderByDescending(subscription => subscription.StartDate)
            .ThenByDescending(subscription => subscription.CreatedAt)
            .Select(subscription => (int?)subscription.MaxEmployees)
            .FirstOrDefaultAsync();

        return (currentCount, maxEmployees);
    }

    private async Task<Dictionary<int, string>> GetDepartmentNamesAsync(int companyId) =>
        await context.Departments.AsNoTracking()
            .Where(department => department.CompanyId == companyId)
            .ToDictionaryAsync(department => department.Id, department => department.Name);

    private async Task<Dictionary<string, string>> GetManagedRolesByUserAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        return await (
            from userRole in context.UserRoles.AsNoTracking()
            join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
                  && role.Name != null
                  && ManagedRoles.Contains(role.Name)
            select new { userRole.UserId, Role = role.Name! })
            .ToDictionaryAsync(item => item.UserId, item => item.Role);
    }

    private async Task<string> GetManagedRoleAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return roles.FirstOrDefault(IsManagedRole) ?? AppRoles.Employee;
    }

    private async Task SyncEmployeeProfileAsync(ApplicationUser user, int companyId)
    {
        var profile = await context.EmployeeProfiles
            .FirstOrDefaultAsync(item => item.CompanyId == companyId && item.UserId == user.Id);
        if (profile is null)
        {
            profile = new EmployeeProfile
            {
                CompanyId = companyId,
                UserId = user.Id,
                EmployeeCode = GenerateEmployeeCode(companyId),
                CreatedAt = DateTime.UtcNow
            };
            context.EmployeeProfiles.Add(profile);
        }

        profile.DepartmentId = user.DepartmentId;
        profile.FullName = user.FullName;
        profile.Phone = user.PhoneNumber;
        profile.Position = user.Position;
        profile.IsActive = user.IsActive;
        profile.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<int?> GetCurrentCompanyIdAsync()
    {
        var user = await userManager.GetUserAsync(User);
        return user?.CompanyId;
    }

    private static bool IsManagedRole(string role) => ManagedRoles.Contains(role);

    private static string GetRoleLabel(string role) =>
        role switch
        {
            AppRoles.Director => "Giám đốc",
            AppRoles.DepartmentManager => "Trưởng phòng",
            _ => "Nhân viên"
        };

    private static string GenerateTemporaryPassword() =>
        $"User@{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";

    private static string GenerateEmployeeCode(int companyId) =>
        $"EMP-{companyId:D4}-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";

    private static string FormatIdentityErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));

    private static void NormalizeForm(EmployeeFormViewModel model)
    {
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.PhoneNumber = NormalizeOptional(model.PhoneNumber);
        model.Position = NormalizeOptional(model.Position);
        model.Role = model.Role?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
