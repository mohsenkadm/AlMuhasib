using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantMenuCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ColorHex { get; set; } = "#00897B";
    public bool IsActive { get; set; } = true;

    public ICollection<RestaurantMenuItem> MenuItems { get; set; } = [];
}
