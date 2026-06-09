using EnterpriseTask.Admin.ViewModels.CompanyProfile;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Admin.Controllers;

[Authorize(Roles = AppRoles.CompanyAdmin)]
[Route("company/profile")]
public class CompanyProfileController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser?.CompanyId is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var companyId = currentUser.CompanyId.Value;
        var company = await context.Companies.AsNoTracking()
            .Where(item => item.Id == companyId)
            .Select(item => new CompanyProfileViewModel
            {
                Name = item.Name,
                Email = item.Email,
                Phone = item.Phone,
                TaxCode = item.TaxCode,
                Address = item.Address,
                Industry = item.Industry,
                Status = item.Status,
                CreatedAt = item.CreatedAt,
                DepartmentCount = item.Departments.Count,
                EmployeeCount = item.EmployeeProfiles.Count,
                TaskCount = item.WorkTasks.Count
            })
            .FirstOrDefaultAsync();

        return company is null
            ? RedirectToAction("AccessDenied", "Account")
            : View(company);
    }
}
