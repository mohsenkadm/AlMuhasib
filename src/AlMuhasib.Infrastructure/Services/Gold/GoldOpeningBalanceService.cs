using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldOpeningBalanceService : IGoldOpeningBalanceService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldOpeningBalanceService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<GoldStockBalance> SetOpeningStockAsync(
        GoldOpeningStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.KaratValue <= 0)
            throw new InvalidOperationException("اختر العيار");
        if (request.GramsOnHand < 0)
            throw new InvalidOperationException("رصيد الافتتاح لا يمكن أن يكون سالباً");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var warehouseId = await GoldWarehouseService.ResolveWarehouseIdAsync(
                context, request.WarehouseId, cancellationToken);

            var balance = await context.GoldStockBalances
                .FirstOrDefaultAsync(
                    s => s.WarehouseId == warehouseId && s.KaratValue == request.KaratValue,
                    cancellationToken);

            var current = balance?.GramsOnHand ?? 0m;
            var delta = request.GramsOnHand - current;

            if (delta != 0 || balance is null)
            {
                await GoldInventoryService.AdjustStockInternalAsync(
                    context,
                    request.KaratValue,
                    delta == 0 && balance is null ? request.GramsOnHand : delta,
                    request.CostPerGram,
                    warehouseId,
                    cancellationToken);

                // Ensure absolute target after adjust (guards rounding / first create)
                balance = await context.GoldStockBalances
                    .FirstAsync(s => s.WarehouseId == warehouseId && s.KaratValue == request.KaratValue, cancellationToken);
                balance.GramsOnHand = GoldCurrencyHelper.Round(request.GramsOnHand, 4);
                if (request.CostPerGram is > 0)
                    balance.AverageCostPerGram = request.CostPerGram.Value;
            }

            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return await context.GoldStockBalances.AsNoTracking()
                .FirstAsync(s => s.WarehouseId == warehouseId && s.KaratValue == request.KaratValue, cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GoldCustomer> SetCustomerOpeningBalanceAsync(
        GoldOpeningCustomerBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CustomerId <= 0)
            throw new InvalidOperationException("اختر الزبون");
        if (request.CreditBalanceIqd < 0 || request.CreditBalanceUsd < 0 || request.GoldCreditGrams < 0)
            throw new InvalidOperationException("أرصدة الافتتاح لا يمكن أن تكون سالبة");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await context.GoldCustomers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("الزبون غير موجود");

        customer.CreditBalanceIqd = GoldCurrencyHelper.Round(request.CreditBalanceIqd);
        customer.CreditBalanceUsd = GoldCurrencyHelper.Round(request.CreditBalanceUsd);
        customer.GoldCreditGrams = GoldCurrencyHelper.Round(request.GoldCreditGrams, 3);
        if (!string.IsNullOrWhiteSpace(request.Notes))
            customer.Notes = string.IsNullOrWhiteSpace(customer.Notes)
                ? $"[افتتاح] {request.Notes.Trim()}"
                : $"{customer.Notes}\n[افتتاح] {request.Notes.Trim()}";

        await context.SaveChangesAsync(cancellationToken);
        return customer;
    }
}
