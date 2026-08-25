using System.Linq.Expressions;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Data;

public class CloudDbContext : DbContext
{
    /// <summary>
    /// Sentinel used when no tenant is bound. Fail-closed: matches no real tenant rows.
    /// Cross-tenant admin/sync paths must use IgnoreQueryFilters() with an explicit TenantId.
    /// </summary>
    public const int UnsetTenantId = -1;

    private readonly ITenantContext? _tenantContext;

    public CloudDbContext(DbContextOptions<CloudDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Evaluated per query against the current DbContext instance (EF rewrites the filter).
    /// Do not capture ITenantContext via Expression.Constant — that freezes the first scoped instance.
    /// </summary>
    public int CurrentTenantId => _tenantContext?.TenantId ?? UnsetTenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantAccount> TenantAccounts => Set<TenantAccount>();
    public DbSet<DeveloperUser> DeveloperUsers => Set<DeveloperUser>();
    public DbSet<DeviceSubscription> DeviceSubscriptions => Set<DeviceSubscription>();
    public DbSet<SyncChangeLog> SyncChangeLogs => Set<SyncChangeLog>();

    public DbSet<CloudCategory> Categories => Set<CloudCategory>();
    public DbSet<CloudProduct> Products => Set<CloudProduct>();
    public DbSet<CloudPricingType> PricingTypes => Set<CloudPricingType>();
    public DbSet<CloudProductPrice> ProductPrices => Set<CloudProductPrice>();
    public DbSet<CloudBusinessSettings> BusinessSettings => Set<CloudBusinessSettings>();
    public DbSet<CloudWarehouse> Warehouses => Set<CloudWarehouse>();
    public DbSet<CloudCustomer> Customers => Set<CloudCustomer>();
    public DbSet<CloudSupplier> Suppliers => Set<CloudSupplier>();
    public DbSet<CloudCashBox> CashBoxes => Set<CloudCashBox>();
    public DbSet<CloudBankAccount> BankAccounts => Set<CloudBankAccount>();
    public DbSet<CloudInvestor> Investors => Set<CloudInvestor>();
    public DbSet<CloudExpenseType> ExpenseTypes => Set<CloudExpenseType>();
    public DbSet<CloudPrintBrandingSettings> PrintBrandingSettings => Set<CloudPrintBrandingSettings>();
    public DbSet<CloudWarehouseStock> WarehouseStocks => Set<CloudWarehouseStock>();
    public DbSet<CloudWarehouseTransfer> WarehouseTransfers => Set<CloudWarehouseTransfer>();
    public DbSet<CloudWarehouseTransferItem> WarehouseTransferItems => Set<CloudWarehouseTransferItem>();
    public DbSet<CloudInvoice> Invoices => Set<CloudInvoice>();
    public DbSet<CloudInvoiceItem> InvoiceItems => Set<CloudInvoiceItem>();
    public DbSet<CloudProductOffer> ProductOffers => Set<CloudProductOffer>();
    public DbSet<CloudInstallmentPlan> InstallmentPlans => Set<CloudInstallmentPlan>();
    public DbSet<CloudInstallment> Installments => Set<CloudInstallment>();
    public DbSet<CloudVoucher> Vouchers => Set<CloudVoucher>();
    public DbSet<CloudExpense> Expenses => Set<CloudExpense>();
    public DbSet<CloudTransfer> Transfers => Set<CloudTransfer>();
    public DbSet<CloudInvestorTransaction> InvestorTransactions => Set<CloudInvestorTransaction>();
    public DbSet<CloudProfitDistribution> ProfitDistributions => Set<CloudProfitDistribution>();
    public DbSet<CloudProfitDistributionDetail> ProfitDistributionDetails => Set<CloudProfitDistributionDetail>();
    public DbSet<CloudCapitalEntry> CapitalEntries => Set<CloudCapitalEntry>();
    public DbSet<CloudCustomerAttachment> CustomerAttachments => Set<CloudCustomerAttachment>();
    public DbSet<CloudHotelSettings> HotelSettings => Set<CloudHotelSettings>();
    public DbSet<CloudHotelFloor> HotelFloors => Set<CloudHotelFloor>();
    public DbSet<CloudHotelRoomType> HotelRoomTypes => Set<CloudHotelRoomType>();
    public DbSet<CloudHotelRoom> HotelRooms => Set<CloudHotelRoom>();
    public DbSet<CloudHotelGuest> HotelGuests => Set<CloudHotelGuest>();
    public DbSet<CloudHotelReservation> HotelReservations => Set<CloudHotelReservation>();
    public DbSet<CloudHotelReservationCharge> HotelReservationCharges => Set<CloudHotelReservationCharge>();
    public DbSet<CloudHotelReservationPayment> HotelReservationPayments => Set<CloudHotelReservationPayment>();
    public DbSet<CloudHotelCashBox> HotelCashBoxes => Set<CloudHotelCashBox>();
    public DbSet<CloudHotelVoucher> HotelVouchers => Set<CloudHotelVoucher>();
    public DbSet<CloudHotelExpenseType> HotelExpenseTypes => Set<CloudHotelExpenseType>();
    public DbSet<CloudHotelExpense> HotelExpenses => Set<CloudHotelExpense>();
    public DbSet<CloudHotelRatePlan> HotelRatePlans => Set<CloudHotelRatePlan>();
    public DbSet<CloudHotelRatePlanSeason> HotelRatePlanSeasons => Set<CloudHotelRatePlanSeason>();
    public DbSet<CloudHotelHousekeepingTask> HotelHousekeepingTasks => Set<CloudHotelHousekeepingTask>();
    public DbSet<CloudRestaurantIngredient> RestaurantIngredients => Set<CloudRestaurantIngredient>();
    public DbSet<CloudRestaurantIngredientStock> RestaurantIngredientStocks => Set<CloudRestaurantIngredientStock>();
    public DbSet<CloudRestaurantMenuCategory> RestaurantMenuCategories => Set<CloudRestaurantMenuCategory>();
    public DbSet<CloudRestaurantRecipe> RestaurantRecipes => Set<CloudRestaurantRecipe>();
    public DbSet<CloudRestaurantMenuItem> RestaurantMenuItems => Set<CloudRestaurantMenuItem>();
    public DbSet<CloudRestaurantRecipeLine> RestaurantRecipeLines => Set<CloudRestaurantRecipeLine>();
    public DbSet<CloudRestaurantTable> RestaurantTables => Set<CloudRestaurantTable>();
    public DbSet<CloudRestaurantOrder> RestaurantOrders => Set<CloudRestaurantOrder>();
    public DbSet<CloudRestaurantOrderLine> RestaurantOrderLines => Set<CloudRestaurantOrderLine>();
    public DbSet<CloudRestaurantOrderPayment> RestaurantOrderPayments => Set<CloudRestaurantOrderPayment>();
    public DbSet<CloudRestaurantStockMovement> RestaurantStockMovements => Set<CloudRestaurantStockMovement>();
    public DbSet<CloudCarSaleContract> CarSaleContracts => Set<CloudCarSaleContract>();
    public DbSet<CloudCarContractPayment> CarContractPayments => Set<CloudCarContractPayment>();
    public DbSet<CloudCarTradeTransaction> CarTradeTransactions => Set<CloudCarTradeTransaction>();
    public DbSet<CloudCarTradePayment> CarTradePayments => Set<CloudCarTradePayment>();
    public DbSet<CloudRealEstateContract> RealEstateContracts => Set<CloudRealEstateContract>();
    public DbSet<CloudRealEstateContractPayment> RealEstateContractPayments => Set<CloudRealEstateContractPayment>();
    public DbSet<CloudRealEstateContractClause> RealEstateContractClauses => Set<CloudRealEstateContractClause>();
    public DbSet<CloudRealEstateClauseTemplate> RealEstateClauseTemplates => Set<CloudRealEstateClauseTemplate>();
    public DbSet<CloudRealEstateParty> RealEstateParties => Set<CloudRealEstateParty>();
    public DbSet<CloudRealEstateExpenseType> RealEstateExpenseTypes => Set<CloudRealEstateExpenseType>();
    public DbSet<CloudRealEstateExpense> RealEstateExpenses => Set<CloudRealEstateExpense>();
    public DbSet<CloudGoldSettings> GoldSettings => Set<CloudGoldSettings>();
    public DbSet<CloudGoldFxRate> GoldFxRates => Set<CloudGoldFxRate>();
    public DbSet<CloudGoldKarat> GoldKarats => Set<CloudGoldKarat>();
    public DbSet<CloudGoldMithqalPrice> GoldMithqalPrices => Set<CloudGoldMithqalPrice>();
    public DbSet<CloudGoldItem> GoldItems => Set<CloudGoldItem>();
    public DbSet<CloudGoldStockBalance> GoldStockBalances => Set<CloudGoldStockBalance>();
    public DbSet<CloudGoldCustomer> GoldCustomers => Set<CloudGoldCustomer>();
    public DbSet<CloudGoldSupplier> GoldSuppliers => Set<CloudGoldSupplier>();
    public DbSet<CloudGoldWarehouse> GoldWarehouses => Set<CloudGoldWarehouse>();
    public DbSet<CloudGoldExpenseType> GoldExpenseTypes => Set<CloudGoldExpenseType>();
    public DbSet<CloudGoldExpense> GoldExpenses => Set<CloudGoldExpense>();
    public DbSet<CloudGoldWarehouseTransfer> GoldWarehouseTransfers => Set<CloudGoldWarehouseTransfer>();
    public DbSet<CloudGoldCashBox> GoldCashBoxes => Set<CloudGoldCashBox>();
    public DbSet<CloudGoldInvoice> GoldInvoices => Set<CloudGoldInvoice>();
    public DbSet<CloudGoldInvoiceLine> GoldInvoiceLines => Set<CloudGoldInvoiceLine>();
    public DbSet<CloudGoldPayment> GoldPayments => Set<CloudGoldPayment>();
    public DbSet<CloudGoldVoucher> GoldVouchers => Set<CloudGoldVoucher>();
    public DbSet<CloudGoldNotification> GoldNotifications => Set<CloudGoldNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantAccount>()
            .HasIndex(a => a.Username)
            .IsUnique();

        modelBuilder.Entity<TenantAccount>()
            .HasOne(a => a.Tenant)
            .WithMany(t => t.Accounts)
            .HasForeignKey(a => a.TenantId);

        modelBuilder.Entity<DeviceSubscription>()
            .HasIndex(d => new { d.TenantId, d.PlayerId })
            .IsUnique();

        modelBuilder.Entity<CloudPricingType>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Name });
        });

        modelBuilder.Entity<CloudProduct>(e =>
        {
            e.Property(x => x.ScientificName).HasMaxLength(300);
            e.Property(x => x.UsageInstructions).HasMaxLength(2000);
            e.Property(x => x.Weight).HasPrecision(18, 4);
            e.Property(x => x.WeightUnit).HasMaxLength(20);
        });

        modelBuilder.Entity<CloudProductPrice>(e =>
        {
            e.Property(x => x.SalePrice).HasPrecision(18, 2);
            e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.ProductId, x.PricingTypeId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            e.HasOne(x => x.Product)
                .WithMany(p => p.ProductPrices)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PricingType)
                .WithMany(t => t.ProductPrices)
                .HasForeignKey(x => x.PricingTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CloudInvoiceItem>(e =>
        {
            e.HasOne(x => x.PricingType)
                .WithMany()
                .HasForeignKey(x => x.PricingTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CloudWarehouseTransfer>(e =>
        {
            e.Property(x => x.TransferNumber).HasMaxLength(50);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.FromWarehouse)
                .WithMany()
                .HasForeignKey(x => x.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToWarehouse)
                .WithMany()
                .HasForeignKey(x => x.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items)
                .WithOne(i => i.WarehouseTransfer)
                .HasForeignKey(i => i.WarehouseTransferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CloudWarehouseTransferItem>(e =>
        {
            e.Property(x => x.Quantity).HasPrecision(18, 2);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CloudCarSaleContract>(e =>
        {
            e.Property(c => c.ContractNumber).HasMaxLength(50);
            e.Property(c => c.SellerName).HasMaxLength(200);
            e.Property(c => c.SellerAddress).HasMaxLength(500);
            e.Property(c => c.SellerIdNumber).HasMaxLength(50);
            e.Property(c => c.SellerPhone).HasMaxLength(50);
            e.Property(c => c.BuyerName).HasMaxLength(200);
            e.Property(c => c.BuyerAddress).HasMaxLength(500);
            e.Property(c => c.BuyerIdNumber).HasMaxLength(50);
            e.Property(c => c.BuyerPhone).HasMaxLength(50);
            e.Property(c => c.AnnualOwnerName).HasMaxLength(200);
            e.Property(c => c.AnnualOwnerAddress).HasMaxLength(500);
            e.Property(c => c.PlateNumber).HasMaxLength(30);
            e.Property(c => c.CarType).HasMaxLength(100);
            e.Property(c => c.CarModel).HasMaxLength(100);
            e.Property(c => c.CarColor).HasMaxLength(50);
            e.Property(c => c.ChassisNumber).HasMaxLength(100);
            e.Property(c => c.CarPriceInWords).HasMaxLength(1000);
            e.Property(c => c.WitnessOneName).HasMaxLength(200);
            e.Property(c => c.WitnessTwoName).HasMaxLength(200);
            e.Property(c => c.Notes).HasMaxLength(2000);
            e.Property(c => c.CarPrice).HasPrecision(18, 2);
            e.Property(c => c.AmountReceived).HasPrecision(18, 2);
            e.Property(c => c.RemainingAmount).HasPrecision(18, 2);
            e.HasIndex(c => new { c.TenantId, c.ContractNumber });
        });

        modelBuilder.Entity<CloudCarContractPayment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.RemainingBefore).HasPrecision(18, 2);
            e.Property(p => p.RemainingAfter).HasPrecision(18, 2);
            e.Property(p => p.Notes).HasMaxLength(1000);
            e.HasOne(p => p.Contract)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CloudRealEstateContract>(e =>
        {
            e.Property(c => c.ContractNumber).HasMaxLength(50);
            e.Property(c => c.PropertyLocation).HasMaxLength(200);
            e.Property(c => c.PropertyAddress).HasMaxLength(500);
            e.Property(c => c.PropertyDescription).HasMaxLength(2000);
            e.Property(c => c.PropertyAreaSqm).HasPrecision(18, 2);
            e.Property(c => c.SellerName).HasMaxLength(200);
            e.Property(c => c.SellerAddress).HasMaxLength(500);
            e.Property(c => c.SellerIdNumber).HasMaxLength(50);
            e.Property(c => c.SellerPhone).HasMaxLength(50);
            e.Property(c => c.BuyerName).HasMaxLength(200);
            e.Property(c => c.BuyerAddress).HasMaxLength(500);
            e.Property(c => c.BuyerIdNumber).HasMaxLength(50);
            e.Property(c => c.BuyerPhone).HasMaxLength(50);
            e.Property(c => c.TotalPrice).HasPrecision(18, 2);
            e.Property(c => c.DownPayment).HasPrecision(18, 2);
            e.Property(c => c.AmountPaid).HasPrecision(18, 2);
            e.Property(c => c.RemainingAmount).HasPrecision(18, 2);
            e.Property(c => c.TotalPriceInWords).HasMaxLength(1000);
            e.Property(c => c.WitnessOneName).HasMaxLength(200);
            e.Property(c => c.WitnessTwoName).HasMaxLength(200);
            e.Property(c => c.Notes).HasMaxLength(2000);
            e.HasIndex(c => new { c.TenantId, c.ContractNumber });
        });

        modelBuilder.Entity<CloudRealEstateContractPayment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.RemainingBefore).HasPrecision(18, 2);
            e.Property(p => p.RemainingAfter).HasPrecision(18, 2);
            e.Property(p => p.Notes).HasMaxLength(1000);
            e.HasOne(p => p.Contract)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CloudRealEstateContractClause>(e =>
        {
            e.Property(c => c.Title).HasMaxLength(200);
            e.Property(c => c.Body).HasMaxLength(4000);
            e.HasOne(c => c.Contract)
                .WithMany(c => c.Clauses)
                .HasForeignKey(c => c.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CloudRealEstateClauseTemplate>(e =>
        {
            e.Property(c => c.Title).HasMaxLength(200);
            e.Property(c => c.Body).HasMaxLength(4000);
            e.HasIndex(c => new { c.TenantId, c.SortOrder });
        });

        modelBuilder.Entity<CloudRealEstateParty>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Phone).HasMaxLength(50);
            e.Property(p => p.Address).HasMaxLength(500);
            e.Property(p => p.IdNumber).HasMaxLength(50);
            e.Property(p => p.Notes).HasMaxLength(2000);
            e.HasIndex(p => new { p.TenantId, p.Name });
        });

        modelBuilder.Entity<CloudRealEstateExpenseType>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(200);
            e.Property(t => t.Notes).HasMaxLength(1000);
            e.HasIndex(t => new { t.TenantId, t.Name });
        });

        modelBuilder.Entity<CloudRealEstateExpense>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.TenantId, x.ExpenseDate });
            e.HasOne(x => x.ExpenseType)
                .WithMany()
                .HasForeignKey(x => x.ExpenseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RelatedContract)
                .WithMany()
                .HasForeignKey(x => x.RelatedContractId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CloudGoldSettings>(e =>
        {
            e.Property(s => s.MithqalGrams).HasPrecision(18, 3);
            e.Property(s => s.ScaleComPort).HasMaxLength(50);
            e.Property(s => s.ScaleStabilityThresholdGrams).HasPrecision(18, 3);
            e.Property(s => s.LowStockAlertGrams).HasPrecision(18, 3);
            e.Property(s => s.EnabledKaratsCsv).HasMaxLength(100);
        });

        modelBuilder.Entity<CloudGoldFxRate>(e =>
        {
            e.Property(r => r.UsdToIqd).HasPrecision(18, 2);
            e.Property(r => r.Notes).HasMaxLength(500);
            e.HasIndex(r => new { r.TenantId, r.RateDate });
        });

        modelBuilder.Entity<CloudGoldKarat>(e =>
        {
            e.Property(k => k.Name).HasMaxLength(50);
            e.Property(k => k.PurityFactor).HasPrecision(18, 6);
            e.HasIndex(k => new { k.TenantId, k.KaratValue });
        });

        modelBuilder.Entity<CloudGoldMithqalPrice>(e =>
        {
            e.Property(p => p.PricePerMithqal).HasPrecision(18, 2);
            e.Property(p => p.FxRateUsed).HasPrecision(18, 2);
            e.Property(p => p.Notes).HasMaxLength(500);
            e.HasIndex(p => new { p.TenantId, p.PriceDate, p.KaratValue });
        });

        modelBuilder.Entity<CloudGoldItem>(e =>
        {
            e.Property(i => i.Name).HasMaxLength(200);
            e.Property(i => i.Barcode).HasMaxLength(100);
            e.Property(i => i.Category).HasMaxLength(100);
            e.Property(i => i.Notes).HasMaxLength(2000);
            e.Property(i => i.WeightGrams).HasPrecision(18, 3);
            e.Property(i => i.SuggestedMakingCharge).HasPrecision(18, 2);
            e.Property(i => i.CostPerGram).HasPrecision(18, 2);
            e.HasIndex(i => new { i.TenantId, i.Barcode });
        });

        modelBuilder.Entity<CloudGoldStockBalance>(e =>
        {
            e.Property(s => s.GramsOnHand).HasPrecision(18, 3);
            e.Property(s => s.AverageCostPerGram).HasPrecision(18, 2);
            e.HasIndex(s => new { s.TenantId, s.WarehouseId, s.KaratValue });
            e.HasOne(s => s.Warehouse)
                .WithMany()
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CloudGoldCustomer>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(50);
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.Notes).HasMaxLength(2000);
            e.Property(c => c.CreditBalanceIqd).HasPrecision(18, 2);
            e.Property(c => c.CreditBalanceUsd).HasPrecision(18, 2);
            e.HasIndex(c => new { c.TenantId, c.Name });
        });

        modelBuilder.Entity<CloudGoldSupplier>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(50);
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.Notes).HasMaxLength(2000);
            e.Property(c => c.CreditBalanceIqd).HasPrecision(18, 2);
            e.Property(c => c.CreditBalanceUsd).HasPrecision(18, 2);
            e.HasIndex(c => new { c.TenantId, c.Name });
        });

        modelBuilder.Entity<CloudGoldWarehouse>(e =>
        {
            e.Property(w => w.Name).HasMaxLength(200);
            e.Property(w => w.Notes).HasMaxLength(2000);
            e.HasIndex(w => new { w.TenantId, w.Name });
        });

        modelBuilder.Entity<CloudGoldExpenseType>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(200);
            e.HasIndex(t => new { t.TenantId, t.Name });
        });

        modelBuilder.Entity<CloudGoldExpense>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.TenantId, x.ExpenseDate });
            e.HasOne(x => x.ExpenseType)
                .WithMany()
                .HasForeignKey(x => x.ExpenseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CashBox)
                .WithMany()
                .HasForeignKey(x => x.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CloudGoldWarehouseTransfer>(e =>
        {
            e.Property(t => t.WeightGrams).HasPrecision(18, 3);
            e.Property(t => t.Notes).HasMaxLength(2000);
            e.HasIndex(t => new { t.TenantId, t.TransferDate });
            e.HasOne(t => t.FromWarehouse)
                .WithMany()
                .HasForeignKey(t => t.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ToWarehouse)
                .WithMany()
                .HasForeignKey(t => t.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CloudGoldCashBox>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CloudGoldInvoice>(e =>
        {
            e.Property(i => i.InvoiceNumber).HasMaxLength(50);
            e.Property(i => i.FxRate).HasPrecision(18, 2);
            e.Property(i => i.ExchangeCashDifference).HasPrecision(18, 2);
            e.Property(i => i.TotalGoldValue).HasPrecision(18, 2);
            e.Property(i => i.TotalMakingCharge).HasPrecision(18, 2);
            e.Property(i => i.DiscountAmount).HasPrecision(18, 2);
            e.Property(i => i.TotalAmount).HasPrecision(18, 2);
            e.Property(i => i.TotalAmountIqd).HasPrecision(18, 2);
            e.Property(i => i.TotalAmountUsd).HasPrecision(18, 2);
            e.Property(i => i.PaidAmount).HasPrecision(18, 2);
            e.Property(i => i.RemainingAmount).HasPrecision(18, 2);
            e.Property(i => i.TotalWeightGrams).HasPrecision(18, 3);
            e.Property(i => i.Notes).HasMaxLength(2000);
            e.HasIndex(i => new { i.TenantId, i.InvoiceNumber });
            e.HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.Supplier)
                .WithMany()
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.Warehouse)
                .WithMany()
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CloudGoldInvoiceLine>(e =>
        {
            e.Property(l => l.WeightGrams).HasPrecision(18, 3);
            e.Property(l => l.MithqalPrice).HasPrecision(18, 2);
            e.Property(l => l.PricePerGram).HasPrecision(18, 2);
            e.Property(l => l.GoldValue).HasPrecision(18, 2);
            e.Property(l => l.MakingCharge).HasPrecision(18, 2);
            e.Property(l => l.LineTotal).HasPrecision(18, 2);
            e.Property(l => l.Description).HasMaxLength(500);
            e.HasOne(l => l.Invoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CloudGoldPayment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.Property(p => p.FxRate).HasPrecision(18, 2);
            e.Property(p => p.Notes).HasMaxLength(1000);
            e.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CloudGoldVoucher>(e =>
        {
            e.Property(v => v.VoucherNumber).HasMaxLength(50);
            e.Property(v => v.Amount).HasPrecision(18, 2);
            e.Property(v => v.Notes).HasMaxLength(2000);
            e.HasIndex(v => new { v.TenantId, v.VoucherNumber });
        });

        modelBuilder.Entity<CloudGoldNotification>(e =>
        {
            e.Property(n => n.Title).HasMaxLength(200);
            e.Property(n => n.Message).HasMaxLength(2000);
            e.Property(n => n.RelatedEntity).HasMaxLength(100);
            e.HasIndex(n => new { n.TenantId, n.IsRead });
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(CloudBaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.HasIndex(nameof(CloudBaseEntity.TenantId), nameof(CloudBaseEntity.SyncId)).IsUnique();
            builder.Property(nameof(CloudBaseEntity.RowVersion)).IsRowVersion();

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Property(parameter, nameof(CloudBaseEntity.IsDeleted));
            Expression body = Expression.Equal(isDeleted, Expression.Constant(false));

            // Always apply tenant isolation via DbContext.CurrentTenantId (per-request, fail-closed).
            var entityTenantId = Expression.Property(parameter, nameof(CloudBaseEntity.TenantId));
            var currentTenantId = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantId));
            var tenantMatch = Expression.Equal(entityTenantId, currentTenantId);
            body = Expression.AndAlso(body, tenantMatch);

            builder.HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    public override int SaveChanges()
    {
        ApplyTenantWriteGuards();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantWriteGuards();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// When a tenant is bound, stamp missing TenantId on inserts and block cross-tenant mutations.
    /// When unbound (admin/design-time), leave entities unchanged.
    /// </summary>
    private void ApplyTenantWriteGuards()
    {
        var tenantId = _tenantContext?.TenantId;
        if (!tenantId.HasValue || tenantId.Value <= 0)
            return;

        foreach (var entry in ChangeTracker.Entries<CloudBaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId <= 0)
                    entry.Entity.TenantId = tenantId.Value;
                else if (entry.Entity.TenantId != tenantId.Value)
                    throw new InvalidOperationException("Cross-tenant write denied.");
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (entry.Entity.TenantId != tenantId.Value)
                    throw new InvalidOperationException("Cross-tenant write denied.");
            }
        }
    }
}
