using EnterpriseTask.Mobile.Models;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Api;

namespace EnterpriseTask.Mobile.Services.Mobile;

public sealed class MobileWorkspaceService(ApiClient apiClient)
{
    public async Task<MobileCurrentUserDto> GetMeAsync(CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<MobileCurrentUserDto>>("api/mobile/me", cancellationToken),
            "Không thể tải thông tin người dùng.");

    public async Task<MobileDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<MobileDashboardDto>>("api/mobile/dashboard", cancellationToken),
            "Không thể tải dashboard.");

    public async Task<MobileCompanyDto> GetCompanyAsync(CancellationToken cancellationToken = default) =>
        MobileApiResponseReader.RequireData(
            await apiClient.GetAsync<ApiResponse<MobileCompanyDto>>("api/mobile/company", cancellationToken),
            "Không thể tải thông tin công ty.");

    public async Task<MobileDepartmentDto?> GetMyDepartmentAsync(CancellationToken cancellationToken = default)
    {
        var response = await apiClient.GetAsync<ApiResponse<MobileDepartmentDto?>>(
            "api/mobile/my-department",
            cancellationToken);
        if (!response.Success)
        {
            throw new ApiException(response.Message);
        }

        return response.Data;
    }
}
