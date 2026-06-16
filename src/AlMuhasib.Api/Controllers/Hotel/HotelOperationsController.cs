using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

[ApiController]
[Route("api/hotel/operations")]
[Authorize(Policy = "Tenant")]
public sealed class HotelOperationsController : HotelApiControllerBase
{
    public HotelOperationsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] HotelCheckInRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var reservation = await Db.HotelReservations
            .FirstOrDefaultAsync(r => r.TenantId == TenantId && r.SyncId == request.ReservationSyncId, ct);
        if (reservation is null) return NotFound("Reservation not found");

        var room = await Db.HotelRooms
            .FirstOrDefaultAsync(r => r.TenantId == TenantId && r.SyncId == request.RoomSyncId, ct);
        if (room is null) return BadRequest("Room not found");
        if (room.Status != RoomStatus.Available)
            return BadRequest("Room is not available");

        reservation.RoomId = room.Id;
        reservation.RoomNumber = room.RoomNumber;
        reservation.Status = ReservationStatus.CheckedIn;
        reservation.ActualCheckIn = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;
        reservation.UpdatedBy = User.Identity?.Name ?? "mobile";

        room.Status = RoomStatus.Occupied;
        room.UpdatedAt = DateTime.UtcNow;
        room.UpdatedBy = User.Identity?.Name ?? "mobile";

        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] HotelCheckOutRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var reservation = await Db.HotelReservations
            .FirstOrDefaultAsync(r => r.TenantId == TenantId && r.SyncId == request.ReservationSyncId, ct);
        if (reservation is null) return NotFound("Reservation not found");

        reservation.Status = ReservationStatus.CheckedOut;
        reservation.ActualCheckOut = DateTime.UtcNow;
        reservation.UpdatedAt = DateTime.UtcNow;
        reservation.UpdatedBy = User.Identity?.Name ?? "mobile";

        if (reservation.RoomId.HasValue)
        {
            var room = await Db.HotelRooms.FirstOrDefaultAsync(r => r.Id == reservation.RoomId.Value, ct);
            if (room is not null)
            {
                room.Status = RoomStatus.Dirty;
                room.UpdatedAt = DateTime.UtcNow;
                room.UpdatedBy = User.Identity?.Name ?? "mobile";
            }
        }

        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("payment")]
    public async Task<IActionResult> RecordPayment([FromBody] HotelPaymentRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var reservation = await Db.HotelReservations
            .FirstOrDefaultAsync(r => r.TenantId == TenantId && r.SyncId == request.ReservationSyncId, ct);
        if (reservation is null) return NotFound("Reservation not found");

        int? cashBoxId = null;
        if (request.CashBoxSyncId.HasValue)
        {
            var cashBox = await Db.HotelCashBoxes
                .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == request.CashBoxSyncId.Value, ct);
            if (cashBox is null) return BadRequest("Cash box not found");
            cashBoxId = cashBox.Id;
            cashBox.CurrentBalance += request.Amount;
            cashBox.UpdatedAt = DateTime.UtcNow;
        }

        var payment = new CloudHotelReservationPayment
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            ReservationId = reservation.Id,
            PaymentDate = DateTime.UtcNow,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod ?? "نقد",
            Notes = request.Notes ?? string.Empty,
            HotelCashBoxId = cashBoxId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "mobile"
        };
        Db.HotelReservationPayments.Add(payment);

        reservation.AmountPaid += request.Amount;
        reservation.RemainingAmount = Math.Max(0, reservation.TotalAmount - reservation.AmountPaid);
        reservation.UpdatedAt = DateTime.UtcNow;

        await Db.SaveChangesAsync(ct);
        return Ok(new { payment.SyncId });
    }
}

public sealed class HotelCheckInRequest
{
    public Guid ReservationSyncId { get; set; }
    public Guid RoomSyncId { get; set; }
}

public sealed class HotelCheckOutRequest
{
    public Guid ReservationSyncId { get; set; }
}

public sealed class HotelPaymentRequest
{
    public Guid ReservationSyncId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public string? Notes { get; set; }
}
