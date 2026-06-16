namespace AlMuhasib.Core.Entities.Hotel;

public class RoomType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; } = 2;
    public decimal BasePrice { get; set; }
    public int SortOrder { get; set; }
    public ICollection<Room> Rooms { get; set; } = [];
}
