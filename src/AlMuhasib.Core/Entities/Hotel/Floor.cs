namespace AlMuhasib.Core.Entities.Hotel;

public class Floor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<Room> Rooms { get; set; } = [];
}
