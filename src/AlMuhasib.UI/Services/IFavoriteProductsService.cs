namespace AlMuhasib.UI.Services;

public interface IFavoriteProductsService
{
    const int MaxFavorites = 12;

    IReadOnlyList<int> GetFavoriteProductIds();
    bool IsFavorite(int productId);
    void ToggleFavorite(int productId);
}
