using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelCheckInOutService : ICheckInOutService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelCheckInOutService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Reservation> CheckInAsync(
        int reservationId,
        int? roomId = null,
        DateTime? checkInTime = null,
        string? checkedInBy = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var reservation = await context.Reservations
            .Include(r => r.Room)
            .Include(r => r.Charges)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("الحجز غير موجود");

        if (reservation.Status != ReservationStatus.Confirmed)
            throw new InvalidOperationException("يمكن تسجيل الوصول للحجوزات المؤكدة فقط");

        var targetRoomId = roomId ?? reservation.RoomId;
        if (!targetRoomId.HasValue)
            throw new InvalidOperationException("يجب تحديد غرفة لتسجيل الوصول");

        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == targetRoomId.Value, cancellationToken)
            ?? throw new InvalidOperationException("الغرفة غير موجودة");

        if (room.Status != RoomStatus.Available)
            throw new InvalidOperationException("الغرفة غير متاحة");

        reservation.RoomId = targetRoomId;
        reservation.Status = ReservationStatus.CheckedIn;
        reservation.ActualCheckIn = checkInTime ?? DateTime.Now;
        if (!string.IsNullOrWhiteSpace(checkedInBy))
            reservation.UpdatedBy = checkedInBy;

        room.Status = RoomStatus.Occupied;

        await HotelReservationAmountHelper.RecalculateTotalsAsync(context, reservation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    public async Task<Reservation> CheckOutAsync(
        int reservationId,
        DateTime? checkOutTime = null,
        string? checkedOutBy = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var reservation = await context.Reservations
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("الحجز غير موجود");

        if (reservation.Status != ReservationStatus.CheckedIn)
            throw new InvalidOperationException("يمكن تسجيل المغادرة للنزلاء المسجلين فقط");

        reservation.Status = ReservationStatus.CheckedOut;
        reservation.ActualCheckOut = checkOutTime ?? DateTime.Now;
        if (!string.IsNullOrWhiteSpace(checkedOutBy))
            reservation.UpdatedBy = checkedOutBy;

        if (reservation.Room is not null)
        {
            reservation.Room.Status = RoomStatus.Dirty;

            await context.HousekeepingTasks.AddAsync(new HousekeepingTask
            {
                RoomId = reservation.Room.Id,
                Status = HousekeepingStatus.Pending,
                Notes = $"بعد مغادرة الحجز {reservation.ReservationNumber}"
            }, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    public async Task<IReadOnlyList<ReservationListItem>> GetTodayArrivalsAsync(
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await HotelReservationAmountHelper.ProjectListItems(
                context.Reservations
                    .Where(r => r.CheckInDate.Date == targetDate
                                && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn)))
            .OrderBy(r => r.CheckInDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationListItem>> GetTodayDeparturesAsync(
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await HotelReservationAmountHelper.ProjectListItems(
                context.Reservations
                    .Where(r => r.CheckOutDate.Date == targetDate
                                && (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.Confirmed)))
            .OrderBy(r => r.CheckOutDate)
            .ToListAsync(cancellationToken);
    }
}
