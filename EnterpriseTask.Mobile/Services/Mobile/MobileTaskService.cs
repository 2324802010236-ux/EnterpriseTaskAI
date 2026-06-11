using EnterpriseTask.Mobile.Models;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Api;

namespace EnterpriseTask.Mobile.Services.Mobile;

public sealed class MobileTaskService(ApiClient apiClient)
{
    public async Task<List<MobileTaskListItemDto>> GetTasksAsync(CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<List<MobileTaskListItemDto>>>("api/mobile/tasks", cancellationToken),
            "Không thể tải danh sách công việc.");

    public async Task<MobileTaskDetailsDto> GetTaskDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<MobileTaskDetailsDto>>($"api/mobile/tasks/{id}", cancellationToken),
            "Không thể tải chi tiết công việc.");

    public async Task<MobileTaskDetailsDto> UpdateStatusAsync(
        int id,
        string status,
        string? note,
        CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.PostAsync<UpdateMobileTaskStatusRequest, ApiResponse<MobileTaskDetailsDto>>(
                $"api/mobile/tasks/{id}/status",
                new UpdateMobileTaskStatusRequest { Status = status, Note = note },
                cancellationToken),
            "Không thể cập nhật trạng thái công việc.");

    public async Task<MobileTaskCommentDto> AddCommentAsync(
        int id,
        string content,
        CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.PostAsync<CreateMobileTaskCommentRequest, ApiResponse<MobileTaskCommentDto>>(
                $"api/mobile/tasks/{id}/comments",
                new CreateMobileTaskCommentRequest { Content = content },
                cancellationToken),
            "Không thể gửi bình luận.");
}
