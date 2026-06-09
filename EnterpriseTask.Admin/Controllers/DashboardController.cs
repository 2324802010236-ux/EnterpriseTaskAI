using EnterpriseTask.Admin.ViewModels.Dashboard;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Admin.Controllers;

[Authorize(Roles = AppRoles.SystemAdmin + "," + AppRoles.CompanyAdmin)]
public class DashboardController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null || !currentUser.IsActive)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (await userManager.IsInRoleAsync(currentUser, AppRoles.SystemAdmin))
        {
            return View(await BuildSystemAdminDashboardAsync());
        }

        if (!currentUser.CompanyId.HasValue)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(await BuildCompanyAdminDashboardAsync(currentUser.CompanyId.Value));
    }

    private async Task<AdminDashboardViewModel> BuildSystemAdminDashboardAsync()
    {
        var totalCompanies = await context.Companies.AsNoTracking().CountAsync();
        var activeCompanies = await context.Companies.AsNoTracking()
            .CountAsync(company => company.Status == CompanyStatus.Active);
        var totalUsers = await userManager.Users.AsNoTracking().CountAsync();
        var totalTasks = await context.WorkTasks.AsNoTracking().CountAsync();

        var recentCompanies = await context.Companies.AsNoTracking()
            .OrderByDescending(company => company.CreatedAt)
            .Take(5)
            .Select(company => new RecentCompanyViewModel
            {
                Id = company.Id,
                Name = company.Name,
                Email = company.Email,
                Status = company.Status == CompanyStatus.Active
                    ? "Đang hoạt động"
                    : company.Status == CompanyStatus.Suspended
                        ? "Tạm ngưng"
                        : "Đã xóa",
                CreatedAt = company.CreatedAt
            })
            .ToListAsync();

        var recentUsers = await BuildRecentUsersAsync(userManager.Users.AsNoTracking(), includeCompany: true);

        return new AdminDashboardViewModel
        {
            DashboardTitle = "Tổng quan hệ thống",
            DashboardSubtitle = "Theo dõi nhanh tình hình vận hành trên toàn bộ nền tảng.",
            StatCards =
            [
                new() { Title = "Tổng công ty", Value = totalCompanies, Icon = "building", Description = "Tất cả doanh nghiệp trên hệ thống" },
                new() { Title = "Công ty hoạt động", Value = activeCompanies, Icon = "activity", Description = "Doanh nghiệp đang sử dụng nền tảng" },
                new() { Title = "Tổng người dùng", Value = totalUsers, Icon = "users", Description = "Tài khoản đã được khởi tạo" },
                new() { Title = "Tổng công việc", Value = totalTasks, Icon = "tasks", Description = "Công việc trên toàn hệ thống" }
            ],
            RecentCompanies = recentCompanies,
            RecentUsers = recentUsers
        };
    }

    private async Task<AdminDashboardViewModel> BuildCompanyAdminDashboardAsync(int companyId)
    {
        var companyName = await context.Companies.AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.Name)
            .FirstOrDefaultAsync();

        if (companyName is null)
        {
            return new AdminDashboardViewModel
            {
                DashboardTitle = "Tổng quan công ty",
                DashboardSubtitle = "Không tìm thấy thông tin công ty được liên kết với tài khoản."
            };
        }

        var companyUsers = userManager.Users.AsNoTracking()
            .Where(user => user.CompanyId == companyId);
        var companyEmployees = context.EmployeeProfiles.AsNoTracking()
            .Where(employee => employee.CompanyId == companyId);
        var companyDepartments = context.Departments.AsNoTracking()
            .Where(department => department.CompanyId == companyId);
        var companyTasks = context.WorkTasks.AsNoTracking()
            .Where(task => task.CompanyId == companyId);

        var totalEmployees = await companyEmployees.CountAsync();
        var totalDepartments = await companyDepartments.CountAsync();
        var totalTasks = await companyTasks.CountAsync();
        var incompleteTasks = await companyTasks.CountAsync(task =>
            task.Status != WorkTaskStatus.Done && task.Status != WorkTaskStatus.Cancelled);
        var overdueTasks = await companyTasks.CountAsync(task =>
            task.Status != WorkTaskStatus.Done
            && task.Status != WorkTaskStatus.Cancelled
            && (task.Status == WorkTaskStatus.Overdue
                || (task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow)));

        var recentUsers = await BuildRecentUsersAsync(companyUsers, includeCompany: false);

        return new AdminDashboardViewModel
        {
            DashboardTitle = $"Tổng quan {companyName}",
            DashboardSubtitle = "Theo dõi nhân sự, phòng ban và tiến độ công việc trong công ty.",
            StatCards =
            [
                new() { Title = "Tổng nhân viên", Value = totalEmployees, Icon = "users", Description = "Hồ sơ nhân viên thuộc công ty" },
                new() { Title = "Tổng phòng ban", Value = totalDepartments, Icon = "departments", Description = "Đơn vị trong cơ cấu tổ chức" },
                new() { Title = "Tổng công việc", Value = totalTasks, Icon = "tasks", Description = "Công việc thuộc công ty" },
                new() { Title = "Task chưa hoàn thành", Value = incompleteTasks, Icon = "clock", Description = $"{overdueTasks} task đã quá hạn" }
            ],
            RecentUsers = recentUsers
        };
    }

    private async Task<List<RecentUserViewModel>> BuildRecentUsersAsync(
        IQueryable<ApplicationUser> usersQuery,
        bool includeCompany)
    {
        var recentUsers = await usersQuery
            .OrderByDescending(user => user.CreatedAt)
            .Take(5)
            .ToListAsync();

        var companyNames = new Dictionary<int, string>();
        if (includeCompany)
        {
            var companyIds = recentUsers
                .Where(user => user.CompanyId.HasValue)
                .Select(user => user.CompanyId!.Value)
                .Distinct()
                .ToList();

            companyNames = await context.Companies.AsNoTracking()
                .Where(company => companyIds.Contains(company.Id))
                .ToDictionaryAsync(company => company.Id, company => company.Name);
        }

        var result = new List<RecentUserViewModel>();
        foreach (var user in recentUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new RecentUserViewModel
            {
                Id = user.Id,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Chưa cập nhật" : user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Chưa phân quyền",
                CompanyName = user.CompanyId.HasValue
                    && companyNames.TryGetValue(user.CompanyId.Value, out var name)
                        ? name
                        : null,
                CreatedAt = user.CreatedAt
            });
        }

        return result;
    }
}
