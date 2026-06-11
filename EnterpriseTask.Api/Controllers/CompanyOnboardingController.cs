using System.Security.Cryptography;
using EnterpriseTask.Api.DTOs.Onboarding;
using EnterpriseTask.Api.Services;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Domain.Constants;
using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Domain.Enums;
using EnterpriseTask.Infrastructure.Data;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/company-onboarding")]
public class CompanyOnboardingController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IEmailDeliveryService emailDeliveryService,
    ILogger<CompanyOnboardingController> logger) : ControllerBase
{
    [HttpGet("subscription-plans")]
    public async Task<ActionResult<ApiResponse<List<SubscriptionPlanPublicDto>>>> GetSubscriptionPlans(
        CancellationToken cancellationToken)
    {
        var plans = await context.SubscriptionPlans.AsNoTracking()
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.Price)
            .Select(plan => new SubscriptionPlanPublicDto
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
                EnableRealtimeChat = plan.EnableRealtimeChat
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<SubscriptionPlanPublicDto>>.Succeeded(
            plans,
            "Active subscription plans retrieved successfully."));
    }

    [HttpPost("purchase")]
    public async Task<ActionResult<ApiResponse<CompanyOnboardingResponse>>> Purchase(
        CompanyOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        NormalizeRequest(request);

        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return BadRequest(FailedResponse("Company name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.AdminFullName))
        {
            return BadRequest(FailedResponse("Admin full name is required."));
        }

        var plan = await context.SubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.SubscriptionPlanId && item.IsActive,
                cancellationToken);
        if (plan is null)
        {
            return BadRequest(FailedResponse("Subscription plan does not exist or is inactive."));
        }

        if (await context.Companies.AsNoTracking()
            .AnyAsync(company => company.Email == request.CompanyEmail, cancellationToken))
        {
            return BadRequest(FailedResponse("Company email already exists."));
        }

        if (await userManager.FindByEmailAsync(request.AdminEmail) is not null)
        {
            return BadRequest(FailedResponse("Admin email already exists."));
        }

        var now = DateTime.UtcNow;
        var temporaryPassword = GenerateTemporaryPassword();
        var webAdminUrl = BuildWebAdminUrl();

        Company company;
        CompanySubscription subscription;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            company = new Company
            {
                Name = request.CompanyName,
                TaxCode = request.TaxCode,
                Email = request.CompanyEmail,
                Phone = request.CompanyPhone,
                Address = request.CompanyAddress,
                Industry = request.Industry,
                Status = CompanyStatus.Active,
                CreatedAt = now
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync(cancellationToken);

            subscription = new CompanySubscription
            {
                CompanyId = company.Id,
                SubscriptionPlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                EndDate = now.AddDays(plan.DurationDays),
                MaxEmployees = plan.MaxEmployees,
                MaxDepartments = plan.MaxDepartments,
                EnableAI = plan.EnableAI,
                EnableRealtimeChat = plan.EnableRealtimeChat,
                CreatedAt = now
            };

            context.CompanySubscriptions.Add(subscription);
            await context.SaveChangesAsync(cancellationToken);

            context.PaymentTransactions.Add(new PaymentTransaction
            {
                CompanyId = company.Id,
                CompanySubscriptionId = subscription.Id,
                Amount = plan.Price,
                PaymentMethod = "SIMULATED",
                Status = PaymentStatus.Success,
                TransactionCode = GenerateTransactionCode(now),
                Note = "Simulated payment for demo onboarding.",
                CreatedAt = now,
                PaidAt = now
            });
            await context.SaveChangesAsync(cancellationToken);

            var admin = new ApplicationUser
            {
                UserName = request.AdminEmail,
                Email = request.AdminEmail,
                FullName = request.AdminFullName,
                PhoneNumber = request.AdminPhone,
                CompanyId = company.Id,
                DepartmentId = null,
                Position = "Company Administrator",
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = now
            };

            var createUserResult = await userManager.CreateAsync(admin, temporaryPassword);
            if (!createUserResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return BadRequest(FailedResponse(FormatIdentityErrors(createUserResult)));
            }

            var addRoleResult = await userManager.AddToRoleAsync(admin, AppRoles.CompanyAdmin);
            if (!addRoleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return BadRequest(FailedResponse(FormatIdentityErrors(addRoleResult)));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(exception, "Company onboarding purchase failed for {AdminEmail}.", request.AdminEmail);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                FailedResponse("Company onboarding failed. No data was created."));
        }

        var emailSent = await TrySendWelcomeEmailAsync(
            request,
            plan,
            subscription,
            webAdminUrl,
            temporaryPassword);
        var message = emailSent
            ? "Đăng ký gói thành công. Thông tin tài khoản quản trị đã được gửi qua email."
            : "Đăng ký gói thành công nhưng gửi email thất bại. Vui lòng kiểm tra cấu hình SMTP.";

        // Demo only. In production, use set-password link instead of temporary password.
        var response = new CompanyOnboardingResponse
        {
            Success = true,
            Message = message,
            CompanyId = company.Id,
            SubscriptionId = subscription.Id,
            AdminEmail = request.AdminEmail,
            TemporaryPassword = temporaryPassword,
            WebAdminUrl = webAdminUrl
        };

        return Ok(ApiResponse<CompanyOnboardingResponse>.Succeeded(response, response.Message));
    }

    private async Task<bool> TrySendWelcomeEmailAsync(
        CompanyOnboardingRequest request,
        SubscriptionPlan plan,
        CompanySubscription subscription,
        string webAdminUrl,
        string temporaryPassword)
    {
        try
        {
            await emailDeliveryService.SendEmailAsync(
                request.AdminEmail,
                "Tài khoản quản trị WorkFlow AI của công ty bạn đã được kích hoạt",
                CompanyOnboardingEmailTemplates.BuildCompanyAdminWelcomeEmail(
                    request.CompanyName,
                    request.AdminFullName,
                    plan.Name,
                    plan.Price,
                    subscription.StartDate,
                    subscription.EndDate,
                    webAdminUrl,
                    request.AdminEmail,
                    temporaryPassword));
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Company onboarding succeeded, but welcome email delivery to {AdminEmail} failed.",
                request.AdminEmail);
            return false;
        }
    }

    private string BuildWebAdminUrl()
    {
        var baseUrl = configuration["WebAdmin:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:5151";
        }

        return $"{baseUrl}/company/dashboard";
    }

    private static ApiResponse<CompanyOnboardingResponse> FailedResponse(string message) =>
        ApiResponse<CompanyOnboardingResponse>.Failed(message);

    private static string GenerateTemporaryPassword() =>
        $"Admin@{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";

    private static string GenerateTransactionCode(DateTime timestamp) =>
        $"SIM-{timestamp:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(0, 10_000):D4}";

    private static string FormatIdentityErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));

    private static void NormalizeRequest(CompanyOnboardingRequest request)
    {
        request.CompanyName = request.CompanyName.Trim();
        request.CompanyEmail = request.CompanyEmail.Trim();
        request.AdminFullName = request.AdminFullName.Trim();
        request.AdminEmail = request.AdminEmail.Trim();
        request.TaxCode = NormalizeOptional(request.TaxCode);
        request.CompanyPhone = NormalizeOptional(request.CompanyPhone);
        request.CompanyAddress = NormalizeOptional(request.CompanyAddress);
        request.Industry = NormalizeOptional(request.Industry);
        request.AdminPhone = NormalizeOptional(request.AdminPhone);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
