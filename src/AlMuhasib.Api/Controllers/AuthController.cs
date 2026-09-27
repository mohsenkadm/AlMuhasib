using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Services;
using AlMuhasib.Sync;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly IAuthTokenService _tokenService;
    private readonly ILicenseValidator _licenseValidator;

    public AuthController(CloudDbContext db, IAuthTokenService tokenService, ILicenseValidator licenseValidator)
    {
        _db = db;
        _tokenService = tokenService;
        _licenseValidator = licenseValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TenantLoginResponse>> Login([FromBody] TenantLoginRequest request, CancellationToken ct)
    {
        _db.BypassBranchFilter = true;

        var account = await _db.TenantAccounts
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.Username == request.Username, ct);

        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            return Unauthorized(new ApiErrorResponse { Code = SyncErrorCodes.InvalidCredentials, Message = "بيانات الدخول غير صحيحة" });

        var license = _licenseValidator.Validate(account.Tenant, account);
        if (!license.IsValid)
            return StatusCode(403, new ApiErrorResponse { Code = license.ErrorCode!, Message = license.Message! });

        await EnsureMainBranchAndAssignmentAsync(account, ct);

        var branches = await LoadAllowedBranchesAsync(account.TenantId, account.Id, ct);
        var canManageAll = false; // reserved for future account-level flags
        var canViewAll = canManageAll;

        int? autoBranchId = null;
        var requiresSelection = branches.Count > 1;
        if (branches.Count == 1)
            autoBranchId = branches[0].BranchId;
        else if (branches.Count > 1)
            autoBranchId = branches.FirstOrDefault(b => b.IsDefault)?.BranchId;

        // Issue token with branch only when single-branch (no picker needed).
        var response = _tokenService.CreateTenantTokens(
            account,
            account.Tenant,
            branchId: branches.Count == 1 ? autoBranchId : null,
            canViewAllBranches: canViewAll,
            canManageAllBranches: canManageAll);

        response.AllowedBranches = branches;
        response.DefaultBranchId = autoBranchId;
        response.RequiresBranchSelection = requiresSelection;
        response.CurrentBranchId = branches.Count == 1 ? autoBranchId : null;

        account.RefreshToken = response.RefreshToken;
        account.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync(ct);
        return Ok(response);
    }

    [HttpPost("select-branch")]
    [Authorize(Policy = "Tenant")]
    public async Task<ActionResult<SelectBranchResponse>> SelectBranch(
        [FromBody] SelectBranchRequest request, CancellationToken ct)
    {
        _db.BypassBranchFilter = true;

        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var account = await _db.TenantAccounts.Include(a => a.Tenant)
            .FirstAsync(a => a.Id == accountId && a.TenantId == tenantId, ct);

        var branches = await LoadAllowedBranchesAsync(tenantId, accountId, ct);
        var allowedIds = branches.Select(b => b.BranchId).ToHashSet();
        var canViewAll = User.HasClaim("can_view_all_branches", "1")
                         || User.HasClaim("can_manage_all_branches", "1");
        var canManageAll = User.HasClaim("can_manage_all_branches", "1");

        if (request.AllBranches)
        {
            if (!canViewAll)
                return StatusCode(403, new ApiErrorResponse { Code = "BRANCH_FORBIDDEN", Message = "غير مصرح بعرض كل الفروع." });

            var tokens = _tokenService.CreateTenantTokens(
                account, account.Tenant, allBranches: true,
                canViewAllBranches: true, canManageAllBranches: canManageAll);

            return Ok(new SelectBranchResponse
            {
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                CurrentBranchId = null,
                IsAllBranchesMode = true
            });
        }

        if (!allowedIds.Contains(request.BranchId) && !canManageAll)
            return StatusCode(403, new ApiErrorResponse { Code = "BRANCH_FORBIDDEN", Message = "لا تملك صلاحية هذا الفرع." });

        var branch = branches.FirstOrDefault(b => b.BranchId == request.BranchId);
        if (branch is null)
        {
            var entity = await _db.Branches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.TenantId == tenantId && b.IsActive, ct);
            if (entity is null)
                return StatusCode(403, new ApiErrorResponse { Code = "BRANCH_FORBIDDEN", Message = "الفرع غير موجود." });
            branch = new BranchInfoDto
            {
                BranchId = entity.Id,
                SyncId = entity.SyncId,
                Name = entity.Name,
                Code = entity.Code,
                IsMain = entity.IsMain
            };
        }

        var response = _tokenService.CreateTenantTokens(
            account, account.Tenant, branchId: request.BranchId,
            canViewAllBranches: canViewAll, canManageAllBranches: canManageAll);

        return Ok(new SelectBranchResponse
        {
            AccessToken = response.AccessToken,
            AccessTokenExpiresAt = response.AccessTokenExpiresAt,
            CurrentBranchId = request.BranchId,
            IsAllBranchesMode = false,
            Branch = branch
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TenantLoginResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        _db.BypassBranchFilter = true;

        var account = await _db.TenantAccounts
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.RefreshToken == request.RefreshToken, ct);

        if (account is null || account.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Unauthorized(new ApiErrorResponse { Code = SyncErrorCodes.InvalidCredentials, Message = "Refresh token غير صالح" });

        var license = _licenseValidator.Validate(account.Tenant, account);
        if (!license.IsValid)
            return StatusCode(403, new ApiErrorResponse { Code = license.ErrorCode!, Message = license.Message! });

        await EnsureMainBranchAndAssignmentAsync(account, ct);
        var branches = await LoadAllowedBranchesAsync(account.TenantId, account.Id, ct);

        var response = _tokenService.CreateTenantTokens(
            account,
            account.Tenant,
            branchId: branches.Count == 1 ? branches[0].BranchId : null);

        response.AllowedBranches = branches;
        response.DefaultBranchId = branches.FirstOrDefault(b => b.IsDefault)?.BranchId
                                   ?? (branches.Count == 1 ? branches[0].BranchId : null);
        response.RequiresBranchSelection = branches.Count > 1;
        response.CurrentBranchId = branches.Count == 1 ? branches[0].BranchId : null;

        account.RefreshToken = response.RefreshToken;
        account.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync(ct);
        return Ok(response);
    }

    [HttpGet("license-status")]
    [Authorize(Policy = "Tenant")]
    public async Task<ActionResult<LicenseStatusResponse>> LicenseStatus(CancellationToken ct)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId, ct);
        var account = await _db.TenantAccounts.AsNoTracking().FirstAsync(a => a.Id == accountId, ct);
        var license = _licenseValidator.Validate(tenant, account);

        return Ok(new LicenseStatusResponse
        {
            IsActive = account.IsActive && tenant.IsActive,
            IsMobileEnabled = tenant.IsMobileEnabled,
            LicenseExpiresAt = tenant.LicenseExpiresAt,
            AccountExpiresAt = account.ExpiresAt,
            StatusCode = license.IsValid ? null : license.ErrorCode,
            Message = license.Message
        });
    }

    [HttpPost("developer/login")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> DeveloperLogin([FromBody] DeveloperLoginRequest request, CancellationToken ct)
    {
        var user = await _db.DeveloperUsers.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ApiErrorResponse { Code = SyncErrorCodes.InvalidCredentials, Message = "بيانات المطور غير صحيحة" });

        return Ok(new { accessToken = _tokenService.CreateDeveloperToken(user), username = user.Username });
    }

    private async Task EnsureMainBranchAndAssignmentAsync(TenantAccount account, CancellationToken ct)
    {
        var main = await _db.Branches.FirstOrDefaultAsync(
            b => b.TenantId == account.TenantId && b.IsMain && !b.IsDeleted, ct);

        if (main is null)
        {
            main = new CloudBranch
            {
                TenantId = account.TenantId,
                Name = "الفرع الرئيسي",
                Code = CloudBranch.MainBranchCode,
                IsActive = true,
                IsMain = true,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            _db.Branches.Add(main);
            await _db.SaveChangesAsync(ct);

            // Backfill any business rows still at BranchId=0 for this tenant.
            await BackfillTenantBranchIdsAsync(account.TenantId, main.Id, ct);
        }

        var hasAssignment = await _db.TenantAccountBranches
            .AnyAsync(x => x.TenantAccountId == account.Id, ct);
        if (!hasAssignment)
        {
            _db.TenantAccountBranches.Add(new TenantAccountBranch
            {
                TenantId = account.TenantId,
                TenantAccountId = account.Id,
                BranchId = main.Id,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task BackfillTenantBranchIdsAsync(int tenantId, int branchId, CancellationToken ct)
    {
        // Idempotent SQL backfill for rows still at BranchId = 0 after schema add.
        string[] tables =
        [
            "Categories", "Products", "PricingTypes", "ProductPrices", "BusinessSettings", "ExchangeRates",
            "Warehouses", "Customers", "Suppliers", "CashBoxes", "BankAccounts", "Investors", "ExpenseTypes",
            "PrintBrandingSettings", "WarehouseStocks", "WarehouseTransfers", "WarehouseTransferItems",
            "Invoices", "InvoiceItems", "ProductOffers", "InstallmentPlans", "Installments",
            "Vouchers", "Expenses", "Transfers", "InvestorTransactions", "ProfitDistributions",
            "ProfitDistributionDetails", "CapitalEntries", "CustomerAttachments"
        ];

        foreach (var table in tables)
        {
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    $"UPDATE [{table}] SET [BranchId] = {{0}} WHERE [TenantId] = {{1}} AND [BranchId] = 0",
                    new object[] { branchId, tenantId },
                    ct);
            }
            catch
            {
                // Table may not exist for this tenant system type.
            }
        }
    }

    private async Task<List<BranchInfoDto>> LoadAllowedBranchesAsync(int tenantId, int accountId, CancellationToken ct)
    {
        var assigned = await _db.TenantAccountBranches.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TenantAccountId == accountId)
            .Join(_db.Branches.Where(b => b.IsActive && !b.IsDeleted),
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

        if (assigned.Count > 0)
            return assigned;

        return await _db.Branches.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.IsActive && !b.IsDeleted)
            .OrderByDescending(b => b.IsMain)
            .ThenBy(b => b.Name)
            .Select(b => new BranchInfoDto
            {
                BranchId = b.Id,
                SyncId = b.SyncId,
                Name = b.Name,
                Code = b.Code,
                IsMain = b.IsMain,
                IsDefault = b.IsMain
            })
            .ToListAsync(ct);
    }
}
