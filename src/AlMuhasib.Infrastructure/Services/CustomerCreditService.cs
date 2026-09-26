using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class CustomerCreditService : ICustomerCreditService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CustomerCreditService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public Task<CreditCheckResult> CheckCreditAsync(int customerId, decimal additionalAmount, bool isInstallment)
        => CheckCreditAsync(customerId, additionalAmount, isInstallment, AccountingCurrency.IQD, 1m);

    public async Task<CreditCheckResult> CheckCreditAsync(
        int customerId,
        decimal additionalAmount,
        bool isInstallment,
        AccountingCurrency currency,
        decimal fxRate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer is null)
            return new CreditCheckResult { IsAllowed = false, Message = "العميل غير موجود" };

        decimal? limit = isInstallment ? customer.MaxInstallmentDebt : customer.MaxCreditLimit;

        decimal currentDebtIqd;
        try
        {
            if (isInstallment)
            {
                var rows = await context.Installments.AsNoTracking()
                    .Where(i => i.InstallmentPlan!.CustomerId == customerId && i.RemainingAmount > 0)
                    .Select(i => new
                    {
                        i.RemainingAmount,
                        Currency = i.InstallmentPlan!.Invoice!.Currency,
                        FxRate = i.InstallmentPlan.Invoice.FxRate
                    })
                    .ToListAsync();
                currentDebtIqd = rows.Sum(r => DebtToIqd(r.RemainingAmount, r.Currency, r.FxRate));
            }
            else
            {
                var rows = await context.Invoices.AsNoTracking()
                    .Where(i => i.CustomerId == customerId &&
                                i.PaymentMethod == PaymentMethod.Credit &&
                                i.RemainingAmount > 0)
                    .Select(i => new { i.RemainingAmount, i.Currency, i.FxRate })
                    .ToListAsync();
                currentDebtIqd = rows.Sum(r => DebtToIqd(r.RemainingAmount, r.Currency, r.FxRate));
            }
        }
        catch (InvalidOperationException ex)
        {
            return new CreditCheckResult
            {
                IsAllowed = false,
                Message = $"لا يمكن احتساب الدين الحالي: {ex.Message}",
                Limit = limit
            };
        }

        decimal additionalIqd;
        try
        {
            additionalIqd = AccountingCurrencyRules.ToBaseIqdStrict(additionalAmount, currency, fxRate);
        }
        catch (InvalidOperationException ex)
        {
            return new CreditCheckResult
            {
                IsAllowed = false,
                Message = ex.Message,
                CurrentDebt = currentDebtIqd,
                Limit = limit
            };
        }

        if (limit is null or <= 0)
            return new CreditCheckResult { IsAllowed = true, CurrentDebt = currentDebtIqd, Limit = limit };

        var projected = currentDebtIqd + additionalIqd;
        if (projected > limit)
        {
            var currencyHint = currency == AccountingCurrency.USD
                ? $" (فاتورة ${additionalAmount:N2} ≈ {additionalIqd:N0} د.ع)"
                : string.Empty;
            return new CreditCheckResult
            {
                IsAllowed = false,
                CurrentDebt = currentDebtIqd,
                Limit = limit,
                Message =
                    $"تجاوز حد الائتمان: الدين الحالي {currentDebtIqd:N0} + الجديد {additionalIqd:N0}{currencyHint} > الحد {limit:N0} د.ع"
            };
        }

        return new CreditCheckResult { IsAllowed = true, CurrentDebt = currentDebtIqd, Limit = limit };
    }

    private static decimal DebtToIqd(decimal amount, AccountingCurrency currency, decimal fxRate)
    {
        if (amount <= 0) return 0;
        // لا نخفي دين الدولار بصمت عند FxRate غير صالح — يُحسب بصرامة أو يُرفض
        return AccountingCurrencyRules.ToBaseIqdStrict(amount, currency, fxRate);
    }

    public async Task UpdateReliabilityScoreAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer is null) return;

        var installments = await context.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan)
            .Where(i => i.InstallmentPlan!.CustomerId == customerId)
            .ToListAsync();

        if (installments.Count == 0) return;

        var paidOnTime = installments.Count(i => i.Status == InstallmentStatus.Paid && i.PaymentDate <= i.DueDate);
        var overdue = installments.Count(i => i.Status == InstallmentStatus.Overdue);
        var score = (int)Math.Clamp(50 + paidOnTime * 5 - overdue * 10, 0, 100);
        customer.ReliabilityScore = score;
        await context.SaveChangesAsync();
    }
}
