using AlMuhasib.Core.Enums;
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

    public async Task<CreditCheckResult> CheckCreditAsync(int customerId, decimal additionalAmount, bool isInstallment)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer is null)
            return new CreditCheckResult { IsAllowed = false, Message = "العميل غير موجود" };

        decimal currentDebt;
        decimal? limit;

        if (isInstallment)
        {
            currentDebt = await context.Installments.AsNoTracking()
                .Include(i => i.InstallmentPlan)
                .Where(i => i.InstallmentPlan!.CustomerId == customerId && i.RemainingAmount > 0)
                .SumAsync(i => i.RemainingAmount);
            limit = customer.MaxInstallmentDebt;
        }
        else
        {
            currentDebt = await context.Invoices.AsNoTracking()
                .Where(i => i.CustomerId == customerId && i.PaymentMethod == PaymentMethod.Credit && i.RemainingAmount > 0)
                .SumAsync(i => i.RemainingAmount);
            limit = customer.MaxCreditLimit;
        }

        if (limit is null or <= 0)
            return new CreditCheckResult { IsAllowed = true, CurrentDebt = currentDebt, Limit = limit };

        var projected = currentDebt + additionalAmount;
        if (projected > limit)
        {
            return new CreditCheckResult
            {
                IsAllowed = false,
                CurrentDebt = currentDebt,
                Limit = limit,
                Message = $"تجاوز حد الائتمان: الدين الحالي {currentDebt:N0} + الجديد {additionalAmount:N0} > الحد {limit:N0} د.ع"
            };
        }

        return new CreditCheckResult { IsAllowed = true, CurrentDebt = currentDebt, Limit = limit };
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
