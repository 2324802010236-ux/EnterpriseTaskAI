namespace EnterpriseTask.Admin.Services;

public enum CompanyPortalAccessResult
{
    Allowed,
    Unauthenticated,
    AccessDenied,
    SubscriptionRequired,
    SubscriptionExpired,
    Suspended
}
