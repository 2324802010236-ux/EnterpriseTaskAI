using EnterpriseTask.Mobile.Models;
using EnterpriseTask.Mobile.Services.Api;

namespace EnterpriseTask.Mobile.Services.Mobile;

internal static class MobileApiResponseReader
{
    public static T RequireData<T>(ApiResponse<T> response, string fallbackMessage)
    {
        if (!response.Success || response.Data is null)
        {
            throw new ApiException(string.IsNullOrWhiteSpace(response.Message)
                ? fallbackMessage
                : response.Message);
        }

        return response.Data;
    }
}
