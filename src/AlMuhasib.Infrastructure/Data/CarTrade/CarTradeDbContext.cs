using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.Configurations;
using AlMuhasib.Infrastructure.Data.CarTrade.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AlMuhasib.Infrastructure.Data.CarTrade;

public class CarTradeDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public bool IsApplyingSyncPull { get; set; }

    public CarTradeDbContext(DbContextOptions<CarTradeDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PrintBrandingSettings> PrintBrandingSettings => Set<PrintBrandingSettings>();
    public DbSet<CarTradeTransaction> CarTradeTransactions => Set<CarTradeTransaction>();
    public DbSet<CarTradePayment> CarTradePayments => Set<CarTradePayment>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new PrintBrandingSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new CarTradeTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CarTradePaymentConfiguration());

        modelBuilder.Entity<SyncState>().HasKey(s => s.EntityType);

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
