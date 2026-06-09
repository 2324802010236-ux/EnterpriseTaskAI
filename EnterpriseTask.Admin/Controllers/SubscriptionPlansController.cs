using EnterpriseTask.Admin.ViewModels.SubscriptionPlans;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Admin.Controllers;

[Authorize(Roles = AppRoles.SystemAdmin)]
[Route("owner/subscription-plans")]
public class SubscriptionPlansController(AppDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var plans = await context.SubscriptionPlans.AsNoTracking()
            .OrderByDescending(plan => plan.IsActive)
            .ThenBy(plan => plan.Price)
            .Select(plan => new SubscriptionPlanListItemViewModel
            {
                Id = plan.Id,
                Name = plan.Name,
                Code = plan.Code,
                Price = plan.Price,
                DurationDays = plan.DurationDays,
                MaxEmployees = plan.MaxEmployees,
                MaxDepartments = plan.MaxDepartments,
                EnableAI = plan.EnableAI,
                EnableRealtimeChat = plan.EnableRealtimeChat,
                IsActive = plan.IsActive
            })
            .ToListAsync();

        return View(plans);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new SubscriptionPlanFormViewModel
        {
            DurationDays = 30,
            EnableRealtimeChat = true,
            IsActive = true
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionPlanFormViewModel model)
    {
        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateUniqueCodeAsync(model.Code, null);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var plan = new SubscriptionPlan
        {
            Name = model.Name,
            Code = model.Code,
            Description = model.Description,
            Price = model.Price,
            DurationDays = model.DurationDays,
            MaxEmployees = model.MaxEmployees,
            MaxDepartments = model.MaxDepartments,
            EnableAI = model.EnableAI,
            EnableRealtimeChat = model.EnableRealtimeChat,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        context.SubscriptionPlans.Add(plan);
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã tạo gói dịch vụ mới.";
        return RedirectToAction(nameof(Details), new { id = plan.Id });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var plan = await context.SubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (plan is null)
        {
            return NotFound();
        }

        return View(new SubscriptionPlanFormViewModel
        {
            Id = plan.Id,
            Name = plan.Name,
            Code = plan.Code,
            Description = plan.Description,
            Price = plan.Price,
            DurationDays = plan.DurationDays,
            MaxEmployees = plan.MaxEmployees,
            MaxDepartments = plan.MaxDepartments,
            EnableAI = plan.EnableAI,
            EnableRealtimeChat = plan.EnableRealtimeChat,
            IsActive = plan.IsActive
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubscriptionPlanFormViewModel model)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateUniqueCodeAsync(model.Code, id);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var plan = await context.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == id);
        if (plan is null)
        {
            return NotFound();
        }

        plan.Name = model.Name;
        plan.Code = model.Code;
        plan.Description = model.Description;
        plan.Price = model.Price;
        plan.DurationDays = model.DurationDays;
        plan.MaxEmployees = model.MaxEmployees;
        plan.MaxDepartments = model.MaxDepartments;
        plan.EnableAI = model.EnableAI;
        plan.EnableRealtimeChat = model.EnableRealtimeChat;
        plan.IsActive = model.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật gói dịch vụ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var plan = await context.SubscriptionPlans.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new SubscriptionPlanDetailsViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Code = item.Code,
                Description = item.Description,
                Price = item.Price,
                DurationDays = item.DurationDays,
                MaxEmployees = item.MaxEmployees,
                MaxDepartments = item.MaxDepartments,
                EnableAI = item.EnableAI,
                EnableRealtimeChat = item.EnableRealtimeChat,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                CompanySubscriptionCount = item.CompanySubscriptions.Count
            })
            .FirstOrDefaultAsync();

        return plan is null ? NotFound() : View(plan);
    }

    [HttpPost("toggle-status/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var plan = await context.SubscriptionPlans.FirstOrDefaultAsync(item => item.Id == id);
        if (plan is null)
        {
            return NotFound();
        }

        plan.IsActive = !plan.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = plan.IsActive
            ? "Đã bật gói dịch vụ."
            : "Đã tắt gói dịch vụ.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateUniqueCodeAsync(string code, int? excludedPlanId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var exists = await context.SubscriptionPlans.AsNoTracking()
            .AnyAsync(plan =>
                plan.Code == code
                && (!excludedPlanId.HasValue || plan.Id != excludedPlanId.Value));

        if (exists)
        {
            ModelState.AddModelError(nameof(SubscriptionPlanFormViewModel.Code), "Mã gói dịch vụ đã tồn tại.");
        }
    }

    private static void NormalizeForm(SubscriptionPlanFormViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Code = model.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        model.Description = model.Description?.Trim() ?? string.Empty;
    }
}
