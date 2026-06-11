using EnterpriseTask.Mobile.Models;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Api;

namespace EnterpriseTask.Mobile.Services.Mobile;

public sealed class MobileNotificationService(ApiClient apiClient)
{
    public async Task<List<MobileNotificationDto>> GetNotificationsAsync(
        bool unreadOnly = false,
        CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<List<MobileNotificationDto>>>(
                $"api/mobile/notifications?unreadOnly={unreadOnly.ToString().ToLowerInvariant()}&page=1&pageSize=100",
                cancellationToken),
            "Không thể tải thông báo.");

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<UnreadNotificationCountDto>>(
                "api/mobile/notifications/unread-count",
                cancellationToken),
            "Không thể tải số thông báo chưa đọc.").Count;

    public async Task<MobileNotificationDto> MarkReadAsync(int id, CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.PostAsync<ApiResponse<MobileNotificationDto>>(
                $"api/mobile/notifications/{id}/read",
                cancellationToken),
            "Không thể đánh dấu thông báo.");

    public async Task ReadAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await apiClient.PostAsync<ApiResponse<object?>>(
            "api/mobile/notifications/read-all",
            cancellationToken);
        if (!response.Success)
        {
            throw new ApiException(response.Message);
        }
    }
}
