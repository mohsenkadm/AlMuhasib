using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelReservationPaymentService : IReservationPaymentService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelReservationPaymentService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ReservationPayment> AddPaymentAsync(
        int reservationId,
        decimal amount,
        DateTime paymentDate,
        string paymentMethod,
        int? cashBoxId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ الدفع يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var reservation = await context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("الحجز غير موجود");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تسديد حجز ملغى");

        if (amount > reservation.RemainingAmount)
            throw new InvalidOperationException("مبلغ الدفع أكبر من المبلغ المتبقي");

        var payment = new ReservationPayment
        {
            ReservationId = reservationId,
            PaymentDate = paymentDate,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Notes = notes ?? string.Empty,
            HotelCashBoxId = cashBoxId
        };

        await context.ReservationPayments.AddAsync(payment, cancellationToken);

        reservation.AmountPaid += amount;
        reservation.RemainingAmount = Math.Max(0, reservation.TotalAmount - reservation.AmountPaid);

        if (cashBoxId.HasValue)
        {
            var cashBox = await context.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == cashBoxId.Value, cancellationToken)
                ?? throw new InvalidOperationException("الصندوق غير موجود");

            cashBox.CurrentBalance += amount;

            var voucherNumber = await GetNextVoucherNumberAsync(context, HotelVoucherType.Receipt, cancellationToken);
            await context.HotelVouchers.AddAsync(new HotelVoucher
            {
                VoucherNumber = voucherNumber,
                VoucherDate = paymentDate,
                Type = HotelVoucherType.Receipt,
                Amount = amount,
                HotelCashBoxId = cashBoxId.Value,
                ReservationId = reservationId,
                Description = $"دفعة حجز {reservation.ReservationNumber}",
                Notes = notes ?? string.Empty
            }, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<IReadOnlyList<ReservationPayment>> GetPaymentsAsync(
        int reservationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ReservationPayments
            .Where(p => p.ReservationId == reservationId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    private static async Task<string> GetNextVoucherNumberAsync(
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
