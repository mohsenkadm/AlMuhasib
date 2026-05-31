using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Services;

public sealed class FavoriteProductsService : IFavoriteProductsService
{
    private readonly IUserPreferencesService _preferences;

    public FavoriteProductsService(IUserPreferencesService preferences) => _preferences = preferences;

    public IReadOnlyList<int> GetFavoriteProductIds() =>
        _preferences.Current.FavoriteProductIds.ToList();

    public bool IsFavorite(int productId) =>
        _preferences.Current.FavoriteProductIds.Contains(productId);

    public void ToggleFavorite(int productId)
    {
        var list = _preferences.Current.FavoriteProductIds.ToList();
        if (list.Contains(productId))
            list.Remove(productId);
        else
        {
            if (list.Count >= IFavoriteProductsService.MaxFavorites)
                list.RemoveAt(0);
            list.Add(productId);
        }

        _preferences.Update(p => p.FavoriteProductIds = list);
    }
}
