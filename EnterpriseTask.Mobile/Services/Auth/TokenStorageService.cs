namespace EnterpriseTask.Mobile.Services.Auth;

public sealed class TokenStorageService
{
    private const string AccessTokenKey = "enterprisetask_access_token";

    public Task SaveTokenAsync(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return SecureStorage.Default.SetAsync(AccessTokenKey, token);
    }

    public Task<string?> GetTokenAsync()
    {
        return SecureStorage.Default.GetAsync(AccessTokenKey);
    }

    public Task ClearTokenAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        return Task.CompletedTask;
    }
}
