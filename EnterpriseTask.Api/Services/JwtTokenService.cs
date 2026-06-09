using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnterpriseTask.Application.DTOs.Auth;
using EnterpriseTask.Infrastructure.Identity;
using EnterpriseTask.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseTask.Api.Services;

public class JwtTokenService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<AuthResponse> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.CompanyId.HasValue)
        {
            claims.Add(new Claim("companyId", user.CompanyId.Value.ToString()));
        }

        if (user.DepartmentId.HasValue)
        {
            claims.Add(new Claim("departmentId", user.DepartmentId.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = role,
            CompanyId = user.CompanyId,
            DepartmentId = user.DepartmentId
        };
    }
}
