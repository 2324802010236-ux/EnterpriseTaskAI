using EnterpriseTask.Admin.ViewModels.Companies;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Admin.Controllers;

[Authorize(Roles = AppRoles.SystemAdmin)]
[Route("owner/companies")]
public class CompaniesController(
    AppDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, CompanyStatus? status)
    {
        var query = context.Companies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(company =>
                company.Name.Contains(keyword) || company.Email.Contains(keyword));
        }

        if (status.HasValue)
        {
            query = query.Where(company => company.Status == status.Value);
        }

        var companies = await query
            .OrderByDescending(company => company.CreatedAt)
            .Select(company => new CompanyListItemViewModel
            {
                Id = company.Id,
                Name = company.Name,
                Email = company.Email,
                Phone = company.Phone,
                Industry = company.Industry,
                Status = company.Status.ToString(),
                DepartmentCount = company.Departments.Count,
                UserCount = context.Users.Count(user => user.CompanyId == company.Id),
                CreatedAt = company.CreatedAt
            })
            .ToListAsync();

        ViewBag.Search = search?.Trim();
        ViewBag.Status = status;

        return View(companies);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var company = await BuildDetailsAsync(id);
        return company is null ? NotFound() : View(company);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new CompanyFormViewModel { Status = CompanyStatus.Active });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompanyFormViewModel model)
    {
        model.Status = CompanyStatus.Active;
        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateUniqueEmailAsync(model.Email, null);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var company = new Company
        {
            Name = model.Name,
            TaxCode = model.TaxCode,
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address,
            Industry = model.Industry,
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã tạo công ty mới thành công.";
        return RedirectToAction(nameof(Details), new { id = company.Id });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var company = await context.Companies.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (company is null)
        {
            return NotFound();
        }

        return View(new CompanyFormViewModel
        {
            Id = company.Id,
            Name = company.Name,
            TaxCode = company.TaxCode,
            Email = company.Email,
            Phone = company.Phone,
            Address = company.Address,
            Industry = company.Industry,
            Status = company.Status
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CompanyFormViewModel model)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        NormalizeForm(model);
        ModelState.Clear();
        TryValidateModel(model);
        await ValidateUniqueEmailAsync(model.Email, id);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var company = await context.Companies.FirstOrDefaultAsync(item => item.Id == id);
        if (company is null)
        {
            return NotFound();
        }

        company.Name = model.Name;
        company.TaxCode = model.TaxCode;
        company.Email = model.Email;
        company.Phone = model.Phone;
        company.Address = model.Address;
        company.Industry = model.Industry;
        company.Status = model.Status;
        company.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật thông tin công ty.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("toggle-status/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var company = await context.Companies.FirstOrDefaultAsync(item => item.Id == id);
        if (company is null)
        {
            return NotFound();
        }

        if (company.Status == CompanyStatus.Active)
        {
            company.Status = CompanyStatus.Suspended;
        }
        else if (company.Status == CompanyStatus.Suspended)
        {
            company.Status = CompanyStatus.Active;
        }
        else
        {
            TempData["ErrorMessage"] = "Trạng thái hiện tại không thể khóa hoặc mở trực tiếp.";
            return RedirectToAction(nameof(Details), new { id });
        }

        company.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] = company.Status == CompanyStatus.Active
            ? "Đã mở hoạt động công ty."
            : "Đã tạm khóa công ty.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<CompanyDetailsViewModel?> BuildDetailsAsync(int companyId)
    {
        return await context.Companies.AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => new CompanyDetailsViewModel
            {
                Id = company.Id,
                Name = company.Name,
                TaxCode = company.TaxCode,
                Email = company.Email,
                Phone = company.Phone,
                Address = company.Address,
                Industry = company.Industry,
                Status = company.Status,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt,
                DepartmentCount = company.Departments.Count,
                UserCount = context.Users.Count(user => user.CompanyId == company.Id),
                TaskCount = company.WorkTasks.Count
            })
            .FirstOrDefaultAsync();
    }

    private async Task ValidateUniqueEmailAsync(string email, int? excludedCompanyId)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var exists = await context.Companies.AsNoTracking()
            .AnyAsync(company =>
                company.Email == email
                && (!excludedCompanyId.HasValue || company.Id != excludedCompanyId.Value));

        if (exists)
        {
            ModelState.AddModelError(nameof(CompanyFormViewModel.Email), "Email công ty đã tồn tại.");
        }
    }

    private static void NormalizeForm(CompanyFormViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.TaxCode = NormalizeOptional(model.TaxCode);
        model.Phone = NormalizeOptional(model.Phone);
        model.Address = NormalizeOptional(model.Address);
        model.Industry = NormalizeOptional(model.Industry);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
