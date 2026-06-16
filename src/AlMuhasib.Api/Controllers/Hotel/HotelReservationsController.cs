using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

[ApiController]
[Route("api/hotel/reservations")]
[Authorize(Policy = "Tenant")]
public sealed class HotelReservationsController : HotelApiControllerBase
{
    public HotelReservationsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet]
    public async Task<ActionResult<PagedResult<HotelReservationListDto>>> GetReservations(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Db.HotelReservations.AsNoTracking().Where(r => r.TenantId == TenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.ReservationNumber.Contains(term) || r.GuestName.Contains(term) || (r.RoomNumber != null && r.RoomNumber.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.CheckInDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new HotelReservationListDto
            {
                SyncId = r.SyncId,
                ReservationNumber = r.ReservationNumber,
                GuestName = r.GuestName,
                RoomNumber = r.RoomNumber,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Status = r.Status.ToString(),
                TotalAmount = r.TotalAmount,
                RemainingAmount = r.RemainingAmount
            }).ToListAsync(ct);

        return Ok(new PagedResult<HotelReservationListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<HotelReservationDetailDto>> GetReservation(Guid syncId, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var r = await Db.HotelReservations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == TenantId && x.SyncId == syncId, ct);
        if (r is null) return NotFound();

        return Ok(new HotelReservationDetailDto
        {
            SyncId = r.SyncId,
            ReservationNumber = r.ReservationNumber,
            GuestName = r.GuestName,
            RoomNumber = r.RoomNumber,
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            ActualCheckIn = r.ActualCheckIn,
            ActualCheckOut = r.ActualCheckOut,
            GuestCount = r.GuestCount,
            Status = r.Status.ToString(),
            TotalAmount = r.TotalAmount,
            AmountPaid = r.AmountPaid,
            RemainingAmount = r.RemainingAmount,
            Notes = r.Notes
        });
    }
}

public class HotelReservationListDto
{
    public Guid SyncId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public sealed class HotelReservationDetailDto : HotelReservationListDto
{
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public int GuestCount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Notes { get; set; } = string.Empty;
}
