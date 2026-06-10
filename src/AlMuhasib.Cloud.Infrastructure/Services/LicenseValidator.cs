using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Sync;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed class LicenseValidator : ILicenseValidator
{
    public LicenseValidationResult Validate(Tenant tenant, TenantAccount account)
    {
        if (!tenant.IsActive)
            return LicenseValidationResult.Fail(SyncErrorCodes.LicenseDisabled, "العميل معطّل");

        if (!account.IsActive)
            return LicenseValidationResult.Fail(SyncErrorCodes.LicenseDisabled, "حساب التطبيق معطّل");

        if (!tenant.IsMobileEnabled)
            return LicenseValidationResult.Fail(SyncErrorCodes.SyncNotEnabled, "المزامنة غير مفعّلة لهذا العميل");

        var now = DateTime.UtcNow;
        if (tenant.LicenseExpiresAt.HasValue && tenant.LicenseExpiresAt.Value < now)
            return LicenseValidationResult.Fail(SyncErrorCodes.LicenseExpired, "انتهت صلاحية ترخيص العميل");

        if (account.ExpiresAt.HasValue && account.ExpiresAt.Value < now)
            return LicenseValidationResult.Fail(SyncErrorCodes.LicenseExpired, "انتهت صلاحية حساب التطبيق");

        return LicenseValidationResult.Ok();
    }
}
