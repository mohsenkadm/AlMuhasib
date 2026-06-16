using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelMasterDataService
{
    Task<IReadOnlyList<Floor>> GetFloorsAsync(CancellationToken cancellationToken = default);
    Task<Floor?> GetFloorByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Floor> CreateFloorAsync(Floor floor, CancellationToken cancellationToken = default);
    Task<Floor> UpdateFloorAsync(Floor floor, CancellationToken cancellationToken = default);
    Task DeleteFloorAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomType>> GetRoomTypesAsync(CancellationToken cancellationToken = default);
    Task<RoomType?> GetRoomTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RoomType> CreateRoomTypeAsync(RoomType roomType, CancellationToken cancellationToken = default);
    Task<RoomType> UpdateRoomTypeAsync(RoomType roomType, CancellationToken cancellationToken = default);
    Task DeleteRoomTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomListItem>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<Room?> GetRoomByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Room> CreateRoomAsync(Room room, CancellationToken cancellationToken = default);
    Task<Room> UpdateRoomAsync(Room room, CancellationToken cancellationToken = default);
    Task DeleteRoomAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> BulkAddRoomsAsync(BulkAddRoomsRequest request, CancellationToken cancellationToken = default);
}
