using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelGuestService : IGuestService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelGuestService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Guest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Guests.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<GuestListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildGuestQuery(context, searchTerm);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(g => g.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GuestListItem
            {
                Id = g.Id,
                FullName = g.FullName,
                IdNumber = g.IdNumber,
                Phone = g.Phone,
                Email = g.Email,
                ReservationCount = g.Reservations.Count
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<GuestListItem>> SearchAsync(
        string term,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await BuildGuestQuery(context, term)
            .OrderBy(g => g.FullName)
            .Take(maxResults)
            .Select(g => new GuestListItem
            {
                Id = g.Id,
                FullName = g.FullName,
                IdNumber = g.IdNumber,
                Phone = g.Phone,
                Email = g.Email,
                ReservationCount = g.Reservations.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationListItem>> GetReservationsByGuestIdAsync(
        int guestId,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await HotelReservationAmountHelper.ProjectListItems(
                context.Reservations.AsNoTracking()
                    .Where(r => r.GuestId == guestId))
            .OrderByDescending(r => r.CheckInDate)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guest> CreateAsync(Guest guest, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Guests.AddAsync(guest, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return guest;
    }

    public async Task<Guest> UpdateAsync(Guest guest, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.Guests.FirstOrDefaultAsync(g => g.Id == guest.Id, cancellationToken)
            ?? throw new InvalidOperationException("النزيل غير موجود");

        existing.FullName = guest.FullName;
        existing.IdNumber = guest.IdNumber;
        existing.Phone = guest.Phone;
        existing.Email = guest.Email;
        existing.Notes = guest.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var guest = await context.Guests.FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("النزيل غير موجود");

        guest.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Guest> BuildGuestQuery(HotelDbContext context, string? searchTerm)
    {
        var query = context.Guests.AsQueryable();
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var term = searchTerm.Trim();
        var like = $"%{term}%";
        return query.Where(g =>
            EF.Functions.Like(g.FullName, like) ||
            EF.Functions.Like(g.Phone, like) ||
            EF.Functions.Like(g.IdNumber, like) ||
            EF.Functions.Like(g.Email, like));
    }
}
