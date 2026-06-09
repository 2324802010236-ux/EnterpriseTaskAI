using EnterpriseTask.Admin.ViewModels.Auth;
using EnterpriseTask.Admin.Services;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Admin.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    CompanyPortalAccessService companyPortalAccessService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var existingPortalRedirect = await GetPortalRedirectAsync();
            if (existingPortalRedirect is not null)
            {
                return existingPortalRedirect;
            }

            await signInManager.SignOutAsync();
        }

        return View(new AdminLoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var existingPortalRedirect = await GetPortalRedirectAsync();
            if (existingPortalRedirect is not null)
            {
                return existingPortalRedirect;
            }

            await signInManager.SignOutAsync();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị vô hiệu hóa.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        var portalRedirect = await GetPortalRedirectAsync(user);
        if (portalRedirect is null)
        {
            await signInManager.SignOutAsync();
            ModelState.AddModelError(string.Empty, "Tài khoản không có quyền truy cập Web Admin.");
            return View(model);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return portalRedirect;
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<IActionResult?> GetPortalRedirectAsync(ApplicationUser? user = null)
    {
        user ??= await userManager.GetUserAsync(User);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        if (await userManager.IsInRoleAsync(user, AppRoles.SystemAdmin))
        {
            return Redirect("/owner/dashboard");
        }

        if (await userManager.IsInRoleAsync(user, AppRoles.CompanyAdmin))
        {
            var accessResult = await companyPortalAccessService.CheckAccessAsync(user);
            return Redirect(CompanyPortalAccessService.GetRedirectPath(accessResult));
        }

        return null;
    }
}
