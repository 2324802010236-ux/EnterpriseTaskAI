using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseTask.Infrastructure.Data;

public static class DbSeeder
{
    private const string SystemAdminEmail = "sysadmin@enterprisetask.local";
    private const string SystemAdminPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<AppDbContext>();

        var roles = new[]
        {
            AppRoles.SystemAdmin,
            AppRoles.CompanyAdmin,
            AppRoles.Director,
            AppRoles.DepartmentManager,
            AppRoles.Employee
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(
                    await roleManager.CreateAsync(new IdentityRole(role)),
                    $"create role '{role}'");
            }
        }

        var systemAdmin = await userManager.FindByEmailAsync(SystemAdminEmail);
        if (systemAdmin is null)
        {
            systemAdmin = new ApplicationUser
            {
                Email = SystemAdminEmail,
                UserName = SystemAdminEmail,
                FullName = "System Administrator",
                IsActive = true,
                CompanyId = null,
                DepartmentId = null,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            EnsureSucceeded(
                await userManager.CreateAsync(systemAdmin, SystemAdminPassword),
                "create the default SystemAdmin user");
        }

        if (!await userManager.IsInRoleAsync(systemAdmin, AppRoles.SystemAdmin))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(systemAdmin, AppRoles.SystemAdmin),
                "assign the SystemAdmin role to the default user");
        }

        await SeedSubscriptionPlansAsync(context);
    }

    private static async Task SeedSubscriptionPlansAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var plans = new[]
        {
            new SubscriptionPlan
            {
                Name = "Basic",
                Code = "BASIC",
                Description = "Gói nền tảng dành cho doanh nghiệp nhỏ bắt đầu quản lý công việc tập trung.",
                Price = 199000m,
                DurationDays = 30,
                MaxEmployees = 30,
                MaxDepartments = 5,
                EnableAI = false,
                EnableRealtimeChat = true,
                IsActive = true,
                CreatedAt = now
            },
            new SubscriptionPlan
            {
                Name = "Pro",
                Code = "PRO",
                Description = "Gói mở rộng cho doanh nghiệp đang tăng trưởng với đầy đủ tính năng cộng tác.",
                Price = 499000m,
                DurationDays = 30,
                MaxEmployees = 100,
                MaxDepartments = 20,
                EnableAI = true,
                EnableRealtimeChat = true,
                IsActive = true,
                CreatedAt = now
            },
            new SubscriptionPlan
            {
                Name = "Enterprise",
                Code = "ENTERPRISE",
                Description = "Gói quy mô lớn dành cho tổ chức cần giới hạn vận hành cao và tính năng nâng cao.",
                Price = 999000m,
                DurationDays = 30,
                MaxEmployees = 500,
                MaxDepartments = 100,
                EnableAI = true,
                EnableRealtimeChat = true,
                IsActive = true,
                CreatedAt = now
            }
        };

        var existingCodes = await context.SubscriptionPlans.AsNoTracking()
            .Select(plan => plan.Code)
            .ToListAsync();
        var missingPlans = plans
            .Where(plan => !existingCodes.Contains(plan.Code, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingPlans.Count == 0)
        {
            return;
        }

        context.SubscriptionPlans.AddRange(missingPlans);
        await context.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Failed to {operation}: {errors}");
    }
}
