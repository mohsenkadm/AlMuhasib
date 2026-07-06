using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Entities;
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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
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

        if (from.HasValue)
            query = query.Where(r => r.CheckInDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(r => r.CheckInDate <= to.Value.Date);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReservationStatus>(status, true, out var statusEnum))
            query = query.Where(r => r.Status == statusEnum);

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

    [HttpPost]
    public async Task<ActionResult<object>> CreateReservation([FromBody] CreateHotelReservationRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest("guestName is required.");

        int? roomId = null;
        string? roomNumber = null;
        if (request.RoomSyncId.HasValue)
        {
            var room = await Db.HotelRooms.FirstOrDefaultAsync(
                r => r.TenantId == TenantId && r.SyncId == request.RoomSyncId.Value, ct);
            if (room is null) return BadRequest("Room not found.");
            roomId = room.Id;
            roomNumber = room.RoomNumber;
        }

        var guest = await Db.HotelGuests.FirstOrDefaultAsync(
            g => g.TenantId == TenantId && g.FullName == request.GuestName.Trim(), ct);
        if (guest is null)
        {
            guest = new CloudHotelGuest
            {
                TenantId = TenantId,
                SyncId = Guid.NewGuid(),
                FullName = request.GuestName.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Mobile"
            };
            Db.HotelGuests.Add(guest);
            await Db.SaveChangesAsync(ct);
        }

        var number = $"R{DateTime.UtcNow:yyyyMMddHHmmss}";
        var entity = new CloudHotelReservation
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            ReservationNumber = number,
            GuestId = guest.Id,
            GuestName = guest.FullName,
            RoomId = roomId,
            RoomNumber = roomNumber,
            CheckInDate = request.CheckInDate.Date,
            CheckOutDate = request.CheckOutDate.Date,
            GuestCount = request.GuestCount,
            Status = ReservationStatus.Confirmed,
            TotalAmount = request.TotalAmount,
            AmountPaid = 0,
            RemainingAmount = request.TotalAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "Mobile"
        };
        Db.HotelReservations.Add(entity);
        await Db.SaveChangesAsync(ct);
        return Ok(new { syncId = entity.SyncId.ToString() });
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

public sealed class CreateHotelReservationRequest
{
    public string GuestName { get; set; } = string.Empty;
    public Guid? RoomSyncId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; } = 1;
    public decimal TotalAmount { get; set; }
}
