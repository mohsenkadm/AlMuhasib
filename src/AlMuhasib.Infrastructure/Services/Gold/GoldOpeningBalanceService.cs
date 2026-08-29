using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
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
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    s => s.WarehouseId == warehouseId && s.KaratValue == request.KaratValue,
                    cancellationToken);

            if (balance is { IsDeleted: true })
                balance.RestoreFromSoftDelete("System");

            var current = balance?.GramsOnHand ?? 0m;
            var delta = request.GramsOnHand - current;

            if (delta != 0 || balance is null)
            {
                balance = await GoldInventoryService.AdjustStockInternalAsync(
                    context,
                    request.KaratValue,
                    delta == 0 && balance is null ? request.GramsOnHand : delta,
                    request.CostPerGram,
                    warehouseId,
                    cancellationToken);

                balance.GramsOnHand = GoldCurrencyHelper.Round(request.GramsOnHand, 4);
                if (request.CostPerGram is > 0)
                    balance.AverageCostPerGram = request.CostPerGram.Value;
            }

            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return balance
                ?? await context.GoldStockBalances.AsNoTracking()
                    .FirstOrDefaultAsync(
                        s => s.WarehouseId == warehouseId && s.KaratValue == request.KaratValue,
                        cancellationToken)
                ?? throw new InvalidOperationException(
                    $"تعذر حفظ رصيد الافتتاح للعيار {request.KaratValue} في المخزن #{warehouseId}");
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
        AppendOpeningNote(customer, request.Notes);

        await SyncCustomerOpeningVouchersAsync(context, customer.Id, customer.CreditBalanceIqd, customer.CreditBalanceUsd, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task ClearCustomerOpeningBalanceAsync(
        int customerId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (customerId <= 0)
            throw new InvalidOperationException("اختر الزبون");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await context.GoldCustomers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new InvalidOperationException("الزبون غير موجود");

        customer.CreditBalanceIqd = 0;
        customer.CreditBalanceUsd = 0;
        customer.GoldCreditGrams = 0;
        AppendOpeningNote(customer, string.IsNullOrWhiteSpace(notes) ? "تم تصفير الرصيد الافتتاحي" : notes);

        await RemoveOpeningVouchersAsync(context, customerId: customerId, supplierId: null, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<GoldSupplier> SetSupplierOpeningBalanceAsync(
        GoldOpeningSupplierBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SupplierId <= 0)
            throw new InvalidOperationException("اختر المورد");
        if (request.CreditBalanceIqd < 0 || request.CreditBalanceUsd < 0)
            throw new InvalidOperationException("أرصدة الافتتاح لا يمكن أن تكون سالبة");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var supplier = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken)
            ?? throw new InvalidOperationException("المورد غير موجود");

        supplier.CreditBalanceIqd = GoldCurrencyHelper.Round(request.CreditBalanceIqd);
        supplier.CreditBalanceUsd = GoldCurrencyHelper.Round(request.CreditBalanceUsd);
        AppendOpeningNote(supplier, request.Notes);

        await SyncSupplierOpeningVouchersAsync(context, supplier.Id, supplier.CreditBalanceIqd, supplier.CreditBalanceUsd, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task ClearSupplierOpeningBalanceAsync(
        int supplierId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (supplierId <= 0)
            throw new InvalidOperationException("اختر المورد");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var supplier = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            ?? throw new InvalidOperationException("المورد غير موجود");

        supplier.CreditBalanceIqd = 0;
        supplier.CreditBalanceUsd = 0;
        AppendOpeningNote(supplier, string.IsNullOrWhiteSpace(notes) ? "تم تصفير الرصيد الافتتاحي" : notes);

        await RemoveOpeningVouchersAsync(context, customerId: null, supplierId: supplierId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SyncCustomerOpeningVouchersAsync(
        GoldDbContext context,
        int customerId,
        decimal iqd,
        decimal usd,
        CancellationToken cancellationToken)
    {
        await UpsertOpeningVoucherAsync(
            context, customerId, null, GoldVoucherType.Receipt, GoldCurrency.IQD, iqd, cancellationToken);
        await UpsertOpeningVoucherAsync(
            context, customerId, null, GoldVoucherType.Receipt, GoldCurrency.USD, usd, cancellationToken);
    }

    private static async Task SyncSupplierOpeningVouchersAsync(
        GoldDbContext context,
        int supplierId,
        decimal iqd,
        decimal usd,
        CancellationToken cancellationToken)
    {
        await UpsertOpeningVoucherAsync(
            context, null, supplierId, GoldVoucherType.Payment, GoldCurrency.IQD, iqd, cancellationToken);
        await UpsertOpeningVoucherAsync(
            context, null, supplierId, GoldVoucherType.Payment, GoldCurrency.USD, usd, cancellationToken);
    }

    private static async Task UpsertOpeningVoucherAsync(
        GoldDbContext context,
        int? customerId,
        int? supplierId,
        GoldVoucherType voucherType,
        GoldCurrency currency,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var existing = await context.GoldVouchers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v =>
                v.IsOpeningBalance &&
                !v.IsDeleted &&
                v.CustomerId == customerId &&
                v.SupplierId == supplierId &&
                v.Currency == currency,
                cancellationToken);

        if (amount <= 0)
        {
            if (existing is not null && !existing.IsDeleted)
                existing.MarkSoftDeleted("System");
            return;
        }

        if (existing is { IsDeleted: true })
            existing.RestoreFromSoftDelete("System");

        if (existing is null)
        {
            var partyKey = customerId ?? supplierId ?? 0;
            var partyPrefix = customerId.HasValue ? "C" : "S";
            existing = new GoldVoucher
            {
                VoucherNumber = $"OB-{partyPrefix}{partyKey}-{currency}",
                VoucherDate = DateTime.Today,
                VoucherType = voucherType,
                Currency = currency,
                CustomerId = customerId,
                SupplierId = supplierId,
                IsOpeningBalance = true,
                AffectsCashBox = false,
                CashBoxId = null,
                Notes = "رصيد افتتاحي — سند توثيقي"
            };
            await context.GoldVouchers.AddAsync(existing, cancellationToken);
        }

        existing.Amount = GoldCurrencyHelper.Round(amount);
        existing.VoucherDate = DateTime.Today;
        existing.VoucherType = voucherType;
        existing.Notes = "رصيد افتتاحي — سند توثيقي";
    }

    private static async Task RemoveOpeningVouchersAsync(
        GoldDbContext context,
        int? customerId,
        int? supplierId,
        CancellationToken cancellationToken)
    {
        var vouchers = await context.GoldVouchers
            .IgnoreQueryFilters()
            .Where(v =>
                v.IsOpeningBalance &&
                !v.IsDeleted &&
                v.CustomerId == customerId &&
                v.SupplierId == supplierId)
            .ToListAsync(cancellationToken);

        foreach (var voucher in vouchers)
            voucher.MarkSoftDeleted("System");
    }

    private static void AppendOpeningNote(GoldCustomer customer, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return;

        var line = $"[افتتاح] {notes.Trim()}";
        customer.Notes = string.IsNullOrWhiteSpace(customer.Notes)
            ? line
            : $"{customer.Notes}\n{line}";
    }

    private static void AppendOpeningNote(GoldSupplier supplier, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return;

        var line = $"[افتتاح] {notes.Trim()}";
        supplier.Notes = string.IsNullOrWhiteSpace(supplier.Notes)
            ? line
            : $"{supplier.Notes}\n{line}";
    }
}
