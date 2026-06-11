using EnterpriseTask.Mobile.Models;
using EnterpriseTask.Mobile.Models.Onboarding;
using EnterpriseTask.Mobile.Services.Api;

namespace EnterpriseTask.Mobile.Services.Onboarding;

public sealed class OnboardingService(ApiClient apiClient)
{
    public async Task<List<SubscriptionPlanPublicDto>> GetPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await apiClient.GetAsync<ApiResponse<List<SubscriptionPlanPublicDto>>>(
            "api/company-onboarding/subscription-plans",
            cancellationToken);

        if (!response.Success)
        {
            throw new ApiException(string.IsNullOrWhiteSpace(response.Message)
                ? "Không thể tải danh sách gói dịch vụ."
                : response.Message);
        }

        return response.Data ?? [];
    }

    public async Task<CompanyOnboardingResponse> PurchaseAsync(
        CompanyOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await apiClient.PostAsync<CompanyOnboardingRequest, ApiResponse<CompanyOnboardingResponse>>(
            "api/company-onboarding/purchase",
            request,
            cancellationToken);

        if (!response.Success || response.Data is null || !response.Data.Success)
        {
            throw new ApiException(string.IsNullOrWhiteSpace(response.Message)
                ? "Không thể hoàn tất đăng ký gói."
                : response.Message);
        }

        return response.Data;
    }
}
