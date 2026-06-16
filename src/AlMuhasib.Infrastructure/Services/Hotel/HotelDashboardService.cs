using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelDashboardService : IHotelDashboardService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelDashboardService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<HotelDashboardStats> GetDashboardStatsAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var today = (asOfDate ?? DateTime.Today).Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var trendStart = today.AddDays(-6);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var rooms = await context.Rooms.AsNoTracking().ToListAsync(cancellationToken);
        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(r => r.Guest)
            .Include(r => r.Room!)
                .ThenInclude(room => room.RoomType)
            .ToListAsync(cancellationToken);

        var payments = await context.ReservationPayments.AsNoTracking().ToListAsync(cancellationToken);
        var pendingTasks = await context.HousekeepingTasks
            .AsNoTracking()
            .CountAsync(t => t.Status == HousekeepingStatus.Pending, cancellationToken);

        var totalRooms = rooms.Count;
        var availableRooms = rooms.Count(r => r.Status == RoomStatus.Available);
        var occupiedRooms = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var dirtyRooms = rooms.Count(r => r.Status == RoomStatus.Dirty);
        var maintenanceRooms = rooms.Count(r => r.Status is RoomStatus.Maintenance or RoomStatus.OutOfOrder);

        var inHouse = reservations.Count(r => r.Status == ReservationStatus.CheckedIn);
        var todayArrivals = reservations.Count(r =>
            r.CheckInDate.Date == today && r.Status == ReservationStatus.Confirmed);
        var todayDepartures = reservations.Count(r =>
            r.CheckOutDate.Date == today && r.Status == ReservationStatus.CheckedIn);

        var todayRevenue = payments.Where(p => p.PaymentDate.Date == today).Sum(p => p.Amount);
        var monthRevenue = payments.Where(p => p.PaymentDate.Date >= monthStart && p.PaymentDate.Date <= today).Sum(p => p.Amount);
        var outstanding = reservations
            .Where(r => r.Status != ReservationStatus.Cancelled && r.RemainingAmount > 0)
            .Sum(r => r.RemainingAmount);

        var occupancyRate = totalRooms > 0 ? Math.Round((decimal)occupiedRooms / totalRooms * 100, 2) : 0;

        var roomStatusChart = Enum.GetValues<RoomStatus>()
            .Select(status => new HotelNameCountPoint
            {
                Name = status.ToString(),
                Count = rooms.Count(r => r.Status == status)
            })
            .Where(p => p.Count > 0)
            .ToList();

        var revenueTrend = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = trendStart.AddDays(offset);
                return new DailyAmountPoint
                {
                    Date = date,
                    Amount = payments.Where(p => p.PaymentDate.Date == date).Sum(p => p.Amount)
                };
            })
            .ToList();

        var revenueByRoomType = reservations
            .Where(r => r.Room?.RoomType is not null && r.Status != ReservationStatus.Cancelled)
            .GroupBy(r => r.Room!.RoomType.Name)
            .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(r => r.TotalAmount) })
            .OrderByDescending(p => p.Amount)
            .ToList();

        var todayArrivalList = await HotelReservationAmountHelper.ProjectListItems(
                context.Reservations.Where(r =>
                    r.CheckInDate.Date == today &&
                    (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn)))
            .ToListAsync(cancellationToken);

        var todayDepartureList = await HotelReservationAmountHelper.ProjectListItems(
                context.Reservations.Where(r =>
                    r.CheckOutDate.Date == today &&
                    (r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.Confirmed)))
            .ToListAsync(cancellationToken);

        var recentReservations = await HotelReservationAmountHelper.ProjectListItems(
                context.Reservations.OrderByDescending(r => r.CreatedAt).Take(10))
            .ToListAsync(cancellationToken);

        return new HotelDashboardStats
        {
            TotalRooms = totalRooms,
            AvailableRooms = availableRooms,
            OccupiedRooms = occupiedRooms,
            DirtyRooms = dirtyRooms,
            MaintenanceRooms = maintenanceRooms,
            OccupancyRate = occupancyRate,
            TodayArrivals = todayArrivals,
            TodayDepartures = todayDepartures,
            InHouseGuests = inHouse,
            PendingHousekeepingTasks = pendingTasks,
            TodayRevenue = todayRevenue,
            MonthRevenue = monthRevenue,
            OutstandingBalances = outstanding,
            RoomStatusChart = roomStatusChart,
            RevenueTrend = revenueTrend,
            RevenueByRoomType = revenueByRoomType,
            TodayArrivalList = todayArrivalList,
            TodayDepartureList = todayDepartureList,
            RecentReservations = recentReservations
        };
    }
}
