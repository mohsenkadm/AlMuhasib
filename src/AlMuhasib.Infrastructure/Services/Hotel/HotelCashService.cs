using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelCashService : IHotelCashService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelCashService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<HotelCashBox>> GetCashBoxesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.HotelCashBoxes.AsQueryable();
        if (activeOnly)
            query = query.Where(c => c.IsActive);

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<HotelCashBox?> GetCashBoxByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<HotelCashBox> CreateCashBoxAsync(HotelCashBox cashBox, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        cashBox.CurrentBalance = cashBox.OpeningBalance;
        await context.HotelCashBoxes.AddAsync(cashBox, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return cashBox;
    }

    public async Task<HotelCashBox> UpdateCashBoxAsync(HotelCashBox cashBox, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == cashBox.Id, cancellationToken)
            ?? throw new InvalidOperationException("الصندوق غير موجود");

        existing.Name = cashBox.Name;
        existing.IsBank = cashBox.IsBank;
        existing.IsActive = cashBox.IsActive;
        existing.Notes = cashBox.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteCashBoxAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var cashBox = await context.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الصندوق غير موجود");

        cashBox.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<HotelVoucher> Items, int TotalCount)> GetVouchersPagedAsync(
        int page,
        int pageSize,
        HotelVoucherFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.HotelVouchers
            .Include(v => v.HotelCashBox)
            .AsQueryable();

        if (filter is not null)
        {
            if (filter.Type.HasValue)
                query = query.Where(v => v.Type == filter.Type.Value);
            if (filter.DateFrom.HasValue)
                query = query.Where(v => v.VoucherDate >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(v => v.VoucherDate <= filter.DateTo.Value);
            if (filter.CashBoxId.HasValue)
                query = query.Where(v => v.HotelCashBoxId == filter.CashBoxId.Value);
            if (filter.ReservationId.HasValue)
                query = query.Where(v => v.ReservationId == filter.ReservationId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<HotelVoucher?> GetVoucherByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotelVouchers
            .Include(v => v.HotelCashBox)
            .Include(v => v.Reservation)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<HotelVoucher> CreateVoucherAsync(HotelVoucher voucher, CancellationToken cancellationToken = default)
    {
        if (voucher.Amount <= 0)
            throw new InvalidOperationException("مبلغ السند يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var cashBox = await context.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == voucher.HotelCashBoxId, cancellationToken)
            ?? throw new InvalidOperationException("الصندوق غير موجود");

        if (string.IsNullOrWhiteSpace(voucher.VoucherNumber))
            voucher.VoucherNumber = await GetNextVoucherNumberInternalAsync(context, voucher.Type, cancellationToken);

        switch (voucher.Type)
        {
            case HotelVoucherType.Receipt:
                cashBox.CurrentBalance += voucher.Amount;
                break;
            case HotelVoucherType.Payment:
                if (cashBox.CurrentBalance < voucher.Amount)
                    throw new InvalidOperationException("رصيد الصندوق غير كافٍ");
                cashBox.CurrentBalance -= voucher.Amount;
                break;
        }

        await context.HotelVouchers.AddAsync(voucher, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return voucher;
    }

    public async Task<string> GetNextVoucherNumberAsync(
        HotelVoucherType type,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await GetNextVoucherNumberInternalAsync(context, type, cancellationToken);
    }

    private static async Task<string> GetNextVoucherNumberInternalAsync(
        HotelDbContext context,
        HotelVoucherType type,
        CancellationToken cancellationToken)
    {
        var prefix = type == HotelVoucherType.Receipt ? "HRC" : "HPY";
        var lastVoucher = await context.HotelVouchers
            .IgnoreQueryFilters()
            .Where(v => v.Type == type && v.VoucherNumber.StartsWith(prefix + "-"))
            .OrderByDescending(v => v.Id)
            .Select(v => v.VoucherNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNum = 1;
        if (lastVoucher is not null)
        {
            var parts = lastVoucher.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{prefix}-{nextNum:D4}";
    }
}
