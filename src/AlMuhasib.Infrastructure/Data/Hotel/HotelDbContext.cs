using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.Configurations;
using AlMuhasib.Infrastructure.Data.Hotel.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AlMuhasib.Infrastructure.Data.Hotel;

public class HotelDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public bool IsApplyingSyncPull { get; set; }

    public HotelDbContext(DbContextOptions<HotelDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PrintBrandingSettings> PrintBrandingSettings => Set<PrintBrandingSettings>();

    public DbSet<HotelSettings> HotelSettings => Set<HotelSettings>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationCharge> ReservationCharges => Set<ReservationCharge>();
    public DbSet<ReservationPayment> ReservationPayments => Set<ReservationPayment>();
    public DbSet<HotelCashBox> HotelCashBoxes => Set<HotelCashBox>();
    public DbSet<HotelVoucher> HotelVouchers => Set<HotelVoucher>();
    public DbSet<HotelExpenseType> HotelExpenseTypes => Set<HotelExpenseType>();
    public DbSet<HotelExpense> HotelExpenses => Set<HotelExpense>();
    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<RatePlanSeason> RatePlanSeasons => Set<RatePlanSeason>();
    public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new PrintBrandingSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new HotelSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new FloorConfiguration());
        modelBuilder.ApplyConfiguration(new RoomTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RoomConfiguration());
        modelBuilder.ApplyConfiguration(new GuestConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationChargeConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new HotelCashBoxConfiguration());
        modelBuilder.ApplyConfiguration(new HotelVoucherConfiguration());
        modelBuilder.ApplyConfiguration(new HotelExpenseTypeConfiguration());
        modelBuilder.ApplyConfiguration(new HotelExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new RatePlanConfiguration());
        modelBuilder.ApplyConfiguration(new RatePlanSeasonConfiguration());
        modelBuilder.ApplyConfiguration(new HousekeepingTaskConfiguration());

        modelBuilder.Entity<SyncState>().HasKey(s => s.EntityType);

        modelBuilder.Entity<User>().Ignore(u => u.Tasks);
        modelBuilder.Entity<User>().Ignore(u => u.Notes);
        modelBuilder.Ignore<UserTask>();
        modelBuilder.Ignore<UserNote>();

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
