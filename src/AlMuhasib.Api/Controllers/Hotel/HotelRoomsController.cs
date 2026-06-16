using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

[ApiController]
[Route("api/hotel/rooms")]
[Authorize(Policy = "Tenant")]
public sealed class HotelRoomsController : HotelApiControllerBase
{
    public HotelRoomsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HotelRoomListDto>>> GetRooms([FromQuery] string? status, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var query = Db.HotelRooms.AsNoTracking()
            .Include(r => r.Floor)
            .Include(r => r.RoomType)
            .Where(r => r.TenantId == TenantId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RoomStatus>(status, true, out var roomStatus))
            query = query.Where(r => r.Status == roomStatus);

        var items = await query.OrderBy(r => r.RoomNumber).Select(r => new HotelRoomListDto
        {
            SyncId = r.SyncId,
            RoomNumber = r.RoomNumber,
            FloorName = r.Floor.Name,
            RoomTypeName = r.RoomType.Name,
            Status = r.Status.ToString(),
            Notes = r.Notes
        }).ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<HotelRoomDetailDto>> GetRoom(Guid syncId, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var room = await Db.HotelRooms.AsNoTracking()
            .Include(r => r.Floor)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.TenantId == TenantId && r.SyncId == syncId, ct);

        if (room is null) return NotFound();

        return Ok(new HotelRoomDetailDto
        {
            SyncId = room.SyncId,
            RoomNumber = room.RoomNumber,
            FloorName = room.Floor.Name,
            RoomTypeName = room.RoomType.Name,
            Status = room.Status.ToString(),
            Notes = room.Notes
        });
    }
}

public class HotelRoomListDto
{
    public Guid SyncId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string FloorName { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelRoomDetailDto : HotelRoomListDto;
