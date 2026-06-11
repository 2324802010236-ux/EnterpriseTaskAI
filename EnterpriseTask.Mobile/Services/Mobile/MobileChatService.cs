using EnterpriseTask.Mobile.Models;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Api;

namespace EnterpriseTask.Mobile.Services.Mobile;

public sealed class MobileChatService(ApiClient apiClient)
{
    public async Task<List<MobileChatRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<List<MobileChatRoomDto>>>("api/mobile/chat/rooms", cancellationToken),
            "Không thể tải phòng chat.");

    public async Task<List<MobileChatMessageDto>> GetMessagesAsync(
        int roomId,
        CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<List<MobileChatMessageDto>>>(
                $"api/mobile/chat/rooms/{roomId}/messages?page=1&pageSize=100",
                cancellationToken),
            "Không thể tải tin nhắn.");

    public async Task<MobileChatMessageDto> SendMessageAsync(
        int roomId,
        string content,
        CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.PostAsync<SendChatMessageRequest, ApiResponse<MobileChatMessageDto>>(
                $"api/mobile/chat/rooms/{roomId}/messages",
                new SendChatMessageRequest { Content = content },
                cancellationToken),
            "Không thể gửi tin nhắn.");
}
