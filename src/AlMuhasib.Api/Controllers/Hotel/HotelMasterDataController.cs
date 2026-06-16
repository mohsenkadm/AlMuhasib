using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

[ApiController]
[Route("api/hotel/master-data")]
[Authorize(Policy = "Tenant")]
public sealed class HotelMasterDataController : HotelApiControllerBase
{
    public HotelMasterDataController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("floors")]
    public async Task<ActionResult<IReadOnlyList<HotelFloorDto>>> GetFloors(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var items = await Db.HotelFloors.AsNoTracking()
            .Where(f => f.TenantId == TenantId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new HotelFloorDto { SyncId = f.SyncId, Name = f.Name, SortOrder = f.SortOrder })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("room-types")]
    public async Task<ActionResult<IReadOnlyList<HotelRoomTypeDto>>> GetRoomTypes(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var items = await Db.HotelRoomTypes.AsNoTracking()
            .Where(t => t.TenantId == TenantId)
            .OrderBy(t => t.SortOrder)
            .Select(t => new HotelRoomTypeDto
            {
                SyncId = t.SyncId,
                Name = t.Name,
                Description = t.Description,
                Capacity = t.Capacity,
                BasePrice = t.BasePrice
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("rate-plans")]
    public async Task<ActionResult<IReadOnlyList<HotelRatePlanDto>>> GetRatePlans(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var roomTypes = await Db.HotelRoomTypes.AsNoTracking()
            .Where(t => t.TenantId == TenantId)
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var items = await Db.HotelRatePlans.AsNoTracking()
            .Where(p => p.TenantId == TenantId && p.IsActive)
            .Select(p => new HotelRatePlanDto
            {
                SyncId = p.SyncId,
                Name = p.Name,
                RoomTypeId = p.RoomTypeId,
                BasePrice = p.BasePrice
            })
            .ToListAsync(ct);

        foreach (var item in items)
            item.RoomTypeName = roomTypes.GetValueOrDefault(item.RoomTypeId, string.Empty);

        return Ok(items);
    }
}

public sealed class HotelFloorDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class HotelRoomTypeDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
}

public sealed class HotelRatePlanDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}
