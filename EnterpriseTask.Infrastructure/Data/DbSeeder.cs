using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
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
