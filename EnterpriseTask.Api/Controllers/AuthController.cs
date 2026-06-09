using EnterpriseTask.Api.Services;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.DTOs.Auth;
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

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    JwtTokenService jwtTokenService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("register-company")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterCompany(
        RegisterCompanyRequest request)
    {
        var adminEmail = request.AdminEmail.Trim();
        var companyEmail = request.CompanyEmail.Trim();

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return BadRequest(ApiResponse<AuthResponse>.Failed("Admin email already exists."));
        }

        if (await context.Companies.AnyAsync(company => company.Email == companyEmail))
        {
            return BadRequest(ApiResponse<AuthResponse>.Failed("Company email already exists."));
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        var company = new Company
        {
            Name = request.CompanyName.Trim(),
            TaxCode = request.TaxCode?.Trim(),
            Email = companyEmail,
            Phone = request.CompanyPhone?.Trim(),
            Address = request.Address?.Trim(),
            Industry = request.Industry?.Trim(),
            Status = CompanyStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = request.AdminFullName.Trim(),
            PhoneNumber = request.AdminPhone?.Trim(),
            CompanyId = company.Id,
            DepartmentId = null,
            Position = "Company Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var createUserResult = await userManager.CreateAsync(user, request.AdminPassword);
        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(ApiResponse<AuthResponse>.Failed(FormatIdentityErrors(createUserResult)));
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, AppRoles.CompanyAdmin);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return BadRequest(ApiResponse<AuthResponse>.Failed(FormatIdentityErrors(addRoleResult)));
        }

        var authResponse = await jwtTokenService.GenerateTokenAsync(user);
        await transaction.CommitAsync();

        return Ok(ApiResponse<AuthResponse>.Succeeded(
            authResponse,
            "Company and CompanyAdmin account registered successfully."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Failed("Invalid email or password."));
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(ApiResponse<AuthResponse>.Failed("Invalid email or password."));
        }

        if (user.CompanyId.HasValue)
        {
            var companyIsActive = await context.Companies
                .AnyAsync(company =>
                    company.Id == user.CompanyId.Value &&
                    company.Status == CompanyStatus.Active);

            if (!companyIsActive)
            {
                return Unauthorized(ApiResponse<AuthResponse>.Failed("Company account is not active."));
            }
        }

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var authResponse = await jwtTokenService.GenerateTokenAsync(user);
        return Ok(ApiResponse<AuthResponse>.Succeeded(authResponse, "Login successful."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> Me()
    {
        if (currentUserService.UserId is null)
        {
            return Unauthorized(ApiResponse<CurrentUserResponse>.Failed("Current user is unavailable."));
        }

        var user = await userManager.FindByIdAsync(currentUserService.UserId);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<CurrentUserResponse>.Failed("Current user is unavailable."));
        }

        var roles = await userManager.GetRolesAsync(user);
        var companyName = user.CompanyId.HasValue
            ? await context.Companies
                .Where(company => company.Id == user.CompanyId.Value)
                .Select(company => company.Name)
                .FirstOrDefaultAsync()
            : null;
        var departmentName = user.DepartmentId.HasValue
            ? await context.Departments
                .Where(department => department.Id == user.DepartmentId.Value)
                .Select(department => department.Name)
                .FirstOrDefaultAsync()
            : null;

        var response = new CurrentUserResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty,
            CompanyId = user.CompanyId,
            DepartmentId = user.DepartmentId,
            CompanyName = companyName,
            DepartmentName = departmentName
        };

        return Ok(ApiResponse<CurrentUserResponse>.Succeeded(response, "Current user retrieved successfully."));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object?>>> ChangePassword(ChangePasswordRequest request)
    {
        if (currentUserService.UserId is null)
        {
            return Unauthorized(ApiResponse<object?>.Failed("Current user is unavailable."));
        }

        var user = await userManager.FindByIdAsync(currentUserService.UserId);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<object?>.Failed("Current user is unavailable."));
        }

        var result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<object?>.Failed(FormatIdentityErrors(result)));
        }

        return Ok(ApiResponse<object?>.Succeeded(null, "Password changed successfully."));
    }

    private static string FormatIdentityErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));
}
