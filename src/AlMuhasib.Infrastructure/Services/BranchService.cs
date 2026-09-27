using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class BranchService : IBranchService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUser;

    public BranchService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUser)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        return await db.Branches.AsNoTracking().OrderByDescending(b => b.IsMain).ThenBy(b => b.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Branch>> GetActiveAsync(CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        return await db.Branches.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.IsMain)
            .ThenBy(b => b.Name)
            .ToListAsync(ct);
    }

    public async Task<Branch?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        return await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Branch> GetMainBranchAsync(CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        var main = await db.Branches.FirstOrDefaultAsync(b => b.IsMain, ct)
            ?? await db.Branches.FirstOrDefaultAsync(b => b.Code == Branch.MainBranchCode, ct);
        if (main is null)
            throw new InvalidOperationException("الفرع الرئيسي غير موجود. طبّق ترحيل قاعدة البيانات أولاً.");
        return main;
    }

    public async Task<Branch> CreateAsync(string name, string code, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        name = name.Trim();
        code = code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("اسم ورمز الفرع مطلوبان.");

        if (await db.Branches.AnyAsync(b => b.Code == code, ct))
            throw new InvalidOperationException("رمز الفرع مستخدم مسبقاً.");

        var branch = new Branch
        {
            Name = name,
            Code = code,
            IsActive = true,
            IsMain = false,
            CreatedBy = _currentUser.Username
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync(ct);
        return branch;
    }

    public async Task UpdateAsync(int id, string name, string code, bool isActive, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new InvalidOperationException("الفرع غير موجود.");

        name = name.Trim();
        code = code.Trim().ToUpperInvariant();
        if (await db.Branches.AnyAsync(b => b.Code == code && b.Id != id, ct))
            throw new InvalidOperationException("رمز الفرع مستخدم مسبقاً.");

        if (branch.IsMain && !isActive)
            throw new InvalidOperationException("لا يمكن تعطيل الفرع الرئيسي.");

        branch.Name = name;
        branch.Code = code;
        branch.IsActive = isActive;
        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = _currentUser.Username;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new InvalidOperationException("الفرع غير موجود.");
        if (branch.IsMain)
            throw new InvalidOperationException("لا يمكن تعطيل الفرع الرئيسي.");
        branch.IsActive = false;
        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = _currentUser.Username;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Branch>> GetBranchesForUserAsync(int userId, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return [];

        // Admin with ManageAllBranches sees all active branches (assignment still used as default).
        var branches = await db.UserBranches.AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .Join(db.Branches.Where(b => b.IsActive), ub => ub.BranchId, b => b.Id, (_, b) => b)
            .OrderByDescending(b => b.IsMain)
            .ThenBy(b => b.Name)
            .ToListAsync(ct);

        if (branches.Count > 0)
            return branches;

        // Single-branch heal only (legacy gap). Multi-branch with empty assignments stays empty.
        var sole = await db.Branches.AsNoTracking()
            .Where(b => b.IsActive && !b.IsDeleted)
            .OrderByDescending(b => b.IsMain)
            .ThenBy(b => b.Id)
            .Take(2)
            .ToListAsync(ct);
        if (sole.Count != 1)
            return branches;

        var only = sole[0];
        db.UserBranches.Add(new UserBranch
        {
            UserId = userId,
            BranchId = only.Id,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        branches.Add(only);
        return branches;
    }

    public async Task<int?> GetDefaultBranchIdForUserAsync(int userId, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        var def = await db.UserBranches.AsNoTracking()
            .Where(ub => ub.UserId == userId && ub.IsDefault)
            .Select(ub => (int?)ub.BranchId)
            .FirstOrDefaultAsync(ct);
        if (def.HasValue)
            return def;

        return await db.UserBranches.AsNoTracking()
            .Where(ub => ub.UserId == userId)
            .OrderBy(ub => ub.Id)
            .Select(ub => (int?)ub.BranchId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AssignUserBranchesAsync(
        int userId, IReadOnlyList<int> branchIds, int? defaultBranchId, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        var userExists = await db.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
            throw new InvalidOperationException("المستخدم غير موجود.");

        var distinct = branchIds.Where(id => id > 0).Distinct().ToList();
        if (distinct.Count == 0)
            throw new InvalidOperationException("يجب ربط المستخدم بفرع واحد على الأقل.");

        var validCount = await db.Branches.CountAsync(b => distinct.Contains(b.Id) && b.IsActive, ct);
        if (validCount != distinct.Count)
            throw new InvalidOperationException("أحد الفروع غير صالح أو غير نشط.");

        var defaultId = defaultBranchId is > 0 && distinct.Contains(defaultBranchId.Value)
            ? defaultBranchId.Value
            : distinct[0];

        var existing = await db.UserBranches.Where(ub => ub.UserId == userId).ToListAsync(ct);
        db.UserBranches.RemoveRange(existing);

        foreach (var branchId in distinct)
        {
            db.UserBranches.Add(new UserBranch
            {
                UserId = userId,
                BranchId = branchId,
                IsDefault = branchId == defaultId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task EnsureUserLinkedToMainAsync(int userId, CancellationToken ct = default)
    {
        await using var db = await CreateBypassAsync(ct);
        if (await db.UserBranches.AnyAsync(ub => ub.UserId == userId, ct))
            return;

        var main = await db.Branches.FirstAsync(b => b.IsMain, ct);
        db.UserBranches.Add(new UserBranch
        {
            UserId = userId,
            BranchId = main.Id,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<AppDbContext> CreateBypassAsync(CancellationToken ct)
    {
        var db = await _contextFactory.CreateDbContextAsync(ct);
        db.BypassBranchFilter = true;
        return db;
    }
}
