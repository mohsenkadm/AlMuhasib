using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class HotelAuthService : IAuthService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelAuthService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Failed("يرجى إدخال اسم المستخدم وكلمة المرور");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
            return AuthResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة");

        if (!user.IsActive)
            return AuthResult.Failed("هذا الحساب معطّل. يرجى مراجعة المسؤول");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return AuthResult.Failed("اسم المستخدم أو كلمة المرور غير صحيحة");

        return AuthResult.Succeeded(user, user.MustChangePassword);
    }

    public Task LogoutAsync() => Task.CompletedTask;

    public async Task<bool> HasPermissionAsync(int userId, string screenName, string action)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var permission = await context.Permissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ScreenName == screenName);

        if (permission is null)
            return false;

        return action switch
        {
            "View" => permission.CanView,
            "Add" => permission.CanAdd,
            "Edit" => permission.CanEdit,
            "Delete" => permission.CanDelete,
            "Print" => permission.CanPrint,
            "Export" => permission.CanExport,
            _ => false
        };
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user is null)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<User>> GetActiveAdminUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive && u.Role == UserRole.Admin)
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive)
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Username)
            .ToListAsync();
    }

    public async Task EnsureAdminAccountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (existingAdmin is not null)
        {
            if (string.IsNullOrWhiteSpace(existingAdmin.FullName))
            {
                existingAdmin.FullName = "مدير النظام";
                await context.SaveChangesAsync();
            }
            return;
        }

        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            FullName = "مدير النظام",
            Role = UserRole.Admin,
            IsActive = true,
            MustChangePassword = true
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.Where(u => !u.IsDeleted).OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task<User> CreateUserAsync(string username, string password, string fullName, UserRole role)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var exists = await context.Users.AnyAsync(u => u.Username == username && !u.IsDeleted);
        if (exists)
            throw new InvalidOperationException("اسم المستخدم موجود مسبقاً");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Role = role,
            IsActive = true,
            MustChangePassword = true,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUserAsync(int userId, string fullName, UserRole role)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        user.FullName = fullName;
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(int userId, string newPassword)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task SetUserActiveAsync(int userId, bool isActive)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<List<Permission>> GetUserPermissionsAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Permissions.Where(p => p.UserId == userId && !p.IsDeleted).ToListAsync();
    }

    public async Task SaveUserPermissionsAsync(int userId, List<Permission> permissions)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Permissions.Where(p => p.UserId == userId).ToListAsync();
        context.Permissions.RemoveRange(existing);

        foreach (var p in permissions)
        {
            p.UserId = userId;
            p.CreatedBy = "admin";
            p.CreatedAt = DateTime.UtcNow;
        }

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }
}
