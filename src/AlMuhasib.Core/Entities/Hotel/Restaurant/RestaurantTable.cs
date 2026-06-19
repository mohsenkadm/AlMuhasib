using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantTable : BaseEntity
{
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;
    public RestaurantTableStatus Status { get; set; } = RestaurantTableStatus.Available;
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<RestaurantOrder> Orders { get; set; } = [];
}
