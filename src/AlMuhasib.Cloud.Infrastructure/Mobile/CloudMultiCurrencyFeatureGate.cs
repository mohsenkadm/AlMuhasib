using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Mobile;

/// <summary>فرض MultiCurrencyEnabled من إعدادات المستأجر على كتابات الموبايل/المزامنة.</summary>
public static class CloudMultiCurrencyFeatureGate
{
    public static async Task<bool> IsEnabledAsync(
        CloudDbContext db,
        int tenantId,
        CancellationToken ct = default)
        => await IsEnabledAsync(db, tenantId, writeBranchId: null, ct);

    public static async Task<bool> IsEnabledAsync(
        CloudDbContext db,
        int tenantId,
        int? writeBranchId,
        CancellationToken ct = default)
    {
        if (writeBranchId is > 0)
        {
            var branchFlag = await db.BusinessSettings.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.BranchId == writeBranchId.Value)
                .Select(s => (bool?)s.MultiCurrencyEnabled)
                .FirstOrDefaultAsync(ct);
            if (branchFlag.HasValue)
                return branchFlag.Value;
        }

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

    public static async Task<(AccountingCurrency Currency, decimal FxRate)> ApplyForWriteAsync(
        CloudDbContext db,
        int tenantId,
        int? writeBranchId,
        AccountingCurrency currency,
        decimal fxRate,
        string contextLabel,
        CancellationToken ct = default)
    {
        var enabled = await IsEnabledAsync(db, tenantId, writeBranchId, ct);
        return AccountingCurrencyRules.ApplyFeatureGate(enabled, currency, fxRate, contextLabel);
    }
}
