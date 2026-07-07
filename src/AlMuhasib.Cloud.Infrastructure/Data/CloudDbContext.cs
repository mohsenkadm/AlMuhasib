using System.Linq.Expressions;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Data;

public class CloudDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public CloudDbContext(DbContextOptions<CloudDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantAccount> TenantAccounts => Set<TenantAccount>();
    public DbSet<DeveloperUser> DeveloperUsers => Set<DeveloperUser>();
    public DbSet<DeviceSubscription> DeviceSubscriptions => Set<DeviceSubscription>();
    public DbSet<SyncChangeLog> SyncChangeLogs => Set<SyncChangeLog>();

    public DbSet<CloudCategory> Categories => Set<CloudCategory>();
    public DbSet<CloudProduct> Products => Set<CloudProduct>();
    public DbSet<CloudWarehouse> Warehouses => Set<CloudWarehouse>();
    public DbSet<CloudCustomer> Customers => Set<CloudCustomer>();
    public DbSet<CloudSupplier> Suppliers => Set<CloudSupplier>();
    public DbSet<CloudCashBox> CashBoxes => Set<CloudCashBox>();
    public DbSet<CloudBankAccount> BankAccounts => Set<CloudBankAccount>();
    public DbSet<CloudInvestor> Investors => Set<CloudInvestor>();
    public DbSet<CloudExpenseType> ExpenseTypes => Set<CloudExpenseType>();
    public DbSet<CloudPrintBrandingSettings> PrintBrandingSettings => Set<CloudPrintBrandingSettings>();
    public DbSet<CloudWarehouseStock> WarehouseStocks => Set<CloudWarehouseStock>();
    public DbSet<CloudInvoice> Invoices => Set<CloudInvoice>();
    public DbSet<CloudInvoiceItem> InvoiceItems => Set<CloudInvoiceItem>();
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

            if (_tenantContext is not null)
            {
                var tenantId = Expression.Property(parameter, nameof(CloudBaseEntity.TenantId));
                var contextTenant = Expression.Property(Expression.Constant(_tenantContext), nameof(ITenantContext.TenantId));
                var hasValue = Expression.Property(contextTenant, nameof(Nullable<int>.HasValue));
                var value = Expression.Property(contextTenant, nameof(Nullable<int>.Value));
                var tenantMatch = Expression.Equal(tenantId, value);
                var tenantFilter = Expression.OrElse(Expression.Not(hasValue), tenantMatch);
                body = Expression.AndAlso(body, tenantFilter);
            }

            builder.HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
