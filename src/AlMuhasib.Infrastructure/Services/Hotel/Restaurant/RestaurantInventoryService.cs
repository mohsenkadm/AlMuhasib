using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel.Restaurant;

public sealed class RestaurantInventoryService : IRestaurantInventoryService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public RestaurantInventoryService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<RestaurantIngredient>> GetIngredientsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var query = db.RestaurantIngredients.Include(i => i.Stock).AsQueryable();
        if (activeOnly)
            query = query.Where(i => i.IsActive);
        return await query.OrderBy(i => i.Name).ToListAsync(ct);
    }

    public async Task<RestaurantIngredient?> GetIngredientByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantIngredients.Include(i => i.Stock).FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<RestaurantIngredient> CreateIngredientAsync(RestaurantIngredient ingredient, decimal initialQuantity = 0, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await db.RestaurantIngredients.AddAsync(ingredient, ct);
        await db.SaveChangesAsync(ct);

        var stock = new RestaurantIngredientStock
        {
            RestaurantIngredientId = ingredient.Id,
            Quantity = initialQuantity
        };
        await db.RestaurantIngredientStocks.AddAsync(stock, ct);
        await db.SaveChangesAsync(ct);
        ingredient.Stock = stock;
        return ingredient;
    }

    public async Task<RestaurantIngredient> UpdateIngredientAsync(RestaurantIngredient ingredient, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await db.RestaurantIngredients.FirstOrDefaultAsync(i => i.Id == ingredient.Id, ct)
            ?? throw new InvalidOperationException("المكون غير موجود");

        existing.Name = ingredient.Name;
        existing.Unit = ingredient.Unit;
        existing.MinQuantity = ingredient.MinQuantity;
        existing.AverageCost = ingredient.AverageCost;
        existing.Notes = ingredient.Notes;
        existing.IsActive = ingredient.IsActive;
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteIngredientAsync(int id, string deletedBy, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var ingredient = await db.RestaurantIngredients.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new InvalidOperationException("المكون غير موجود");
        ingredient.MarkSoftDeleted(deletedBy);
        await db.SaveChangesAsync(ct);
    }

    public async Task<RestaurantIngredientStock?> GetStockAsync(int ingredientId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantIngredientStocks.FirstOrDefaultAsync(s => s.RestaurantIngredientId == ingredientId, ct);
    }

    public async Task PurchaseStockAsync(int ingredientId, decimal quantity, decimal unitCost, int? cashBoxId, string notes, CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");

        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ingredient = await db.RestaurantIngredients.FirstOrDefaultAsync(i => i.Id == ingredientId, ct)
            ?? throw new InvalidOperationException("المكون غير موجود");

        var stock = await db.RestaurantIngredientStocks.FirstOrDefaultAsync(s => s.RestaurantIngredientId == ingredientId, ct);
        if (stock is null)
        {
            stock = new RestaurantIngredientStock { RestaurantIngredientId = ingredientId, Quantity = 0 };
            await db.RestaurantIngredientStocks.AddAsync(stock, ct);
        }

        var oldQty = stock.Quantity;
        var newQty = oldQty + quantity;
        ingredient.AverageCost = newQty > 0
            ? ((oldQty * ingredient.AverageCost) + (quantity * unitCost)) / newQty
            : unitCost;
        stock.Quantity = newQty;

        await db.RestaurantStockMovements.AddAsync(new RestaurantStockMovement
        {
            RestaurantIngredientId = ingredientId,
            MovementType = RestaurantStockMovementType.Purchase,
            Quantity = quantity,
            UnitCost = unitCost,
            MovementDate = DateTime.Now,
            Notes = notes
        }, ct);

        var totalCost = quantity * unitCost;
        if (cashBoxId.HasValue && totalCost > 0)
        {
            var expenseType = await db.HotelExpenseTypes.FirstOrDefaultAsync(t => t.Name == "مشتريات مطعم", ct);
            if (expenseType is null)
            {
                expenseType = new HotelExpenseType { Name = "مشتريات مطعم", Notes = "مصاريف شراء مخزون المطعم" };
                await db.HotelExpenseTypes.AddAsync(expenseType, ct);
                await db.SaveChangesAsync(ct);
            }

            var expense = new HotelExpense
            {
                HotelExpenseTypeId = expenseType.Id,
                ExpenseDate = DateTime.Today,
                Amount = totalCost,
                HotelCashBoxId = cashBoxId,
                Description = $"شراء مخزون: {ingredient.Name}",
                Notes = notes
            };
            await db.HotelExpenses.AddAsync(expense, ct);
            await db.SaveChangesAsync(ct);

            var cashBox = await db.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == cashBoxId.Value, ct)
                ?? throw new InvalidOperationException("الصندوق غير موجود");
            if (cashBox.CurrentBalance < totalCost)
                throw new InvalidOperationException("رصيد الصندوق غير كافٍ");
            cashBox.CurrentBalance -= totalCost;

            var voucherNumber = await GetNextVoucherNumberAsync(db, HotelVoucherType.Payment, ct);
            await db.HotelVouchers.AddAsync(new HotelVoucher
            {
                VoucherNumber = voucherNumber,
                Type = HotelVoucherType.Payment,
                VoucherDate = DateTime.Today,
                Amount = totalCost,
                HotelCashBoxId = cashBoxId.Value,
                HotelExpenseId = expense.Id,
                Description = expense.Description,
                Notes = notes
            }, ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task AdjustStockAsync(int ingredientId, decimal newQuantity, string notes, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var stock = await db.RestaurantIngredientStocks.FirstOrDefaultAsync(s => s.RestaurantIngredientId == ingredientId, ct)
            ?? throw new InvalidOperationException("المخزون غير موجود");

        var diff = newQuantity - stock.Quantity;
        stock.Quantity = newQuantity;

        await db.RestaurantStockMovements.AddAsync(new RestaurantStockMovement
        {
            RestaurantIngredientId = ingredientId,
            MovementType = RestaurantStockMovementType.Adjustment,
            Quantity = Math.Abs(diff),
            UnitCost = 0,
            MovementDate = DateTime.Now,
            Notes = notes
        }, ct);

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RestaurantIngredient>> GetLowStockAlertsAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantIngredients
            .Include(i => i.Stock)
            .Where(i => i.IsActive && i.Stock != null && i.Stock.Quantity <= i.MinQuantity)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RestaurantStockMovement>> GetMovementsAsync(int? ingredientId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var query = db.RestaurantStockMovements.Include(m => m.Ingredient).AsQueryable();
        if (ingredientId.HasValue)
            query = query.Where(m => m.RestaurantIngredientId == ingredientId.Value);
        if (from.HasValue)
            query = query.Where(m => m.MovementDate >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.MovementDate <= to.Value.AddDays(1));
        return await query.OrderByDescending(m => m.MovementDate).Take(500).ToListAsync(ct);
    }

    private static async Task<string> GetNextVoucherNumberAsync(HotelDbContext db, HotelVoucherType type, CancellationToken ct)
    {
        var prefix = type == HotelVoucherType.Receipt ? "HRC" : "HPY";
        var lastVoucher = await db.HotelVouchers.IgnoreQueryFilters()
            .Where(v => v.Type == type && v.VoucherNumber.StartsWith(prefix + "-"))
            .OrderByDescending(v => v.Id)
            .Select(v => v.VoucherNumber)
            .FirstOrDefaultAsync(ct);

        var nextNum = 1;
        if (lastVoucher is not null)
        {
            var parts = lastVoucher.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{prefix}-{nextNum:D4}";
    }
}
