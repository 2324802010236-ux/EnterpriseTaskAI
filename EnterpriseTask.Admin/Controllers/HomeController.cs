using System.Diagnostics;
using EnterpriseTask.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using EnterpriseTask.Admin.Models;

namespace EnterpriseTask.Admin.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        if (User.IsInRole(AppRoles.SystemAdmin))
        {
            return Redirect("/owner/dashboard");
        }

        if (User.IsInRole(AppRoles.CompanyAdmin))
        {
            return Redirect("/company/dashboard");
        }

        return RedirectToAction("AccessDenied", "Account");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
