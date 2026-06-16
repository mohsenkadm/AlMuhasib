using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelReportService : IHotelReportService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelReportService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<OccupancyReportData> GetOccupancyReportAsync(
        HotelReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var dateFrom = (filter.DateFrom ?? DateTime.Today.AddDays(-30)).Date;
        var dateTo = (filter.DateTo ?? DateTime.Today).Date;
        if (dateTo < dateFrom)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var roomsQuery = context.Rooms.AsNoTracking().AsQueryable();
        if (filter.FloorId.HasValue)
            roomsQuery = roomsQuery.Where(r => r.FloorId == filter.FloorId.Value);
        if (filter.RoomTypeId.HasValue)
            roomsQuery = roomsQuery.Where(r => r.RoomTypeId == filter.RoomTypeId.Value);

        var totalRooms = await roomsQuery.CountAsync(cancellationToken);
        var roomIds = await roomsQuery.Select(r => r.Id).ToListAsync(cancellationToken);

        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(r => r.Room!)
                .ThenInclude(room => room.RoomType)
            .Where(r => r.RoomId != null
                        && roomIds.Contains(r.RoomId.Value)
                        && r.Status != ReservationStatus.Cancelled
                        && r.Status != ReservationStatus.NoShow
                        && r.CheckInDate.Date <= dateTo
                        && r.CheckOutDate.Date > dateFrom)
            .ToListAsync(cancellationToken);

        if (filter.Status.HasValue)
            reservations = reservations.Where(r => r.Status == filter.Status.Value).ToList();

        var rows = new List<OccupancyReportRow>();
        var dailyChart = new List<DailyAmountPoint>();
        var soldRoomNights = 0;

        for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
        {
            var occupied = reservations.Count(r => HotelReservationAmountHelper.IsActiveStayOnDate(r, date));
            var available = Math.Max(0, totalRooms - occupied);
            var rate = totalRooms > 0 ? Math.Round((decimal)occupied / totalRooms * 100, 2) : 0;

            var arrivals = reservations.Count(r => r.CheckInDate.Date == date);
            var departures = reservations.Count(r => r.CheckOutDate.Date == date);

            soldRoomNights += occupied;
            rows.Add(new OccupancyReportRow
            {
                Date = date,
                TotalRooms = totalRooms,
                OccupiedRooms = occupied,
                AvailableRooms = available,
                OccupancyRate = rate,
                Arrivals = arrivals,
                Departures = departures
            });

            dailyChart.Add(new DailyAmountPoint { Date = date, Amount = rate });
        }

        var dayCount = Math.Max(1, (dateTo - dateFrom).Days + 1);
        var totalRoomNights = totalRooms * dayCount;
        var averageRate = totalRoomNights > 0
            ? Math.Round((decimal)soldRoomNights / totalRoomNights * 100, 2)
            : 0;

        var byRoomType = reservations
            .Where(r => r.Room?.RoomType is not null)
            .GroupBy(r => r.Room!.RoomType.Name)
            .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Count() })
            .OrderByDescending(p => p.Amount)
            .ToList();

        return new OccupancyReportData
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalRoomNights = totalRoomNights,
            SoldRoomNights = soldRoomNights,
            AverageOccupancyRate = averageRate,
            Rows = rows,
            DailyOccupancyChart = dailyChart,
            ByRoomTypeChart = byRoomType
        };
    }

    public async Task<RevenueReportData> GetRevenueReportAsync(
        HotelReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var dateFrom = (filter.DateFrom ?? DateTime.Today.AddDays(-30)).Date;
        var dateTo = (filter.DateTo ?? DateTime.Today).Date;
        if (dateTo < dateFrom)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var reservationsQuery = context.Reservations
            .AsNoTracking()
            .Include(r => r.Guest)
            .Include(r => r.Room!)
                .ThenInclude(room => room.RoomType)
            .Include(r => r.Charges)
            .Where(r => r.Status != ReservationStatus.Cancelled
                        && r.CheckInDate.Date <= dateTo
                        && r.CheckOutDate.Date >= dateFrom);

        if (filter.RoomTypeId.HasValue)
            reservationsQuery = reservationsQuery.Where(r => r.Room != null && r.Room.RoomTypeId == filter.RoomTypeId.Value);
        if (filter.FloorId.HasValue)
            reservationsQuery = reservationsQuery.Where(r => r.Room != null && r.Room.FloorId == filter.FloorId.Value);
        if (filter.Status.HasValue)
            reservationsQuery = reservationsQuery.Where(r => r.Status == filter.Status.Value);

        var reservations = await reservationsQuery.ToListAsync(cancellationToken);

        var payments = await context.ReservationPayments
            .AsNoTracking()
            .Where(p => p.PaymentDate.Date >= dateFrom && p.PaymentDate.Date <= dateTo)
            .ToListAsync(cancellationToken);

        var charges = await context.ReservationCharges
            .AsNoTracking()
            .Where(c => c.ChargeDate.Date >= dateFrom && c.ChargeDate.Date <= dateTo)
            .ToListAsync(cancellationToken);

        var rows = reservations
            .Select(r =>
            {
                var nights = HotelReservationAmountHelper.GetNightCount(r);
                var extraCharges = r.Charges.Sum(c => c.Amount);
                var roomRevenue = r.TotalAmount - extraCharges;
                return new RevenueReportRow
                {
                    Date = r.CheckInDate,
                    ReservationNumber = r.ReservationNumber,
                    GuestName = r.Guest.FullName,
                    RoomNumber = r.Room?.RoomNumber ?? string.Empty,
                    RoomTypeName = r.Room?.RoomType.Name ?? string.Empty,
                    Nights = nights,
                    RoomRevenue = roomRevenue,
                    ExtraCharges = extraCharges,
                    TotalAmount = r.TotalAmount,
                    AmountPaid = r.AmountPaid,
                    RemainingAmount = r.RemainingAmount
                };
            })
            .OrderBy(r => r.Date)
            .ToList();

        var dailyRevenue = payments
            .GroupBy(p => p.PaymentDate.Date)
            .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(p => p.Amount) })
            .OrderBy(p => p.Date)
            .ToList();

        var byRoomType = rows
            .Where(r => !string.IsNullOrEmpty(r.RoomTypeName))
            .GroupBy(r => r.RoomTypeName)
            .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(r => r.TotalAmount) })
            .OrderByDescending(p => p.Amount)
            .ToList();

        var byPaymentMethod = payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(p => p.Amount) })
            .OrderByDescending(p => p.Amount)
            .ToList();

        return new RevenueReportData
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalRoomRevenue = rows.Sum(r => r.RoomRevenue),
            TotalPayments = payments.Sum(p => p.Amount),
            TotalCharges = charges.Sum(c => c.Amount),
            OutstandingBalance = reservations.Sum(r => r.RemainingAmount),
            Rows = rows,
            DailyRevenueChart = dailyRevenue,
            ByRoomTypeChart = byRoomType,
            ByPaymentMethodChart = byPaymentMethod
        };
    }

    public async Task<NightAuditReportData> GetNightAuditReportAsync(
        DateTime auditDate,
        CancellationToken cancellationToken = default)
    {
        var date = auditDate.Date;
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var rooms = await context.Rooms.AsNoTracking().ToListAsync(cancellationToken);
        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .ToListAsync(cancellationToken);

        var payments = await context.ReservationPayments
            .AsNoTracking()
            .Where(p => p.PaymentDate.Date == date)
            .ToListAsync(cancellationToken);

        var expenses = await context.HotelExpenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate.Date == date)
            .ToListAsync(cancellationToken);

        var cashBoxes = await context.HotelCashBoxes
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        var vouchers = await context.HotelVouchers
            .AsNoTracking()
            .Where(v => v.VoucherDate.Date == date)
            .ToListAsync(cancellationToken);

        var totalRooms = rooms.Count;
        var occupiedRooms = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var availableRooms = rooms.Count(r => r.Status == RoomStatus.Available);

        var inHouse = reservations
            .Where(r => r.Status == ReservationStatus.CheckedIn
                        && r.CheckInDate.Date <= date
                        && r.CheckOutDate.Date > date)
            .Select(MapNightAuditRow)
            .ToList();

        var expectedArrivals = reservations
            .Where(r => r.CheckInDate.Date == date && r.Status == ReservationStatus.Confirmed)
            .Select(MapNightAuditRow)
            .ToList();

        var expectedDepartures = reservations
            .Where(r => r.CheckOutDate.Date == date && r.Status == ReservationStatus.CheckedIn)
            .Select(MapNightAuditRow)
            .ToList();

        var noShows = reservations.Count(r =>
            r.CheckInDate.Date == date && r.Status == ReservationStatus.NoShow);

        var walkIns = reservations.Count(r =>
            r.Status == ReservationStatus.CheckedIn
            && r.CheckInDate.Date == date
            && r.CreatedAt.Date == date);

        var roomRevenue = reservations
            .Where(r => r.CheckOutDate.Date == date && r.Status == ReservationStatus.CheckedOut)
            .Sum(r => r.TotalAmount);

        var cashBoxRows = cashBoxes.Select(cb =>
        {
            var receipts = vouchers
                .Where(v => v.HotelCashBoxId == cb.Id && v.Type == HotelVoucherType.Receipt)
                .Sum(v => v.Amount);
            var paymentsOut = vouchers
                .Where(v => v.HotelCashBoxId == cb.Id && v.Type == HotelVoucherType.Payment)
                .Sum(v => v.Amount);

            return new NightAuditCashBoxRow
            {
                CashBoxId = cb.Id,
                CashBoxName = cb.Name,
                OpeningBalance = cb.CurrentBalance - receipts + paymentsOut,
                Receipts = receipts,
                Payments = paymentsOut,
                ClosingBalance = cb.CurrentBalance
            };
        }).ToList();

        return new NightAuditReportData
        {
            AuditDate = date,
            TotalRooms = totalRooms,
            OccupiedRooms = occupiedRooms,
            AvailableRooms = availableRooms,
            ArrivalsToday = expectedArrivals.Count,
            DeparturesToday = expectedDepartures.Count,
            NoShows = noShows,
            WalkIns = walkIns,
            RoomRevenue = roomRevenue,
            PaymentsCollected = payments.Sum(p => p.Amount),
            ExpensesPosted = expenses.Sum(e => e.Amount),
            CashOnHand = cashBoxes.Sum(c => c.CurrentBalance),
            InHouseGuests = inHouse,
            ExpectedArrivals = expectedArrivals,
            ExpectedDepartures = expectedDepartures,
            CashBoxBalances = cashBoxRows
        };
    }

    private static NightAuditReservationRow MapNightAuditRow(Core.Entities.Hotel.Reservation r) =>
        new()
        {
            ReservationId = r.Id,
            ReservationNumber = r.ReservationNumber,
            GuestName = r.Guest.FullName,
            RoomNumber = r.Room?.RoomNumber ?? string.Empty,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Status = r.Status,
            TotalAmount = r.TotalAmount,
            RemainingAmount = r.RemainingAmount
        };
}
