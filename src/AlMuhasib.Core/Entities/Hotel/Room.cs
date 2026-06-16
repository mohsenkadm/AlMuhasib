using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel;

public class Room : BaseEntity
{
    public string RoomNumber { get; set; } = string.Empty;
    public int FloorId { get; set; }
    public int RoomTypeId { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string Notes { get; set; } = string.Empty;

    public Floor Floor { get; set; } = null!;
    public RoomType RoomType { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = [];
}
