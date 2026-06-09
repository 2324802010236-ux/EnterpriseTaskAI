using EnterpriseTask.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Admin.Controllers;

[Authorize(Roles = AppRoles.CompanyAdmin)]
[Route("company")]
public class CompanyAccessController : Controller
{
    [HttpGet("subscription-required")]
    public IActionResult SubscriptionRequired()
    {
        return View();
    }

    [HttpGet("subscription-expired")]
    public IActionResult SubscriptionExpired()
    {
        return View();
    }

    [HttpGet("suspended")]
    public IActionResult Suspended()
    {
        return View();
    }
}
