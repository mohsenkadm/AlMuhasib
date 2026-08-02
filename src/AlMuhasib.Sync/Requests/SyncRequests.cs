using AlMuhasib.Sync.Dtos;

namespace AlMuhasib.Sync.Requests;

public sealed class SyncDataBundle
{
    public List<CategorySyncDto> Categories { get; set; } = [];
    public List<ProductSyncDto> Products { get; set; } = [];
    public List<PricingTypeSyncDto> PricingTypes { get; set; } = [];
    public List<ProductPriceSyncDto> ProductPrices { get; set; } = [];
    public List<BusinessSettingsSyncDto> BusinessSettings { get; set; } = [];
    public List<WarehouseSyncDto> Warehouses { get; set; } = [];
    public List<CustomerSyncDto> Customers { get; set; } = [];
    public List<SupplierSyncDto> Suppliers { get; set; } = [];
    public List<CashBoxSyncDto> CashBoxes { get; set; } = [];
    public List<BankAccountSyncDto> BankAccounts { get; set; } = [];
    public List<InvestorSyncDto> Investors { get; set; } = [];
    public List<ExpenseTypeSyncDto> ExpenseTypes { get; set; } = [];
    public List<PrintBrandingSettingsSyncDto> PrintBrandingSettings { get; set; } = [];
    public List<WarehouseStockSyncDto> WarehouseStocks { get; set; } = [];
    public List<WarehouseTransferSyncDto> WarehouseTransfers { get; set; } = [];
    public List<WarehouseTransferItemSyncDto> WarehouseTransferItems { get; set; } = [];
    public List<InvoiceSyncDto> Invoices { get; set; } = [];
    public List<InvoiceItemSyncDto> InvoiceItems { get; set; } = [];
    public List<InstallmentPlanSyncDto> InstallmentPlans { get; set; } = [];
    public List<InstallmentSyncDto> Installments { get; set; } = [];
    public List<VoucherSyncDto> Vouchers { get; set; } = [];
    public List<ExpenseSyncDto> Expenses { get; set; } = [];
    public List<TransferSyncDto> Transfers { get; set; } = [];
    public List<InvestorTransactionSyncDto> InvestorTransactions { get; set; } = [];
    public List<ProfitDistributionSyncDto> ProfitDistributions { get; set; } = [];
    public List<ProfitDistributionDetailSyncDto> ProfitDistributionDetails { get; set; } = [];
    public List<CapitalEntrySyncDto> CapitalEntries { get; set; } = [];
    public List<CustomerAttachmentSyncDto> CustomerAttachments { get; set; } = [];

    public List<HotelSettingsSyncDto> HotelSettings { get; set; } = [];
    public List<HotelFloorSyncDto> HotelFloors { get; set; } = [];
    public List<HotelRoomTypeSyncDto> HotelRoomTypes { get; set; } = [];
    public List<HotelRoomSyncDto> HotelRooms { get; set; } = [];
    public List<HotelGuestSyncDto> HotelGuests { get; set; } = [];
    public List<HotelReservationSyncDto> HotelReservations { get; set; } = [];
    public List<HotelReservationChargeSyncDto> HotelReservationCharges { get; set; } = [];
    public List<HotelReservationPaymentSyncDto> HotelReservationPayments { get; set; } = [];
    public List<HotelCashBoxSyncDto> HotelCashBoxes { get; set; } = [];
    public List<HotelVoucherSyncDto> HotelVouchers { get; set; } = [];
    public List<HotelExpenseTypeSyncDto> HotelExpenseTypes { get; set; } = [];
    public List<HotelExpenseSyncDto> HotelExpenses { get; set; } = [];
    public List<HotelRatePlanSyncDto> HotelRatePlans { get; set; } = [];
    public List<HotelRatePlanSeasonSyncDto> HotelRatePlanSeasons { get; set; } = [];
    public List<HotelHousekeepingTaskSyncDto> HotelHousekeepingTasks { get; set; } = [];

    public List<RestaurantIngredientSyncDto> RestaurantIngredients { get; set; } = [];
    public List<RestaurantIngredientStockSyncDto> RestaurantIngredientStocks { get; set; } = [];
    public List<RestaurantMenuCategorySyncDto> RestaurantMenuCategories { get; set; } = [];
    public List<RestaurantRecipeSyncDto> RestaurantRecipes { get; set; } = [];
    public List<RestaurantMenuItemSyncDto> RestaurantMenuItems { get; set; } = [];
    public List<RestaurantRecipeLineSyncDto> RestaurantRecipeLines { get; set; } = [];
    public List<RestaurantTableSyncDto> RestaurantTables { get; set; } = [];
    public List<RestaurantOrderSyncDto> RestaurantOrders { get; set; } = [];
    public List<RestaurantOrderLineSyncDto> RestaurantOrderLines { get; set; } = [];
    public List<RestaurantOrderPaymentSyncDto> RestaurantOrderPayments { get; set; } = [];
    public List<RestaurantStockMovementSyncDto> RestaurantStockMovements { get; set; } = [];

    public List<CarSaleContractSyncDto> CarSaleContracts { get; set; } = [];
    public List<CarContractPaymentSyncDto> CarContractPayments { get; set; } = [];

    public List<CarTradeTransactionSyncDto> CarTradeTransactions { get; set; } = [];
    public List<CarTradePaymentSyncDto> CarTradePayments { get; set; } = [];

    public List<RealEstateContractSyncDto> RealEstateContracts { get; set; } = [];
    public List<RealEstateContractPaymentSyncDto> RealEstateContractPayments { get; set; } = [];
    public List<RealEstateContractClauseSyncDto> RealEstateContractClauses { get; set; } = [];
    public List<RealEstateClauseTemplateSyncDto> RealEstateClauseTemplates { get; set; } = [];
    public List<RealEstatePartySyncDto> RealEstateParties { get; set; } = [];
    public List<RealEstateExpenseTypeSyncDto> RealEstateExpenseTypes { get; set; } = [];
    public List<RealEstateExpenseSyncDto> RealEstateExpenses { get; set; } = [];

    public List<GoldSettingsSyncDto> GoldSettings { get; set; } = [];
    public List<GoldFxRateSyncDto> GoldFxRates { get; set; } = [];
    public List<GoldKaratSyncDto> GoldKarats { get; set; } = [];
    public List<GoldMithqalPriceSyncDto> GoldMithqalPrices { get; set; } = [];
    public List<GoldItemSyncDto> GoldItems { get; set; } = [];
    public List<GoldStockBalanceSyncDto> GoldStockBalances { get; set; } = [];
    public List<GoldCustomerSyncDto> GoldCustomers { get; set; } = [];
    public List<GoldCashBoxSyncDto> GoldCashBoxes { get; set; } = [];
    public List<GoldInvoiceSyncDto> GoldInvoices { get; set; } = [];
    public List<GoldInvoiceLineSyncDto> GoldInvoiceLines { get; set; } = [];
    public List<GoldPaymentSyncDto> GoldPayments { get; set; } = [];
    public List<GoldVoucherSyncDto> GoldVouchers { get; set; } = [];
    public List<GoldNotificationSyncDto> GoldNotifications { get; set; } = [];
}

public sealed class SyncPushRequest
{
    public SyncDataBundle Data { get; set; } = new();
}

public sealed class SyncPullRequest
{
    public DateTime? Since { get; set; }
    public string? Cursor { get; set; }
}
