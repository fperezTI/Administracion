namespace AssetManagement.Infrastructure.Security;

/// <summary>HttpContext.Items keys populated once per request by CurrentUserProvisioningMiddleware and
/// read synchronously afterwards by HttpContextCurrentUserContext / HttpContextCurrentCompanyContext.</summary>
internal static class RequestContextKeys
{
    public const string LocalUserId = "AssetManagement.LocalUserId";
    public const string EntraObjectId = "AssetManagement.EntraObjectId";
    public const string DisplayName = "AssetManagement.DisplayName";
    public const string Email = "AssetManagement.Email";
    public const string ActiveCompanyId = "AssetManagement.ActiveCompanyId";
    public const string AccessibleCompanyIds = "AssetManagement.AccessibleCompanyIds";
}
