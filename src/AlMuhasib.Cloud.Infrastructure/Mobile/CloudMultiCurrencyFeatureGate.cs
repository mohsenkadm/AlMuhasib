using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Mobile;

/// <summary>فرض MultiCurrencyEnabled من إعدادات المستأجر على كتابات الموبايل.</summary>
public static class CloudMultiCurrencyFeatureGate
{
    public static async Task<bool> IsEnabledAsync(CloudDbContext db, int tenantId, CancellationToken ct = default)
    {
        return await db.BusinessSettings.AsNoTracking()
                   .Where(s => s.TenantId == tenantId)
                   .Select(s => (bool?)s.MultiCurrencyEnabled)
                   .FirstOrDefaultAsync(ct)
               ?? false;
    }

    public static async Task<(AccountingCurrency Currency, decimal FxRate)> ApplyForWriteAsync(
        CloudDbContext db,
        int tenantId,
        AccountingCurrency currency,
        decimal fxRate,
        string contextLabel,
        CancellationToken ct = default)
    {
        var enabled = await IsEnabledAsync(db, tenantId, ct);
        return AccountingCurrencyRules.ApplyFeatureGate(enabled, currency, fxRate, contextLabel);
    }
}
