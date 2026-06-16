using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed class TenantService : ITenantService
{
    private readonly CloudDbContext _db;

    public TenantService(CloudDbContext db)
    {
        _db = db;
    }

    public Task<List<Tenant>> GetAllAsync(CancellationToken ct = default) =>
        _db.Tenants.AsNoTracking().OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

    public Task<Tenant?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Tenants.Include(t => t.Accounts).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Tenant> CreateAsync(string companyName, bool isMobileEnabled, DateTime? licenseExpiresAt, int applicationSystemType = 0, CancellationToken ct = default)
    {
        var tenant = new Tenant
        {
            CompanyName = companyName,
            IsMobileEnabled = isMobileEnabled,
            LicenseExpiresAt = licenseExpiresAt,
            ApplicationSystemType = applicationSystemType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _db.Tenants.Update(tenant);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TenantAccount> CreateAccountAsync(int tenantId, string username, string password, DateTime? expiresAt, CancellationToken ct = default)
    {
        if (await _db.TenantAccounts.AnyAsync(a => a.Username == username, ct))
            throw new InvalidOperationException("اسم المستخدم موجود مسبقاً");

        var account = new TenantAccount
        {
            TenantId = tenantId,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.TenantAccounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return account;
    }

    public Task<List<TenantAccount>> GetAccountsAsync(int tenantId, CancellationToken ct = default) =>
        _db.TenantAccounts.AsNoTracking().Where(a => a.TenantId == tenantId).ToListAsync(ct);

    public async Task ResetPasswordAsync(int accountId, string newPassword, CancellationToken ct = default)
    {
        var account = await _db.TenantAccounts.FindAsync([accountId], ct)
            ?? throw new InvalidOperationException("الحساب غير موجود");
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetAccountActiveAsync(int accountId, bool isActive, CancellationToken ct = default)
    {
        var account = await _db.TenantAccounts.FindAsync([accountId], ct)
            ?? throw new InvalidOperationException("الحساب غير موجود");
        account.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ExtendAccountAsync(int accountId, DateTime? expiresAt, CancellationToken ct = default)
    {
        var account = await _db.TenantAccounts.FindAsync([accountId], ct)
            ?? throw new InvalidOperationException("الحساب غير موجود");
        account.ExpiresAt = expiresAt;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ExtendLicenseAsync(int tenantId, DateTime? licenseExpiresAt, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync([tenantId], ct)
            ?? throw new InvalidOperationException("العميل غير موجود");
        tenant.LicenseExpiresAt = licenseExpiresAt;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetMobileEnabledAsync(int tenantId, bool enabled, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FindAsync([tenantId], ct)
            ?? throw new InvalidOperationException("العميل غير موجود");
        tenant.IsMobileEnabled = enabled;
        await _db.SaveChangesAsync(ct);
    }
}
