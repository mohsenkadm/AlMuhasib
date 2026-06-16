using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

[ApiController]
[Route("api/hotel")]
[Authorize(Policy = "Tenant")]
public sealed class HotelMobileController : HotelApiControllerBase
{
    public HotelMobileController(ITenantContext tenantContext, CloudDbContext db) : base(db, tenantContext) { }

    [HttpGet("dashboard")]
    public async Task<ActionResult<HotelDashboardDto>> GetDashboard(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var rooms = await Db.HotelRooms.AsNoTracking().Where(r => r.TenantId == TenantId).ToListAsync(ct);
        var total = rooms.Count;
        var occupied = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var available = rooms.Count(r => r.Status == RoomStatus.Available);
        var dirty = rooms.Count(r => r.Status == RoomStatus.Dirty);

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var arrivals = await Db.HotelReservations.AsNoTracking()
            .CountAsync(r => r.TenantId == TenantId && r.CheckInDate >= today && r.CheckInDate < tomorrow && r.Status != ReservationStatus.Cancelled, ct);
        var departures = await Db.HotelReservations.AsNoTracking()
            .CountAsync(r => r.TenantId == TenantId && r.CheckOutDate >= today && r.CheckOutDate < tomorrow && r.Status == ReservationStatus.CheckedIn, ct);

        var revenueToday = await Db.HotelReservationPayments.AsNoTracking()
            .Where(p => p.TenantId == TenantId && p.PaymentDate >= today && p.PaymentDate < tomorrow)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;

        return Ok(new HotelDashboardDto
        {
            TotalRooms = total,
            OccupiedRooms = occupied,
            AvailableRooms = available,
            DirtyRooms = dirty,
            OccupancyRate = total > 0 ? Math.Round((decimal)occupied / total * 100, 1) : 0,
            TodayArrivals = arrivals,
            TodayDepartures = departures,
            TodayRevenue = revenueToday
        });
    }

    [HttpGet("occupancy")]
    public async Task<ActionResult<HotelOccupancySummaryDto>> GetOccupancySummary(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var rooms = await Db.HotelRooms.AsNoTracking().Where(r => r.TenantId == TenantId).ToListAsync(ct);
        var total = rooms.Count;
        var occupied = rooms.Count(r => r.Status == RoomStatus.Occupied);

        return Ok(new HotelOccupancySummaryDto
        {
            TotalRooms = total,
            OccupiedRooms = occupied,
            AvailableRooms = rooms.Count(r => r.Status == RoomStatus.Available),
            OccupancyRate = total > 0 ? Math.Round((decimal)occupied / total * 100, 1) : 0
        });
    }

    [HttpGet("reservations/today")]
    public async Task<ActionResult<IReadOnlyList<HotelReservationMobileDto>>> GetTodayReservations(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var items = await Db.HotelReservations.AsNoTracking()
            .Where(r => r.TenantId == TenantId
                && r.CheckInDate >= today && r.CheckInDate < tomorrow
                && r.Status != ReservationStatus.Cancelled)
            .OrderBy(r => r.CheckInDate)
            .Select(r => new HotelReservationMobileDto
            {
                SyncId = r.SyncId,
                ReservationNumber = r.ReservationNumber,
                GuestName = r.GuestName,
                RoomNumber = r.RoomNumber,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Status = r.Status.ToString(),
                RemainingAmount = r.RemainingAmount
            })
            .ToListAsync(ct);

        return Ok(items);
    }
}

public sealed class HotelDashboardDto
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int DirtyRooms { get; set; }
    public decimal OccupancyRate { get; set; }
    public int TodayArrivals { get; set; }
    public int TodayDepartures { get; set; }
    public decimal TodayRevenue { get; set; }
}

public sealed class HotelOccupancySummaryDto
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public decimal OccupancyRate { get; set; }
}

public sealed class HotelReservationMobileDto
{
    public Guid SyncId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
}
