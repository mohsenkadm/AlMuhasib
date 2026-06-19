using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel.Restaurant;

public sealed class RestaurantMenuService : IRestaurantMenuService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public RestaurantMenuService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task EnsureSeedDataAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        if (await db.RestaurantMenuCategories.AnyAsync(ct))
            return;

        var categories = new[]
        {
            new RestaurantMenuCategory { Name = "مشروبات", SortOrder = 1, ColorHex = "#00897B" },
            new RestaurantMenuCategory { Name = "مقبلات", SortOrder = 2, ColorHex = "#FF8F00" },
            new RestaurantMenuCategory { Name = "أطباق رئيسية", SortOrder = 3, ColorHex = "#5E35B1" },
            new RestaurantMenuCategory { Name = "حلويات", SortOrder = 4, ColorHex = "#E91E63" },
            new RestaurantMenuCategory { Name = "سفري", SortOrder = 5, ColorHex = "#546E7A" }
        };
        await db.RestaurantMenuCategories.AddRangeAsync(categories, ct);
        await db.SaveChangesAsync(ct);

        var ingredients = new[]
        {
            new RestaurantIngredient { Name = "لحم", Unit = "كغ", MinQuantity = 5, AverageCost = 15000 },
            new RestaurantIngredient { Name = "دجاج", Unit = "كغ", MinQuantity = 5, AverageCost = 8000 },
            new RestaurantIngredient { Name = "أرز", Unit = "كغ", MinQuantity = 10, AverageCost = 2500 },
            new RestaurantIngredient { Name = "خضار", Unit = "كغ", MinQuantity = 3, AverageCost = 1500 },
            new RestaurantIngredient { Name = "زيت", Unit = "لتر", MinQuantity = 2, AverageCost = 5000 },
            new RestaurantIngredient { Name = "قهوة", Unit = "كغ", MinQuantity = 1, AverageCost = 12000 },
            new RestaurantIngredient { Name = "شاي", Unit = "كغ", MinQuantity = 1, AverageCost = 8000 },
            new RestaurantIngredient { Name = "حليب", Unit = "لتر", MinQuantity = 5, AverageCost = 2000 },
            new RestaurantIngredient { Name = "دقيق", Unit = "كغ", MinQuantity = 5, AverageCost = 1800 },
            new RestaurantIngredient { Name = "سكر", Unit = "كغ", MinQuantity = 3, AverageCost = 1200 }
        };
        await db.RestaurantIngredients.AddRangeAsync(ingredients, ct);
        await db.SaveChangesAsync(ct);

        foreach (var ing in ingredients)
        {
            await db.RestaurantIngredientStocks.AddAsync(new RestaurantIngredientStock
            {
                RestaurantIngredientId = ing.Id,
                Quantity = ing.MinQuantity * 2
            }, ct);
        }

        for (var i = 1; i <= 8; i++)
        {
            await db.RestaurantTables.AddAsync(new RestaurantTable
            {
                TableNumber = i.ToString(),
                Capacity = 4,
                SortOrder = i
            }, ct);
        }

        var expenseType = await db.HotelExpenseTypes.FirstOrDefaultAsync(t => t.Name == "مشتريات مطعم", ct);
        if (expenseType is null)
        {
            await db.HotelExpenseTypes.AddAsync(new Core.Entities.Hotel.HotelExpenseType
            {
                Name = "مشتريات مطعم",
                Notes = "مصاريف شراء مخزون المطعم"
            }, ct);
        }

        await db.SaveChangesAsync(ct);

        var menuItems = new (string name, int catIdx, decimal price)[]
        {
            ("قهوة عربية", 0, 3000),
            ("شاي", 0, 2000),
            ("عصير برتقال", 0, 4000),
            ("ماء معدني", 0, 1000),
            ("سلطة خضراء", 1, 5000),
            ("حمص", 1, 4000),
            ("كباب لحم", 2, 15000),
            ("دجاج مشوي", 2, 12000),
            ("برياني", 2, 10000),
            ("سمك مشوي", 2, 18000),
            ("أرز أبيض", 2, 3000),
            ("كنافة", 3, 6000),
            ("آيس كريم", 3, 4000),
            ("ساندويش دجاج", 4, 7000),
            ("برجر", 4, 9000)
        };

        foreach (var (name, catIdx, price) in menuItems)
        {
            var recipe = new RestaurantRecipe { Name = name };
            await db.RestaurantRecipes.AddAsync(recipe, ct);
            await db.SaveChangesAsync(ct);

            var item = new RestaurantMenuItem
            {
                RestaurantMenuCategoryId = categories[catIdx].Id,
                Name = name,
                SalePrice = price,
                RecipeId = recipe.Id
            };
            await db.RestaurantMenuItems.AddAsync(item, ct);
            await db.SaveChangesAsync(ct);

            var mainIng = ingredients[catIdx % ingredients.Length];
            await db.RestaurantRecipeLines.AddAsync(new RestaurantRecipeLine
            {
                RestaurantRecipeId = recipe.Id,
                RestaurantIngredientId = mainIng.Id,
                Quantity = 0.25m
            }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RestaurantMenuCategory>> GetCategoriesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var query = db.RestaurantMenuCategories.AsQueryable();
        if (activeOnly)
            query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<RestaurantMenuCategory> SaveCategoryAsync(RestaurantMenuCategory category, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        if (category.Id == 0)
        {
            await db.RestaurantMenuCategories.AddAsync(category, ct);
        }
        else
        {
            var existing = await db.RestaurantMenuCategories.FirstOrDefaultAsync(c => c.Id == category.Id, ct)
                ?? throw new InvalidOperationException("الفئة غير موجودة");
            existing.Name = category.Name;
            existing.SortOrder = category.SortOrder;
            existing.ColorHex = category.ColorHex;
            existing.IsActive = category.IsActive;
        }

        await db.SaveChangesAsync(ct);
        return category;
    }

    public async Task DeleteCategoryAsync(int id, string deletedBy, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var category = await db.RestaurantMenuCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("الفئة غير موجودة");
        category.MarkSoftDeleted(deletedBy);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RestaurantMenuItem>> GetMenuItemsAsync(int? categoryId = null, bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var query = db.RestaurantMenuItems.Include(m => m.Category).AsQueryable();
        if (categoryId.HasValue)
            query = query.Where(m => m.RestaurantMenuCategoryId == categoryId.Value);
        if (activeOnly)
            query = query.Where(m => m.IsActive);
        return await query.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToListAsync(ct);
    }

    public async Task<RestaurantMenuItem?> GetMenuItemByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantMenuItems.Include(m => m.Category).Include(m => m.Recipe).FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<RestaurantMenuItem> SaveMenuItemAsync(RestaurantMenuItem item, RestaurantRecipe? recipe, IReadOnlyList<RestaurantRecipeLine>? lines, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        if (item.Id == 0)
        {
            if (recipe is not null)
            {
                await db.RestaurantRecipes.AddAsync(recipe, ct);
                await db.SaveChangesAsync(ct);
                item.RecipeId = recipe.Id;
            }

            await db.RestaurantMenuItems.AddAsync(item, ct);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            var existing = await db.RestaurantMenuItems.FirstOrDefaultAsync(m => m.Id == item.Id, ct)
                ?? throw new InvalidOperationException("الصنف غير موجود");
            existing.Name = item.Name;
            existing.RestaurantMenuCategoryId = item.RestaurantMenuCategoryId;
            existing.SalePrice = item.SalePrice;
            existing.Barcode = item.Barcode;
            existing.IsActive = item.IsActive;
            existing.SortOrder = item.SortOrder;
            existing.Notes = item.Notes;
            await db.SaveChangesAsync(ct);
            item = existing;
        }

        if (recipe is not null && lines is not null && item.RecipeId.HasValue)
        {
            var oldLines = await db.RestaurantRecipeLines.Where(l => l.RestaurantRecipeId == item.RecipeId.Value).ToListAsync(ct);
            db.RestaurantRecipeLines.RemoveRange(oldLines);
            foreach (var line in lines)
            {
                line.RestaurantRecipeId = item.RecipeId.Value;
                line.Id = 0;
                await db.RestaurantRecipeLines.AddAsync(line, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return item;
    }

    public async Task DeleteMenuItemAsync(int id, string deletedBy, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var item = await db.RestaurantMenuItems.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new InvalidOperationException("الصنف غير موجود");
        item.MarkSoftDeleted(deletedBy);
        await db.SaveChangesAsync(ct);
    }

    public async Task<RestaurantRecipe?> GetRecipeForMenuItemAsync(int menuItemId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var item = await db.RestaurantMenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, ct);
        if (item?.RecipeId is null)
            return null;

        return await db.RestaurantRecipes
            .Include(r => r.Lines).ThenInclude(l => l.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == item.RecipeId.Value, ct);
    }
}
