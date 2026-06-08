using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class UserNoteService : IUserNoteService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public UserNoteService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    private int RequireUserId()
    {
        if (_currentUserService.UserId is not int userId)
            throw new InvalidOperationException("المستخدم غير مسجل الدخول.");
        return userId;
    }

    public async Task<IReadOnlyList<UserNote>> GetAllAsync()
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserNotes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.LastEditedAt)
            .ToListAsync();
    }

    public async Task<UserNote> CreateAsync(string? title = null)
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var note = new UserNote
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? "ملاحظة جديدة" : title.Trim(),
            Content = string.Empty,
            LastEditedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };

        await context.UserNotes.AddAsync(note);
        await context.SaveChangesAsync();
        return note;
    }

    public async Task UpdateAsync(int id, string title, string content)
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var note = await context.UserNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId)
            ?? throw new InvalidOperationException("الملاحظة غير موجودة.");

        note.Title = string.IsNullOrWhiteSpace(title) ? "بدون عنوان" : title.Trim();
        note.Content = content ?? string.Empty;
        note.LastEditedAt = DateTime.UtcNow;
        note.UpdatedBy = _currentUserService.Username;
        note.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var note = await context.UserNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId)
            ?? throw new InvalidOperationException("الملاحظة غير موجودة.");

        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;
        note.DeletedBy = _currentUserService.Username;

        await context.SaveChangesAsync();
    }
}
