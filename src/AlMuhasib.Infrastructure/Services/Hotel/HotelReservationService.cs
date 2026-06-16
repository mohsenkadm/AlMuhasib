using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelReservationService : IReservationService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelReservationService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Reservation?> GetByIdAsync(
        int id,
        bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<Reservation> query = context.Reservations;

        if (includeDetails)
        {
            query = query
                .Include(r => r.Guest)
                .Include(r => r.Room!)
                    .ThenInclude(room => room.RoomType)
                .Include(r => r.Charges)
                .Include(r => r.Payments);
        }

        return await query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<ReservationListItem> Items, int TotalCount)> SearchPagedAsync(
        ReservationFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildFilterQuery(context, filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await HotelReservationAmountHelper.ProjectListItems(query
                .OrderByDescending(r => r.CheckInDate)
                .ThenByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        ValidateDates(reservation);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(reservation.ReservationNumber))
            reservation.ReservationNumber = await ReservationNumberHelper.GenerateNextAsync(context);

        if (reservation.RoomId.HasValue)
            await ValidateRoomAvailableAsync(context, reservation.RoomId.Value, cancellationToken);

        await HotelReservationAmountHelper.RecalculateTotalsAsync(context, reservation, cancellationToken);
        await context.Reservations.AddAsync(reservation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    public async Task<Reservation> UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        ValidateDates(reservation);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.Reservations
            .Include(r => r.Charges)
            .FirstOrDefaultAsync(r => r.Id == reservation.Id, cancellationToken)
            ?? throw new InvalidOperationException("الحجز غير موجود");

        if (existing.Status is ReservationStatus.Cancelled or ReservationStatus.CheckedOut)
            throw new InvalidOperationException("لا يمكن تعديل حجز ملغى أو تم تسجيل مغادرته");

        if (reservation.RoomId != existing.RoomId && reservation.RoomId.HasValue)
            await ValidateRoomAvailableAsync(context, reservation.RoomId.Value, cancellationToken);

        existing.GuestId = reservation.GuestId;
        existing.RoomId = reservation.RoomId;
        existing.CheckInDate = reservation.CheckInDate;
        existing.CheckOutDate = reservation.CheckOutDate;
        existing.GuestCount = reservation.GuestCount;
        existing.Notes = reservation.Notes;

        if (existing.Status == ReservationStatus.Confirmed)
        {
            existing.Charges.Clear();
            foreach (var charge in reservation.Charges)
            {
                existing.Charges.Add(new ReservationCharge
                {
                    Description = charge.Description,
                    Amount = charge.Amount,
                    ChargeDate = charge.ChargeDate,
                    Notes = charge.Notes
                });
            }
        }

        await HotelReservationAmountHelper.RecalculateTotalsAsync(context, existing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task CancelAsync(
        int id,
        string cancelledBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var reservation = await context.Reservations
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الحجز غير موجود");

        if (reservation.Status != ReservationStatus.Confirmed)
            throw new InvalidOperationException("يمكن إلغاء الحجوزات المؤكدة فقط");

        reservation.Status = ReservationStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            reservation.Notes = string.IsNullOrWhiteSpace(reservation.Notes)
                ? reason
                : $"{reservation.Notes}\n{reason}";

        if (reservation.RoomId.HasValue && reservation.Room is not null)
            reservation.Room.Status = RoomStatus.Available;

        reservation.UpdatedBy = cancelledBy;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الحجز غير موجود");

        reservation.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateReservationNumberAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await ReservationNumberHelper.GenerateNextAsync(context);
    }

    private static void ValidateDates(Reservation reservation)
    {
        if (reservation.CheckOutDate.Date <= reservation.CheckInDate.Date)
            throw new InvalidOperationException("تاريخ المغادرة يجب أن يكون بعد تاريخ الوصول");
    }

    private static async Task ValidateRoomAvailableAsync(
        HotelDbContext context,
        int roomId,
        CancellationToken cancellationToken)
    {
        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new InvalidOperationException("الغرفة غير موجودة");

        if (room.Status != RoomStatus.Available)
            throw new InvalidOperationException("الغرفة غير متاحة");
    }

    private static IQueryable<Reservation> BuildFilterQuery(HotelDbContext context, ReservationFilter filter)
    {
        var query = context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room!)
                .ThenInclude(room => room.RoomType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            var like = $"%{term}%";
            query = query.Where(r =>
                EF.Functions.Like(r.ReservationNumber, like) ||
                EF.Functions.Like(r.Guest.FullName, like) ||
                (r.Room != null && EF.Functions.Like(r.Room.RoomNumber, like)));
        }

        if (filter.CheckInFrom.HasValue)
            query = query.Where(r => r.CheckInDate >= filter.CheckInFrom.Value);
        if (filter.CheckInTo.HasValue)
            query = query.Where(r => r.CheckInDate <= filter.CheckInTo.Value);
        if (filter.CheckOutFrom.HasValue)
            query = query.Where(r => r.CheckOutDate >= filter.CheckOutFrom.Value);
        if (filter.CheckOutTo.HasValue)
            query = query.Where(r => r.CheckOutDate <= filter.CheckOutTo.Value);
        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);
        if (filter.RoomId.HasValue)
            query = query.Where(r => r.RoomId == filter.RoomId.Value);
        if (filter.GuestId.HasValue)
            query = query.Where(r => r.GuestId == filter.GuestId.Value);
        if (filter.UnpaidOnly)
            query = query.Where(r => r.RemainingAmount > 0);

        return query;
    }
}
