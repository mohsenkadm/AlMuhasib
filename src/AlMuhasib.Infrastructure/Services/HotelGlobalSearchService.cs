using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class HotelGlobalSearchService : IHotelGlobalSearchService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelGlobalSearchService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<GlobalSearchHit>> SearchAsync(
        string term,
        int maxResults = 30,
        CancellationToken cancellationToken = default)
    {
        term = term?.Trim() ?? string.Empty;
        if (term.Length < 2)
            return [];

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var like = $"%{term}%";
        var perKind = Math.Max(1, maxResults / 3);

        var guestHits = await context.Guests.AsNoTracking()
            .Where(g =>
                EF.Functions.Like(g.FullName, like) ||
                EF.Functions.Like(g.Phone, like) ||
                EF.Functions.Like(g.IdNumber, like) ||
                EF.Functions.Like(g.Email, like))
            .OrderBy(g => g.FullName)
            .Take(perKind)
            .Select(g => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.HotelGuest,
                EntityId = g.Id,
                Title = g.FullName,
                Subtitle = g.Phone,
                ScreenName = HotelPermissionRegistryScreen.Guests
            })
            .ToListAsync(cancellationToken);

        var roomHits = await context.Rooms.AsNoTracking()
            .Where(r => EF.Functions.Like(r.RoomNumber, like))
            .OrderBy(r => r.RoomNumber)
            .Take(perKind)
            .Select(r => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.HotelRoom,
                EntityId = r.Id,
                Title = r.RoomNumber,
                Subtitle = r.Floor.Name,
                ScreenName = HotelPermissionRegistryScreen.Rooms
            })
            .ToListAsync(cancellationToken);

        var reservationHits = await context.Reservations.AsNoTracking()
            .Where(r =>
                EF.Functions.Like(r.ReservationNumber, like) ||
                EF.Functions.Like(r.Guest.FullName, like) ||
                (r.Room != null && EF.Functions.Like(r.Room.RoomNumber, like)))
            .OrderByDescending(r => r.CheckInDate)
            .Take(perKind)
            .Select(r => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.HotelReservation,
                EntityId = r.Id,
                Title = r.ReservationNumber,
                Subtitle = r.Room != null
                    ? $"{r.Guest.FullName} — {r.Room.RoomNumber}"
                    : $"{r.Guest.FullName} — بدون غرفة",
                ScreenName = HotelPermissionRegistryScreen.Reservations
            })
            .ToListAsync(cancellationToken);

        return guestHits
            .Concat(roomHits)
            .Concat(reservationHits)
            .Take(maxResults)
            .ToList();
    }
}

internal static class HotelPermissionRegistryScreen
{
    public const string Guests = "HotelGuests";
    public const string Rooms = "HotelRooms";
    public const string Reservations = "HotelReservations";
}
