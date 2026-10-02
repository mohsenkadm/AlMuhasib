using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>قراءة MultiCurrencyEnabled من BusinessSettings وفرضها على عمليات الكتابة.</summary>
public static class MultiCurrencyFeatureGate
{
    public static async Task<bool> IsEnabledAsync(AppDbContext context, CancellationToken ct = default)
    {
        // إعدادات الشركة موحّدة عبر الفروع — لا تعتمد على فلتر فرع الجلسة
        // وإلا تظهر الواجهة ON (من GetOrCreate بـ Bypass) بينما الحفظ يرفض USD.
        return await context.BusinessSettings.AsNoTracking()
                   .IgnoreQueryFilters()
                   .Where(s => !s.IsDeleted)
                   .OrderBy(s => s.Id)
                   .Select(s => (bool?)s.MultiCurrencyEnabled)
                   .FirstOrDefaultAsync(ct)
               ?? false;
    }

    public static async Task<(AccountingCurrency Currency, decimal FxRate)> ApplyForWriteAsync(
        AppDbContext context,
        AccountingCurrency currency,
        decimal fxRate,
        string contextLabel,
        CancellationToken ct = default)
    {
        var enabled = await IsEnabledAsync(context, ct);
        return AccountingCurrencyRules.ApplyFeatureGate(enabled, currency, fxRate, contextLabel);
    }
}
