using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseTask.Mobile.Services.Auth;

namespace EnterpriseTask.Mobile.Services.Api;

public sealed class ApiClient(HttpClient httpClient, TokenStorageService tokenStorageService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        await AttachBearerTokenAsync(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest requestBody,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };
        await AttachBearerTokenAsync(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse> PostAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        await AttachBearerTokenAsync(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task AttachBearerTokenAsync(HttpRequestMessage request)
    {
        var token = await tokenStorageService.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static async Task<T> ReadResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadErrorMessageAsync(response, cancellationToken);
            throw new ApiException(message, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new ApiException("Máy chủ trả về dữ liệu không hợp lệ.", response.StatusCode);
    }

    private static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Yêu cầu thất bại ({(int)response.StatusCode}).";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString() ?? "Yêu cầu thất bại.";
            }
        }
        catch (JsonException)
        {
            // Keep unexpected response bodies out of user-facing errors.
        }

        return $"Yêu cầu thất bại ({(int)response.StatusCode}).";
    }
}
