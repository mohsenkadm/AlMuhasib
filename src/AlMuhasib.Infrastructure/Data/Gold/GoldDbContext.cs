using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.Configurations;
using AlMuhasib.Infrastructure.Data.Gold.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Data.Gold;

public class GoldDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public bool IsApplyingSyncPull { get; set; }

    public GoldDbContext(DbContextOptions<GoldDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PrintBrandingSettings> PrintBrandingSettings => Set<PrintBrandingSettings>();
    public DbSet<CloudSyncSettings> CloudSyncSettings => Set<CloudSyncSettings>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    public DbSet<GoldKarat> GoldKarats => Set<GoldKarat>();
    public DbSet<GoldSettings> GoldSettings => Set<GoldSettings>();
    public DbSet<GoldCashBox> GoldCashBoxes => Set<GoldCashBox>();
    public DbSet<GoldCustomer> GoldCustomers => Set<GoldCustomer>();
    public DbSet<GoldItem> GoldItems => Set<GoldItem>();
    public DbSet<GoldInvoice> GoldInvoices => Set<GoldInvoice>();
    public DbSet<GoldInvoiceLine> GoldInvoiceLines => Set<GoldInvoiceLine>();
    public DbSet<GoldPayment> GoldPayments => Set<GoldPayment>();
    public DbSet<GoldVoucher> GoldVouchers => Set<GoldVoucher>();
    public DbSet<GoldFxRate> GoldFxRates => Set<GoldFxRate>();
    public DbSet<GoldMithqalPrice> GoldMithqalPrices => Set<GoldMithqalPrice>();
    public DbSet<GoldStockBalance> GoldStockBalances => Set<GoldStockBalance>();
    public DbSet<GoldNotification> GoldNotifications => Set<GoldNotification>();
    public DbSet<GoldSupplier> GoldSuppliers => Set<GoldSupplier>();
    public DbSet<GoldExpenseType> GoldExpenseTypes => Set<GoldExpenseType>();
    public DbSet<GoldExpense> GoldExpenses => Set<GoldExpense>();
    public DbSet<GoldCategory> GoldCategories => Set<GoldCategory>();
    public DbSet<GoldWarehouse> GoldWarehouses => Set<GoldWarehouse>();
    public DbSet<GoldWarehouseTransfer> GoldWarehouseTransfers => Set<GoldWarehouseTransfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new PrintBrandingSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new GoldKaratConfiguration());
        modelBuilder.ApplyConfiguration(new GoldSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new GoldCashBoxConfiguration());
        modelBuilder.ApplyConfiguration(new GoldCustomerConfiguration());
        modelBuilder.ApplyConfiguration(new GoldItemConfiguration());
        modelBuilder.ApplyConfiguration(new GoldInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new GoldInvoiceLineConfiguration());
        modelBuilder.ApplyConfiguration(new GoldPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new GoldVoucherConfiguration());
        modelBuilder.ApplyConfiguration(new GoldFxRateConfiguration());
        modelBuilder.ApplyConfiguration(new GoldMithqalPriceConfiguration());
        modelBuilder.ApplyConfiguration(new GoldStockBalanceConfiguration());
        modelBuilder.ApplyConfiguration(new GoldNotificationConfiguration());
        modelBuilder.ApplyConfiguration(new GoldSupplierConfiguration());
        modelBuilder.ApplyConfiguration(new GoldExpenseTypeConfiguration());
        modelBuilder.ApplyConfiguration(new GoldExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new GoldWarehouseConfiguration());
        modelBuilder.ApplyConfiguration(new GoldWarehouseTransferConfiguration());

        modelBuilder.Entity<SyncState>().HasKey(s => s.EntityType);
        modelBuilder.Entity<Core.Entities.CloudSyncSettings>().HasData(
            new Core.Entities.CloudSyncSettings { Id = Core.Entities.CloudSyncSettings.SingletonId });

        modelBuilder.Entity<User>().Ignore(u => u.Tasks);
        modelBuilder.Entity<User>().Ignore(u => u.Notes);
        modelBuilder.Ignore<Core.Entities.UserTask>();
        modelBuilder.Ignore<Core.Entities.UserNote>();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = System.Linq.Expressions.Expression.Lambda(
                System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var builder = modelBuilder.Entity(entityType.ClrType);
            builder.Property(nameof(BaseEntity.CreatedBy)).HasMaxLength(100);
            builder.Property(nameof(BaseEntity.UpdatedBy)).HasMaxLength(100);
            builder.Property(nameof(BaseEntity.DeletedBy)).HasMaxLength(100);
            builder.HasIndex(nameof(BaseEntity.IsDeleted));
            builder.HasIndex(nameof(BaseEntity.SyncId));
            builder.Property(nameof(BaseEntity.RowVersion)).IsRowVersion();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService?.Username ?? "System";
        var userId = _currentUserService?.UserId;
        var pendingAuditLogs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is AuditLog or Permission)
                continue;

            if (IsApplyingSyncPull)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                    if (userId.HasValue)
                    {
                        pendingAuditLogs.Add(new AuditLog
                        {
                            UserId = userId.Value,
                            Action = AuditAction.Add,
                            EntityName = entry.Metadata.ClrType.Name,
                            EntityId = entry.Entity.Id,
                            Timestamp = DateTime.UtcNow,
                            CreatedBy = currentUser
                        });
                    }
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    if (userId.HasValue)
                    {
                        pendingAuditLogs.Add(new AuditLog
                        {
                            UserId = userId.Value,
                            Action = AuditAction.Edit,
                            EntityName = entry.Metadata.ClrType.Name,
                            EntityId = entry.Entity.Id,
                            Timestamp = DateTime.UtcNow,
                            CreatedBy = currentUser
                        });
                    }
                    break;
            }
        }

        foreach (var log in pendingAuditLogs)
            AuditLogs.Add(log);

        return base.SaveChangesAsync(cancellationToken);
    }
}
