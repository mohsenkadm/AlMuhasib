using System.Text.Json;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.Configurations;
using AlMuhasib.Infrastructure.Data.Customs.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AlMuhasib.Infrastructure.Data.Customs;

public class CustomsDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public bool IsApplyingSyncPull { get; set; }

    /// <summary>Exposed for CustomsAuditWriter semantic entries.</summary>
    internal ICurrentUserService? CurrentUserService => _currentUserService;

    public CustomsDbContext(DbContextOptions<CustomsDbContext> options, ICurrentUserService? currentUserService = null)
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

    public DbSet<CustomsCustomer> CustomsCustomers => Set<CustomsCustomer>();
    public DbSet<CustomsInvoice> CustomsInvoices => Set<CustomsInvoice>();
    public DbSet<CustomsVehicle> CustomsVehicles => Set<CustomsVehicle>();
    public DbSet<CustomsExpenseType> CustomsExpenseTypes => Set<CustomsExpenseType>();
    public DbSet<CustomsExpense> CustomsExpenses => Set<CustomsExpense>();
    public DbSet<CustomsReceipt> CustomsReceipts => Set<CustomsReceipt>();
    public DbSet<CustomsReceiptAllocation> CustomsReceiptAllocations => Set<CustomsReceiptAllocation>();
    public DbSet<CustomsFxRate> CustomsFxRates => Set<CustomsFxRate>();
    public DbSet<CustomsAttachment> CustomsAttachments => Set<CustomsAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        NonAccountingEntityModel.BlockAccountingGraphDiscovery(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new NonAccountingAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new NonAccountingPrintBrandingSettingsConfiguration());

        modelBuilder.ApplyConfiguration(new CustomsCustomerConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsVehicleConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsExpenseTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsReceiptConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsReceiptAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsFxRateConfiguration());
        modelBuilder.ApplyConfiguration(new CustomsAttachmentConfiguration());

        NonAccountingEntityModel.IgnoreSharedBranchProperties(modelBuilder);

        modelBuilder.Entity<SyncState>().HasKey(s => s.EntityType);
        modelBuilder.Entity<Core.Entities.CloudSyncSettings>().HasData(
            new Core.Entities.CloudSyncSettings { Id = Core.Entities.CloudSyncSettings.SingletonId });

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService?.Username ?? "System";
        var userId = _currentUserService?.UserId;
        var auditEntries = new List<AuditEntry>();
        var addEntityLinks = new List<(AuditLog Log, BaseEntity Entity)>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            // Auto-audit skips AuditLog (no wipe path) and Permission (written via CustomsAuditWriter).
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
                        var audit = new AuditEntry(entry, AuditAction.Add, userId.Value, currentUser);
                        var log = audit.ToAuditLog();
                        if (log is not null)
                        {
                            AuditLogs.Add(log);
                            addEntityLinks.Add((log, entry.Entity));
                        }
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    if (userId.HasValue)
                    {
                        var isDeletedProp = entry.Property(nameof(BaseEntity.IsDeleted));
                        var becameDeleted = isDeletedProp.IsModified
                            && isDeletedProp.OriginalValue is false
                            && isDeletedProp.CurrentValue is true;
                        var audit = new AuditEntry(
                            entry,
                            becameDeleted ? AuditAction.Delete : AuditAction.Edit,
                            userId.Value,
                            currentUser);
                        var log = audit.ToAuditLog();
                        if (log is not null)
                            AuditLogs.Add(log);
                    }
                    break;

                case EntityState.Deleted:
                    // Financial trail: prefer soft-delete / Cancel status — hard deletes are still logged if they occur.
                    if (userId.HasValue)
                    {
                        var audit = new AuditEntry(entry, AuditAction.Delete, userId.Value, currentUser);
                        var log = audit.ToAuditLog();
                        if (log is not null)
                            AuditLogs.Add(log);
                    }
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Fix EntityId on Add audits that were captured before the database assigned keys.
        var needsSecondSave = false;
        foreach (var (log, entity) in addEntityLinks)
        {
            if (log.EntityId == 0 && entity.Id != 0)
            {
                log.EntityId = entity.Id;
                needsSecondSave = true;
            }
        }

        if (needsSecondSave)
            await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Temporary audit entry holder used to capture old/new values before SaveChanges.
    /// Mirrors <c>AppDbContext.AuditEntry</c> without BranchId (ignored on Customs AuditLogs).
    /// </summary>
    private sealed class AuditEntry
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly HashSet<string> ExcludedProps =
        [
            nameof(BaseEntity.CreatedAt), nameof(BaseEntity.CreatedBy),
            nameof(BaseEntity.UpdatedAt), nameof(BaseEntity.UpdatedBy),
            nameof(BaseEntity.DeletedAt), nameof(BaseEntity.DeletedBy),
            nameof(BaseEntity.IsDeleted),
            nameof(BaseEntity.RowVersion),
            nameof(BaseEntity.SyncId),
            "HeaderImageData",
            "FooterImageData"
        ];

        private static bool ShouldSkipProperty(PropertyEntry prop)
        {
            if (ExcludedProps.Contains(prop.Metadata.Name)) return true;
            if (prop.Metadata.IsPrimaryKey()) return true;
            var clr = Nullable.GetUnderlyingType(prop.Metadata.ClrType) ?? prop.Metadata.ClrType;
            return clr == typeof(byte[]);
        }

        public EntityEntry Entry { get; }
        public AuditAction Action { get; }
        public int UserId { get; }
        public string CreatedBy { get; }

        public AuditEntry(EntityEntry entry, AuditAction action, int userId, string createdBy)
        {
            Entry = entry;
            Action = action;
            UserId = userId;
            CreatedBy = createdBy;
        }

        public AuditLog? ToAuditLog()
        {
            var entityName = Entry.Metadata.ClrType.Name;
            var entityId = Entry.Entity is BaseEntity be ? be.Id : 0;

            string? oldValues = null;
            string? newValues = null;

            switch (Action)
            {
                case AuditAction.Add:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in Entry.Properties)
                    {
                        if (ShouldSkipProperty(prop)) continue;
                        dict[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    newValues = JsonSerializer.Serialize(dict, JsonOptions);
                    break;
                }
                case AuditAction.Edit:
                {
                    var oldDict = new Dictionary<string, object?>();
                    var newDict = new Dictionary<string, object?>();
                    foreach (var prop in Entry.Properties)
                    {
                        if (ShouldSkipProperty(prop)) continue;
                        if (!prop.IsModified) continue;
                        oldDict[prop.Metadata.Name] = prop.OriginalValue;
                        newDict[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    if (oldDict.Count == 0) return null;
                    oldValues = JsonSerializer.Serialize(oldDict, JsonOptions);
                    newValues = JsonSerializer.Serialize(newDict, JsonOptions);
                    break;
                }
                case AuditAction.Delete:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in Entry.Properties)
                    {
                        if (ShouldSkipProperty(prop)) continue;
                        dict[prop.Metadata.Name] = prop.OriginalValue ?? prop.CurrentValue;
                    }
                    oldValues = JsonSerializer.Serialize(dict, JsonOptions);
                    break;
                }
            }

            return new AuditLog
            {
                UserId = UserId,
                Action = Action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = DateTime.UtcNow,
                CreatedBy = CreatedBy
            };
        }
    }
}
