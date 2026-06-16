using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

[ApiController]
[Route("api/hotel/guests")]
[Authorize(Policy = "Tenant")]
public sealed class HotelGuestsController : HotelApiControllerBase
{
    public HotelGuestsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet]
    public async Task<ActionResult<PagedResult<HotelGuestListDto>>> GetGuests(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Db.HotelGuests.AsNoTracking().Where(g => g.TenantId == TenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(g => g.FullName.Contains(term) || g.Phone.Contains(term) || g.IdNumber.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(g => g.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(g => new HotelGuestListDto
            {
                SyncId = g.SyncId,
                FullName = g.FullName,
                Phone = g.Phone,
                Email = g.Email,
                IdNumber = g.IdNumber
            }).ToListAsync(ct);

        return Ok(new PagedResult<HotelGuestListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<HotelGuestDetailDto>> GetGuest(Guid syncId, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var guest = await Db.HotelGuests.AsNoTracking()
            .FirstOrDefaultAsync(g => g.TenantId == TenantId && g.SyncId == syncId, ct);
        if (guest is null) return NotFound();

        return Ok(new HotelGuestDetailDto
        {
            SyncId = guest.SyncId,
            FullName = guest.FullName,
            Phone = guest.Phone,
            Email = guest.Email,
            IdNumber = guest.IdNumber,
            Notes = guest.Notes
        });
    }

    [HttpPost]
    public async Task<ActionResult<HotelGuestDetailDto>> CreateGuest([FromBody] HotelGuestUpsertRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var entity = new CloudHotelGuest
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            FullName = request.FullName,
            Phone = request.Phone ?? string.Empty,
            Email = request.Email ?? string.Empty,
            IdNumber = request.IdNumber ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "mobile"
        };
        Db.HotelGuests.Add(entity);
        await Db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetGuest), new { syncId = entity.SyncId }, new HotelGuestDetailDto
        {
            SyncId = entity.SyncId,
            FullName = entity.FullName,
            Phone = entity.Phone,
            Email = entity.Email,
            IdNumber = entity.IdNumber,
            Notes = entity.Notes
        });
    }

    [HttpPut("{syncId:guid}")]
    public async Task<IActionResult> UpdateGuest(Guid syncId, [FromBody] HotelGuestUpsertRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var guest = await Db.HotelGuests.FirstOrDefaultAsync(g => g.TenantId == TenantId && g.SyncId == syncId, ct);
        if (guest is null) return NotFound();

        guest.FullName = request.FullName;
        guest.Phone = request.Phone ?? string.Empty;
        guest.Email = request.Email ?? string.Empty;
        guest.IdNumber = request.IdNumber ?? string.Empty;
        guest.Notes = request.Notes ?? string.Empty;
        guest.UpdatedAt = DateTime.UtcNow;
        guest.UpdatedBy = User.Identity?.Name ?? "mobile";
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public class HotelGuestListDto
{
    public Guid SyncId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
}

public sealed class HotelGuestDetailDto : HotelGuestListDto
{
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelGuestUpsertRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? IdNumber { get; set; }
    public string? Notes { get; set; }
}
