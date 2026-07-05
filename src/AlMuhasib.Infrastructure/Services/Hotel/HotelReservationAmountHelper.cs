using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

internal static class HotelReservationAmountHelper
{
    public static int GetNightCount(Reservation reservation) =>
        Math.Max(1, (reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days);

    public static async Task<decimal?> GetPriceForDateAsync(
        HotelDbContext context,
        int roomTypeId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var d = date.Date;

        var seasonPrice = await context.RatePlanSeasons
            .AsNoTracking()
            .Where(s => s.RatePlan.RoomTypeId == roomTypeId
                        && s.RatePlan.IsActive
                        && s.StartDate <= d
                        && s.EndDate >= d)
            .OrderByDescending(s => s.PricePerNight)
            .Select(s => (decimal?)s.PricePerNight)
            .FirstOrDefaultAsync(cancellationToken);

        if (seasonPrice.HasValue)
            return seasonPrice;

        var planPrice = await context.RatePlans
            .AsNoTracking()
            .Where(p => p.RoomTypeId == roomTypeId && p.IsActive)
            .OrderByDescending(p => p.BasePrice)
            .Select(p => (decimal?)p.BasePrice)
            .FirstOrDefaultAsync(cancellationToken);

        if (planPrice.HasValue)
            return planPrice;

        return await context.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.Id == roomTypeId)
            .Select(rt => (decimal?)rt.BasePrice)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task RecalculateTotalsAsync(
        HotelDbContext context,
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        int? roomTypeId = null;
        if (reservation.RoomId.HasValue)
        {
            roomTypeId = await context.Rooms
                .AsNoTracking()
                .Where(r => r.Id == reservation.RoomId.Value)
                .Select(r => (int?)r.RoomTypeId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        decimal roomTotal = 0;
        if (roomTypeId.HasValue)
        {
            for (var d = reservation.CheckInDate.Date; d < reservation.CheckOutDate.Date; d = d.AddDays(1))
            {
                var price = await GetPriceForDateAsync(context, roomTypeId.Value, d, cancellationToken);
                roomTotal += price ?? 0;
            }
        }

        var chargesTotal = reservation.Charges.Count > 0
            ? reservation.Charges.Sum(c => c.Amount)
            : reservation.Id > 0
                ? await context.ReservationCharges
                    .Where(c => c.ReservationId == reservation.Id)
                    .SumAsync(c => c.Amount, cancellationToken)
                : 0;

        reservation.TotalAmount = roomTotal + chargesTotal;
        reservation.RemainingAmount = Math.Max(0, reservation.TotalAmount - reservation.AmountPaid);
    }

    public static IQueryable<ReservationListItem> ProjectListItems(IQueryable<Reservation> query) =>
        query.Select(r => new ReservationListItem
        {
            Id = r.Id,
            GuestId = r.GuestId,
            RoomId = r.RoomId,
            ReservationNumber = r.ReservationNumber,
            GuestName = r.Guest.FullName,
            RoomNumber = r.Room != null ? r.Room.RoomNumber : null,
            RoomTypeName = r.Room != null ? r.Room.RoomType.Name : string.Empty,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            ActualCheckIn = r.ActualCheckIn,
            ActualCheckOut = r.ActualCheckOut,
            GuestCount = r.GuestCount,
            Status = r.Status,
            TotalAmount = r.TotalAmount,
            AmountPaid = r.AmountPaid,
            RemainingAmount = r.RemainingAmount,
            Notes = r.Notes
        });

    public static bool IsActiveStayOnDate(Reservation reservation, DateTime date)
    {
        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.NoShow)
            return false;
        if (!reservation.RoomId.HasValue)
            return false;

        var d = date.Date;
        return reservation.CheckInDate.Date <= d && reservation.CheckOutDate.Date > d;
    }
}
