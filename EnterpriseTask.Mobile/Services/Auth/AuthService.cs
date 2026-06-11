using EnterpriseTask.Mobile.Models;
using EnterpriseTask.Mobile.Models.Auth;
using EnterpriseTask.Mobile.Services.Api;

namespace EnterpriseTask.Mobile.Services.Auth;

public sealed class AuthService(ApiClient apiClient, TokenStorageService tokenStorageService)
{
    public async Task<LoginResponse> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var response = await apiClient.PostAsync<LoginRequest, ApiResponse<LoginResponse>>(
            "api/auth/login",
            new LoginRequest
            {
                Email = email.Trim(),
                Password = password
            },
            cancellationToken);

        if (!response.Success || response.Data is null || string.IsNullOrWhiteSpace(response.Data.Token))
        {
            throw new ApiException(string.IsNullOrWhiteSpace(response.Message)
                ? "Đăng nhập không thành công."
                : response.Message);
        }

        await tokenStorageService.SaveTokenAsync(response.Data.Token);
        return response.Data;
    }

    public Task LogoutAsync()
    {
        return tokenStorageService.ClearTokenAsync();
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var token = await tokenStorageService.GetTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }
}
