using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Sync.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/tenant-accounts")]
[Authorize(Policy = "Tenant")]
public sealed class TenantAccountBranchesController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TenantAccountBranchesController(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>كل فروع المستأجر النشطة (لتعيين الحسابات — مالك فقط).</summary>
    [HttpGet("all-branches")]
    public async Task<ActionResult<List<BranchInfoDto>>> ListAllBranches(CancellationToken ct)
    {
        try
        {
            var tenantId = RequireTenant();
            await EnsureOwnerOrManageAllAsync(tenantId, ct);

            _db.BypassBranchFilter = true;
            try
            {
                var items = await _db.Branches.AsNoTracking()
                    .Where(b => b.TenantId == tenantId && !b.IsDeleted && b.IsActive)
                    .OrderByDescending(b => b.IsMain)
                    .ThenBy(b => b.Name)
                    .Select(b => new BranchInfoDto
                    {
                        BranchId = b.Id,
                        SyncId = b.SyncId,
                        Name = b.Name,
                        Code = b.Code,
                        IsMain = b.IsMain,
                        IsDefault = false
                    })
                    .ToListAsync(ct);
                return Ok(items);
            }
            finally
            {
                _db.BypassBranchFilter = false;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse { Code = "FORBIDDEN", Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantAccountListItem>>> ListAccounts(CancellationToken ct)
    {
        try
        {
            var tenantId = RequireTenant();
            await EnsureOwnerOrManageAllAsync(tenantId, ct);

            var items = await _db.TenantAccounts.AsNoTracking()
                .Where(a => a.TenantId == tenantId)
                .OrderBy(a => a.CreatedAt).ThenBy(a => a.Id)
                .Select(a => new TenantAccountListItem
                {
                    Id = a.Id,
                    Username = a.Username,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    IsOwner = false
                })
                .ToListAsync(ct);

            if (items.Count > 0)
            {
                var ownerId = items.Where(a => a.IsActive).OrderBy(a => a.CreatedAt).ThenBy(a => a.Id).FirstOrDefault()?.Id
                              ?? items[0].Id;
                foreach (var item in items)
                    item.IsOwner = item.Id == ownerId;
            }

            return Ok(items);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse { Code = "FORBIDDEN", Message = ex.Message });
        }
    }

    [HttpGet("{accountId:int}/branches")]
    public async Task<ActionResult<TenantAccountBranchesResponse>> GetBranches(int accountId, CancellationToken ct)
    {
        int tenantId;
        try
        {
            tenantId = RequireTenant();
            await EnsureOwnerOrManageAllAsync(tenantId, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse { Code = "FORBIDDEN", Message = ex.Message });
        }

        var account = await _db.TenantAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId && a.TenantId == tenantId, ct);
        if (account is null)
            return NotFound(new ApiErrorResponse { Code = "ACCOUNT_NOT_FOUND", Message = "الحساب غير موجود" });

        _db.BypassBranchFilter = true;
        try
        {
            var assigned = await _db.TenantAccountBranches.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TenantAccountId == accountId)
                .Join(_db.Branches.Where(b => b.TenantId == tenantId && !b.IsDeleted),
                    x => x.BranchId, b => b.Id,
                    (x, b) => new BranchInfoDto
                    {
                        BranchId = b.Id,
                        SyncId = b.SyncId,
                        Name = b.Name,
                        Code = b.Code,
                        IsMain = b.IsMain,
                        IsDefault = x.IsDefault
                    })
                .OrderByDescending(b => b.IsMain)
                .ThenBy(b => b.Name)
                .ToListAsync(ct);

            return Ok(new TenantAccountBranchesResponse
            {
                AccountId = accountId,
                Username = account.Username,
                Branches = assigned,
                DefaultBranchId = assigned.FirstOrDefault(b => b.IsDefault)?.BranchId
                                  ?? (assigned.Count == 1 ? assigned[0].BranchId : null)
            });
        }
        finally
        {
            _db.BypassBranchFilter = false;
        }
    }

    [HttpPut("{accountId:int}/branches")]
    public async Task<ActionResult<TenantAccountBranchesResponse>> SetBranches(
        int accountId, [FromBody] SetTenantAccountBranchesRequest request, CancellationToken ct)
    {
        int tenantId;
        try
        {
            tenantId = RequireTenant();
            await EnsureOwnerOrManageAllAsync(tenantId, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse { Code = "FORBIDDEN", Message = ex.Message });
        }

        var account = await _db.TenantAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.TenantId == tenantId, ct);
        if (account is null)
            return NotFound(new ApiErrorResponse { Code = "ACCOUNT_NOT_FOUND", Message = "الحساب غير موجود" });

        var branchIds = (request.BranchIds ?? []).Where(id => id > 0).Distinct().ToList();
        if (branchIds.Count == 0)
            return BadRequest(new ApiErrorResponse { Code = "BRANCHES_REQUIRED", Message = "يجب تعيين فرع واحد على الأقل" });

        _db.BypassBranchFilter = true;
        try
        {
            var validBranches = await _db.Branches
                .Where(b => b.TenantId == tenantId && !b.IsDeleted && b.IsActive && branchIds.Contains(b.Id))
                .Select(b => b.Id)
                .ToListAsync(ct);
            if (validBranches.Count != branchIds.Count)
                return BadRequest(new ApiErrorResponse { Code = "INVALID_BRANCH", Message = "أحد الفروع غير صالح أو غير نشط" });

            var defaultId = request.DefaultBranchId is int d && validBranches.Contains(d)
                ? d
                : validBranches[0];

            var existing = await _db.TenantAccountBranches
                .Where(x => x.TenantId == tenantId && x.TenantAccountId == accountId)
                .ToListAsync(ct);
            _db.TenantAccountBranches.RemoveRange(existing);

            foreach (var branchId in validBranches)
            {
                _db.TenantAccountBranches.Add(new TenantAccountBranch
                {
                    TenantId = tenantId,
                    TenantAccountId = accountId,
                    BranchId = branchId,
                    IsDefault = branchId == defaultId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
        }
        finally
        {
            _db.BypassBranchFilter = false;
        }

        return await GetBranches(accountId, ct);
    }

    private int RequireTenant()
    {
        var tenantId = _tenantContext.TenantId
            ?? int.Parse(User.FindFirst("tenant_id")!.Value);
        return tenantId;
    }

    private async Task EnsureOwnerOrManageAllAsync(int tenantId, CancellationToken ct)
    {
        if (User.HasClaim("can_manage_all_branches", "1"))
            return;

        var accountId = _tenantContext.TenantAccountId
            ?? (int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var aid)
                ? aid
                : 0);
        if (accountId <= 0)
            throw new UnauthorizedAccessException("يجب تسجيل الدخول.");

        var ownerId = await _db.TenantAccounts.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .OrderBy(a => a.CreatedAt).ThenBy(a => a.Id)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(ct);

        if (ownerId is null || ownerId.Value != accountId)
            throw new UnauthorizedAccessException("إدارة تعيين الفروع مسموحة لحساب المالك فقط.");
    }
}

public sealed class TenantAccountListItem
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOwner { get; set; }
}

public sealed class TenantAccountBranchesResponse
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<BranchInfoDto> Branches { get; set; } = [];
    public int? DefaultBranchId { get; set; }
}

public sealed class SetTenantAccountBranchesRequest
{
    public List<int> BranchIds { get; set; } = [];
    public int? DefaultBranchId { get; set; }
}
