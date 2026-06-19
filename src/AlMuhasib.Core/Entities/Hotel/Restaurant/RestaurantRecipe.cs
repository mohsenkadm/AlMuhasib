using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantRecipe : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public ICollection<RestaurantRecipeLine> Lines { get; set; } = [];
    public RestaurantMenuItem? MenuItem { get; set; }
}
