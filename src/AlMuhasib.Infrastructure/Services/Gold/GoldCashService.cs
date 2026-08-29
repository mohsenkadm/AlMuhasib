using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldCashService : IGoldCashService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldCashService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<GoldCashBox>> GetCashBoxesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureDefaultCashBoxesAsync(context, cancellationToken);

        var query = context.GoldCashBoxes.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Currency)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<GoldCashBox?> GetCashBoxByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldCashBoxes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<GoldCashBox?> GetDefaultCashBoxAsync(GoldCurrency currency, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureDefaultCashBoxesAsync(context, cancellationToken);

        return await context.GoldCashBoxes.AsNoTracking()
            .Where(c => c.Currency == currency && c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GoldCashBox> CreateCashBoxAsync(GoldCashBox cashBox, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cashBox.Name))
            throw new InvalidOperationException("اسم الصندوق مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (cashBox.IsDefault)
        {
            var others = await context.GoldCashBoxes
                .Where(c => c.Currency == cashBox.Currency && c.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
                other.IsDefault = false;
        }

        await context.GoldCashBoxes.AddAsync(cashBox, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return cashBox;
    }

    public async Task<GoldCashBox> UpdateCashBoxAsync(GoldCashBox cashBox, CancellationToken cancellationToken = default)
    {
        if (cashBox.Balance < 0)
            throw new InvalidOperationException("رصيد القاصة لا يمكن أن يكون سالباً");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var existing = await context.GoldCashBoxes.FirstOrDefaultAsync(c => c.Id == cashBox.Id, cancellationToken)
            ?? throw new InvalidOperationException("الصندوق غير موجود");

        if (cashBox.IsDefault)
        {
            var others = await context.GoldCashBoxes
                .Where(c => c.Id != cashBox.Id && c.Currency == cashBox.Currency && c.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
                other.IsDefault = false;
        }

        var newBalance = GoldCurrencyHelper.Round(cashBox.Balance);
        var delta = GoldCurrencyHelper.Round(newBalance - existing.Balance);

        existing.Name = cashBox.Name;
        existing.Currency = cashBox.Currency;
        existing.IsDefault = cashBox.IsDefault;
        existing.IsActive = cashBox.IsActive;

        if (delta != 0)
        {
            // Keep cash-movement reports consistent: apply balance via adjustment voucher.
            var voucherType = delta > 0 ? GoldVoucherType.Receipt : GoldVoucherType.Payment;
            var amount = Math.Abs(delta);
            var voucher = new GoldVoucher
            {
                VoucherNumber = await GetNextVoucherNumberInternalAsync(context, voucherType, cancellationToken),
                VoucherDate = DateTime.Today,
                VoucherType = voucherType,
                Currency = existing.Currency,
                Amount = amount,
                CashBoxId = existing.Id,
                Notes = "تعديل رصيد القاصة"
            };
            await context.GoldVouchers.AddAsync(voucher, cancellationToken);
            AdjustCashBoxBalance(existing, delta);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteCashBoxAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var cashBox = await context.GoldCashBoxes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الصندوق غير موجود");

        if (cashBox.IsDefault)
            throw new InvalidOperationException("لا يمكن حذف الصندوق الافتراضي");

        cashBox.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<GoldVoucher> Items, int TotalCount)> GetVouchersPagedAsync(
        int page,
        int pageSize,
        GoldVoucherType? type = null,
        GoldCurrency? currency = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? cashBoxId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldVouchers.AsNoTracking()
            .Include(v => v.Customer)
            .Include(v => v.Supplier)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(v => v.VoucherType == type.Value);
        if (currency.HasValue)
            query = query.Where(v => v.Currency == currency.Value);
        if (dateFrom.HasValue)
            query = query.Where(v => v.VoucherDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(v => v.VoucherDate.Date <= dateTo.Value.Date);
        if (cashBoxId.HasValue)
            query = query.Where(v => v.CashBoxId == cashBoxId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<GoldVoucher?> GetVoucherByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldVouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<GoldVoucher> CreateVoucherAsync(GoldVoucher voucher, CancellationToken cancellationToken = default)
    {
        if (voucher.Amount <= 0)
            throw new InvalidOperationException("مبلغ السند يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureDefaultCashBoxesAsync(context, cancellationToken);

        var cashBox = await ResolveCashBoxAsync(context, voucher.CashBoxId, voucher.Currency, cancellationToken);
        voucher.CashBoxId = cashBox.Id;
        voucher.Currency = cashBox.Currency;

        if (string.IsNullOrWhiteSpace(voucher.VoucherNumber))
            voucher.VoucherNumber = await GetNextVoucherNumberInternalAsync(context, voucher.VoucherType, cancellationToken);

        if (voucher.AffectsCashBox)
        {
            AdjustCashBoxBalance(cashBox,
                voucher.VoucherType == GoldVoucherType.Receipt ? voucher.Amount : -voucher.Amount);
        }

        if (!voucher.IsOpeningBalance && voucher.CustomerId.HasValue)
        {
            var customer = await context.GoldCustomers.FirstOrDefaultAsync(c => c.Id == voucher.CustomerId.Value, cancellationToken)
                ?? throw new InvalidOperationException("الزبون غير موجود");

            var creditDelta = voucher.VoucherType == GoldVoucherType.Receipt
                ? -voucher.Amount
                : voucher.Amount;
            GoldCustomerService.AdjustCredit(customer, voucher.Currency, creditDelta);
        }

        if (!voucher.IsOpeningBalance && voucher.SupplierId.HasValue)
        {
            var supplier = await context.GoldSuppliers.FirstOrDefaultAsync(s => s.Id == voucher.SupplierId.Value, cancellationToken)
                ?? throw new InvalidOperationException("المورد غير موجود");

            var creditDelta = voucher.VoucherType == GoldVoucherType.Payment
                ? -voucher.Amount
                : voucher.Amount;
            GoldSupplierService.AdjustCredit(supplier, voucher.Currency, creditDelta);
        }

        await context.GoldVouchers.AddAsync(voucher, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return voucher;
    }

    public async Task<string> GetNextVoucherNumberAsync(GoldVoucherType type, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await GetNextVoucherNumberInternalAsync(context, type, cancellationToken);
    }

    internal static async Task EnsureDefaultCashBoxesAsync(GoldDbContext context, CancellationToken cancellationToken)
    {
        await GoldSettingsService.EnsureDefaultCashBoxesInternalAsync(context, cancellationToken);
        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync(cancellationToken);
    }

    internal static async Task<GoldCashBox> ResolveCashBoxAsync(
        GoldDbContext context,
        int? cashBoxId,
        GoldCurrency currency,
        CancellationToken cancellationToken)
    {
        if (cashBoxId.HasValue)
        {
            return await context.GoldCashBoxes.FirstOrDefaultAsync(c => c.Id == cashBoxId.Value, cancellationToken)
                ?? throw new InvalidOperationException("الصندوق غير موجود");
        }

        return await context.GoldCashBoxes
                .Where(c => c.Currency == currency && c.IsActive)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("لا يوجد صندوق نقدي للعملة المحددة");
    }

    internal static void AdjustCashBoxBalance(GoldCashBox cashBox, decimal delta)
    {
        cashBox.Balance = GoldCurrencyHelper.Round(cashBox.Balance + delta);
    }

    private static async Task<string> GetNextVoucherNumberInternalAsync(
        GoldDbContext context,
        GoldVoucherType type,
        CancellationToken cancellationToken)
    {
        var prefix = type == GoldVoucherType.Receipt ? "GRC" : "GPY";
        var last = await context.GoldVouchers
            .IgnoreQueryFilters()
            .Where(v => v.VoucherType == type && v.VoucherNumber.StartsWith(prefix + "-"))
            .OrderByDescending(v => v.Id)
            .Select(v => v.VoucherNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNum = 1;
        if (last is not null)
        {
            var parts = last.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{prefix}-{nextNum:D4}";
    }
}
