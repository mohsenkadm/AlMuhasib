using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelMasterDataService : IHotelMasterDataService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelMasterDataService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Floor>> GetFloorsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Floors
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Floor?> GetFloorByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Floors.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<Floor> CreateFloorAsync(Floor floor, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Floors.AddAsync(floor, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return floor;
    }

    public async Task<Floor> UpdateFloorAsync(Floor floor, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.Floors.FirstOrDefaultAsync(f => f.Id == floor.Id, cancellationToken)
            ?? throw new InvalidOperationException("الطابق غير موجود");

        existing.Name = floor.Name;
        existing.SortOrder = floor.SortOrder;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteFloorAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var floor = await context.Floors.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الطابق غير موجود");

        floor.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoomType>> GetRoomTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RoomTypes
            .OrderBy(rt => rt.SortOrder)
            .ThenBy(rt => rt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoomType?> GetRoomTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<RoomType> CreateRoomTypeAsync(RoomType roomType, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.RoomTypes.AddAsync(roomType, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return roomType;
    }

    public async Task<RoomType> UpdateRoomTypeAsync(RoomType roomType, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == roomType.Id, cancellationToken)
            ?? throw new InvalidOperationException("نوع الغرفة غير موجود");

        existing.Name = roomType.Name;
        existing.Description = roomType.Description;
        existing.Capacity = roomType.Capacity;
        existing.BasePrice = roomType.BasePrice;
        existing.SortOrder = roomType.SortOrder;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteRoomTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var roomType = await context.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("نوع الغرفة غير موجود");

        roomType.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoomListItem>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var checkedInGuestNames = await context.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.CheckedIn && r.RoomId != null)
            .Select(r => new { RoomId = r.RoomId!.Value, r.Guest.FullName })
            .ToDictionaryAsync(x => x.RoomId, x => x.FullName, cancellationToken);

        var rooms = await context.Rooms
            .AsNoTracking()
            .Include(r => r.Floor)
            .Include(r => r.RoomType)
            .OrderBy(r => r.Floor.SortOrder)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync(cancellationToken);

        return rooms.Select(r => new RoomListItem
        {
            Id = r.Id,
            RoomNumber = r.RoomNumber,
            FloorName = r.Floor.Name,
            RoomTypeName = r.RoomType.Name,
            Status = r.Status,
            Capacity = r.RoomType.Capacity,
            BasePrice = r.RoomType.BasePrice,
            CurrentGuestName = checkedInGuestNames.GetValueOrDefault(r.Id)
        }).ToList();
    }

    public async Task<Room?> GetRoomByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Rooms
            .Include(r => r.Floor)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Room> CreateRoomAsync(Room room, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Rooms.AddAsync(room, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return room;
    }

    public async Task<Room> UpdateRoomAsync(Room room, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.Rooms.FirstOrDefaultAsync(r => r.Id == room.Id, cancellationToken)
            ?? throw new InvalidOperationException("الغرفة غير موجودة");

        existing.RoomNumber = room.RoomNumber;
        existing.FloorId = room.FloorId;
        existing.RoomTypeId = room.RoomTypeId;
        existing.Status = room.Status;
        existing.Notes = room.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteRoomAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الغرفة غير موجودة");

        room.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> BulkAddRoomsAsync(
        BulkAddRoomsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromNumber > request.ToNumber)
            throw new InvalidOperationException("نطاق أرقام الغرف غير صحيح");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var floorExists = await context.Floors.AnyAsync(f => f.Id == request.FloorId, cancellationToken);
        if (!floorExists)
            throw new InvalidOperationException("الطابق غير موجود");

        var roomTypeExists = await context.RoomTypes.AnyAsync(rt => rt.Id == request.RoomTypeId, cancellationToken);
        if (!roomTypeExists)
            throw new InvalidOperationException("نوع الغرفة غير موجود");

        var rooms = new List<Room>();
        for (var n = request.FromNumber; n <= request.ToNumber; n++)
        {
            rooms.Add(new Room
            {
                RoomNumber = $"{request.NumberPrefix}{n}",
                FloorId = request.FloorId,
                RoomTypeId = request.RoomTypeId,
                Status = request.InitialStatus
            });
        }

        await context.Rooms.AddRangeAsync(rooms, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return rooms;
    }
}
