using EnterpriseTask.Mobile.Models.Onboarding;

namespace EnterpriseTask.Mobile.Services.Onboarding;

public sealed class OnboardingState
{
    public SubscriptionPlanPublicDto? SelectedPlan { get; set; }

    public CompanyOnboardingResponse? PurchaseResult { get; set; }

    public void Reset()
    {
        SelectedPlan = null;
        PurchaseResult = null;
    }
}
