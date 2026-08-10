using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed class SyncIdResolver
{
    private readonly CloudDbContext _db;
    private readonly int _tenantId;
    private readonly Dictionary<string, int> _cache = new();

    public SyncIdResolver(CloudDbContext db, int tenantId)
    {
        _db = db;
        _tenantId = tenantId;
    }

    public async Task<int?> ResolveCategoryAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Categories, syncId, ct);

    public async Task<int?> ResolveProductAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Products, syncId, ct);

    public async Task<int?> ResolveWarehouseAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Warehouses, syncId, ct);

    public async Task<int?> ResolveCustomerAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Customers, syncId, ct);

    public async Task<int?> ResolveSupplierAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Suppliers, syncId, ct);

    public async Task<int?> ResolveCashBoxAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.CashBoxes, syncId, ct);

    public async Task<int?> ResolveBankAccountAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.BankAccounts, syncId, ct);

    public async Task<int?> ResolveInvestorAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Investors, syncId, ct);

    public async Task<int?> ResolveExpenseTypeAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.ExpenseTypes, syncId, ct);

    public async Task<int?> ResolvePricingTypeAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.PricingTypes, syncId, ct);

    public async Task<int?> ResolveInvoiceAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Invoices, syncId, ct);

    public async Task<int?> ResolveWarehouseTransferAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.WarehouseTransfers, syncId, ct);

    public async Task<int?> ResolveInstallmentPlanAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.InstallmentPlans, syncId, ct);

    public async Task<int?> ResolveInstallmentAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.Installments, syncId, ct);

    public async Task<int?> ResolveProfitDistributionAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.ProfitDistributions, syncId, ct);

    public async Task<int?> ResolveHotelFloorAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelFloors, syncId, ct);

    public async Task<int?> ResolveHotelRoomTypeAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelRoomTypes, syncId, ct);

    public async Task<int?> ResolveHotelRoomAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelRooms, syncId, ct);

    public async Task<int?> ResolveHotelGuestAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelGuests, syncId, ct);

    public async Task<int?> ResolveHotelReservationAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelReservations, syncId, ct);

    public async Task<int?> ResolveHotelCashBoxAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelCashBoxes, syncId, ct);

    public async Task<int?> ResolveHotelExpenseTypeAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelExpenseTypes, syncId, ct);

    public async Task<int?> ResolveHotelExpenseAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelExpenses, syncId, ct);

    public async Task<int?> ResolveHotelRatePlanAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.HotelRatePlans, syncId, ct);

    public async Task<int?> ResolveRestaurantIngredientAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RestaurantIngredients, syncId, ct);

    public async Task<int?> ResolveRestaurantMenuCategoryAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RestaurantMenuCategories, syncId, ct);

    public async Task<int?> ResolveRestaurantRecipeAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RestaurantRecipes, syncId, ct);

    public async Task<int?> ResolveRestaurantMenuItemAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RestaurantMenuItems, syncId, ct);

    public async Task<int?> ResolveRestaurantTableAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RestaurantTables, syncId, ct);

    public async Task<int?> ResolveRestaurantOrderAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RestaurantOrders, syncId, ct);

    public async Task<int?> ResolveCarSaleContractAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.CarSaleContracts, syncId, ct);

    public async Task<int?> ResolveCarTradeTransactionAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.CarTradeTransactions, syncId, ct);

    public async Task<int?> ResolveRealEstateContractAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RealEstateContracts, syncId, ct);

    public async Task<int?> ResolveRealEstateExpenseTypeAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.RealEstateExpenseTypes, syncId, ct);

    public async Task<int?> ResolveGoldCustomerAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.GoldCustomers, syncId, ct);

    public async Task<int?> ResolveGoldSupplierAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.GoldSuppliers, syncId, ct);

    public async Task<int?> ResolveGoldWarehouseAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.GoldWarehouses, syncId, ct);

    public async Task<int?> ResolveGoldExpenseTypeAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.GoldExpenseTypes, syncId, ct);

    public async Task<int?> ResolveGoldCashBoxAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.GoldCashBoxes, syncId, ct);

    public async Task<int?> ResolveGoldItemAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.GoldItems, syncId, ct);

    public async Task<int?> ResolveGoldInvoiceAsync(Guid? syncId, CancellationToken ct) =>
        await ResolveAsync(_db.GoldInvoices, syncId, ct);

    public async Task<int> ResolveTransferAccountAsync(TransferAccountType type, Guid syncId, CancellationToken ct) =>
        type switch
        {
            TransferAccountType.CashBox => await ResolveCashBoxAsync(syncId, ct) ?? 0,
            TransferAccountType.Bank => await ResolveBankAccountAsync(syncId, ct) ?? 0,
            _ => 0
        };

    private async Task<int?> ResolveAsync<T>(DbSet<T> set, Guid? syncId, CancellationToken ct)
        where T : CloudBaseEntity
    {
        if (!syncId.HasValue || syncId.Value == Guid.Empty)
            return null;

        var key = $"{typeof(T).Name}:{syncId}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var entity = await set.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.TenantId == _tenantId && e.SyncId == syncId.Value, ct);
        if (entity is null)
            return null;

        _cache[key] = entity.Id;
        return entity.Id;
    }

    public void Cache<T>(Guid syncId, int id) where T : CloudBaseEntity =>
        _cache[$"{typeof(T).Name}:{syncId}"] = id;
}
