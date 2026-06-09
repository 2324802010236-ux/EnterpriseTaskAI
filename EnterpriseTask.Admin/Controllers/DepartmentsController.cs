using EnterpriseTask.Admin.ViewModels.Departments;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Admin.Controllers;

[Authorize(Roles = AppRoles.CompanyAdmin)]
[Route("company/departments")]
public class DepartmentsController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var query = context.Departments.AsNoTracking()
            .Where(department => department.CompanyId == companyId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(department => department.Name.Contains(keyword));
        }

        var departments = await query
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentListItemViewModel
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                IsActive = department.IsActive,
                EmployeeCount = department.EmployeeProfiles.Count,
                CreatedAt = department.CreatedAt
            })
            .ToListAsync();

        ViewBag.Search = search?.Trim();
        return View(departments);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var limitResult = await GetDepartmentLimitAsync(companyId.Value);
        ViewBag.DepartmentCount = limitResult.CurrentCount;
        ViewBag.MaxDepartments = limitResult.MaxDepartments;

        return View(new DepartmentFormViewModel { IsActive = true });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateUniqueNameAsync(companyId.Value, model.Name, null);

        var limitResult = await GetDepartmentLimitAsync(companyId.Value);
        ViewBag.DepartmentCount = limitResult.CurrentCount;
        ViewBag.MaxDepartments = limitResult.MaxDepartments;
        if (!limitResult.MaxDepartments.HasValue
            || limitResult.CurrentCount >= limitResult.MaxDepartments.Value)
        {
            var message = limitResult.MaxDepartments.HasValue
                ? $"Gói hiện tại chỉ cho phép tối đa {limitResult.MaxDepartments.Value} phòng ban."
                : "Không tìm thấy giới hạn phòng ban của gói đang hoạt động.";
            ModelState.AddModelError(string.Empty, message);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var department = new Department
        {
            CompanyId = companyId.Value,
            Name = model.Name,
            Description = model.Description,
            FunctionDescription = model.FunctionDescription,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã tạo phòng ban mới.";
        return RedirectToAction(nameof(Details), new { id = department.Id });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var department = await context.Departments.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId.Value);
        if (department is null)
        {
            return NotFound();
        }

        return View(new DepartmentFormViewModel
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            FunctionDescription = department.FunctionDescription,
            IsActive = department.IsActive
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentFormViewModel model)
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

        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateUniqueNameAsync(companyId.Value, model.Name, id);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var department = await context.Departments
            .FirstOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId.Value);
        if (department is null)
        {
            return NotFound();
        }

        department.Name = model.Name;
        department.Description = model.Description;
        department.FunctionDescription = model.FunctionDescription;
        department.IsActive = model.IsActive;
        department.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật phòng ban.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var department = await context.Departments.AsNoTracking()
            .Where(item => item.Id == id && item.CompanyId == companyId.Value)
            .Select(item => new DepartmentDetailsViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                FunctionDescription = item.FunctionDescription,
                IsActive = item.IsActive,
                EmployeeCount = item.EmployeeProfiles.Count,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return department is null ? NotFound() : View(department);
    }

    [HttpPost("toggle-status/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var companyId = await GetCurrentCompanyIdAsync();
        if (!companyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var department = await context.Departments
            .FirstOrDefaultAsync(item => item.Id == id && item.CompanyId == companyId.Value);
        if (department is null)
        {
            return NotFound();
        }

        department.IsActive = !department.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = department.IsActive
            ? "Đã kích hoạt phòng ban."
            : "Đã tạm ngưng phòng ban.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<int?> GetCurrentCompanyIdAsync()
    {
        var user = await userManager.GetUserAsync(User);
        return user?.CompanyId;
    }

    private async Task<(int CurrentCount, int? MaxDepartments)> GetDepartmentLimitAsync(int companyId)
    {
        var currentCount = await context.Departments.AsNoTracking()
            .CountAsync(department => department.CompanyId == companyId);
        var maxDepartments = await context.CompanySubscriptions.AsNoTracking()
            .Where(subscription =>
                subscription.CompanyId == companyId
                && subscription.Status == SubscriptionStatus.Active
                && subscription.EndDate >= DateTime.UtcNow.Date)
            .OrderByDescending(subscription => subscription.StartDate)
            .ThenByDescending(subscription => subscription.CreatedAt)
            .Select(subscription => (int?)subscription.MaxDepartments)
            .FirstOrDefaultAsync();

        return (currentCount, maxDepartments);
    }

    private async Task ValidateUniqueNameAsync(int companyId, string name, int? excludedDepartmentId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var exists = await context.Departments.AsNoTracking()
            .AnyAsync(department =>
                department.CompanyId == companyId
                && department.Name == name
                && (!excludedDepartmentId.HasValue || department.Id != excludedDepartmentId.Value));

        if (exists)
        {
            ModelState.AddModelError(nameof(DepartmentFormViewModel.Name), "Tên phòng ban đã tồn tại trong công ty.");
        }
    }

    private static void NormalizeForm(DepartmentFormViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Description = NormalizeOptional(model.Description);
        model.FunctionDescription = NormalizeOptional(model.FunctionDescription);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
