using System.Linq.Expressions;
using System.Text.Json;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AlMuhasib.Infrastructure.Data;

public class AppDbContext : DbContext
{
    /// <summary>Fail-closed when no branch is bound for branch-scoped entities.</summary>
    public const int UnsetBranchId = -1;

    private readonly ICurrentUserService? _currentUserService;
    private readonly IBranchContext? _branchContext;

    /// <summary>يُفعّل أثناء تطبيق Pull لتجنب استبدال طوابع التحديث من السحابة.</summary>
    public bool IsApplyingSyncPull { get; set; }

    /// <summary>تعطيل فلتر الفرع مؤقتاً (مزامنة / ترحيل / صيانة).</summary>
    public bool BypassBranchFilter { get; set; }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null,
        IBranchContext? branchContext = null)
        : base(options)
    {
        _currentUserService = currentUserService;
        _branchContext = branchContext;
    }

    /// <summary>
    /// يُقيَّم لكل استعلام. عند Bypass أو وضع كل الفروع تُرجع UnsetBranchId
    /// ويُعتمد على فلتر إضافي/تجاهل الفلاتر في مسارات التقارير.
    /// </summary>
    public int CurrentBranchIdForFilter
    {
        get
        {
            if (BypassBranchFilter || IsApplyingSyncPull)
                return UnsetBranchId;
            if (_branchContext?.IsAllBranchesMode == true)
                return UnsetBranchId;
            return _branchContext?.CurrentBranchId ?? UnsetBranchId;
        }
    }

    /// <summary>true عندما يجب تعطيل مقارنة BranchId في الفلتر العام (bypass / all-branches).</summary>
    public bool SuppressBranchIdMatch =>
        BypassBranchFilter
        || IsApplyingSyncPull
        || _branchContext?.IsAllBranchesMode == true;

    // DbSets
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SalesRepresentative> SalesRepresentatives => Set<SalesRepresentative>();
    public DbSet<SalesRepCommissionRule> SalesRepCommissionRules => Set<SalesRepCommissionRule>();
    public DbSet<SalesRepCommissionEntry> SalesRepCommissionEntries => Set<SalesRepCommissionEntry>();
    public DbSet<SalesRepTarget> SalesRepTargets => Set<SalesRepTarget>();
    public DbSet<SalesRepCollection> SalesRepCollections => Set<SalesRepCollection>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<CashBox> CashBoxes => Set<CashBox>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<InstallmentPlan> InstallmentPlans => Set<InstallmentPlan>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<InvestorTransaction> InvestorTransactions => Set<InvestorTransaction>();
    public DbSet<ProfitDistribution> ProfitDistributions => Set<ProfitDistribution>();
    public DbSet<ProfitDistributionDetail> ProfitDistributionDetails => Set<ProfitDistributionDetail>();
    public DbSet<CapitalEntry> CapitalEntries => Set<CapitalEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CustomerAttachment> CustomerAttachments => Set<CustomerAttachment>();
    public DbSet<PrintBrandingSettings> PrintBrandingSettings => Set<PrintBrandingSettings>();
    public DbSet<UserTask> UserTasks => Set<UserTask>();
    public DbSet<UserNote> UserNotes => Set<UserNote>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<CloudSyncSettings> CloudSyncSettings => Set<CloudSyncSettings>();
    public DbSet<WarehouseTransfer> WarehouseTransfers => Set<WarehouseTransfer>();
    public DbSet<WarehouseTransferItem> WarehouseTransferItems => Set<WarehouseTransferItem>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<ProductSerial> ProductSerials => Set<ProductSerial>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<ProductSizeStock> ProductSizeStocks => Set<ProductSizeStock>();
    public DbSet<ProductColor> ProductColors => Set<ProductColor>();
    public DbSet<UserLoginLog> UserLoginLogs => Set<UserLoginLog>();
    public DbSet<PricingType> PricingTypes => Set<PricingType>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<EntityCustomFieldSettings> EntityCustomFieldSettings => Set<EntityCustomFieldSettings>();
    public DbSet<PackagingType> PackagingTypes => Set<PackagingType>();
    public DbSet<LoyaltySettings> LoyaltySettings => Set<LoyaltySettings>();
    public DbSet<CustomerLoyaltyAccount> CustomerLoyaltyAccounts => Set<CustomerLoyaltyAccount>();
    public DbSet<LoyaltyPointTransaction> LoyaltyPointTransactions => Set<LoyaltyPointTransaction>();
    public DbSet<ProductOffer> ProductOffers => Set<ProductOffer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Accounting-only configs (exclude Car/Hotel modules in the same assembly)
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly,
            type => type.Namespace == "AlMuhasib.Infrastructure.Data.Configurations");

        // Soft-delete filter (+ BranchId for branch-scoped entities)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeleted = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            Expression body = Expression.Equal(isDeleted, Expression.Constant(false));

            if (typeof(IBranchEntity).IsAssignableFrom(entityType.ClrType))
            {
                // (!SuppressBranchIdMatch && BranchId == CurrentBranchIdForFilter) || SuppressBranchIdMatch
                var branchId = Expression.Property(parameter, nameof(IBranchEntity.BranchId));
                var currentBranch = Expression.Property(
                    Expression.Constant(this),
                    nameof(CurrentBranchIdForFilter));
                var suppress = Expression.Property(
                    Expression.Constant(this),
                    nameof(SuppressBranchIdMatch));
                var branchMatch = Expression.Equal(branchId, currentBranch);
                var branchClause = Expression.OrElse(suppress, branchMatch);
                body = Expression.AndAlso(body, branchClause);

                modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(IBranchEntity.BranchId));
            }

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }

        // Configure BaseEntity common columns for all derived types
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

        modelBuilder.Entity<SyncState>().HasKey(s => s.EntityType);
        modelBuilder.Entity<Core.Entities.CloudSyncSettings>().HasData(new Core.Entities.CloudSyncSettings { Id = Core.Entities.CloudSyncSettings.SingletonId });
    }

    public override int SaveChanges()
    {
        ApplyBranchWriteGuards();
        return SaveChangesWithAudit();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyBranchWriteGuards();
        return SaveChangesWithAuditAsync(cancellationToken);
    }

    private void ApplyBranchWriteGuards()
    {
        if (BypassBranchFilter || IsApplyingSyncPull)
            return;

        var writeBranchId = _branchContext?.HasWriteBranchContext == true
            ? _branchContext.CurrentBranchId
            : null;

        foreach (var entry in ChangeTracker.Entries<IBranchEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.BranchId <= 0)
                {
                    if (writeBranchId is not > 0)
                        throw new InvalidOperationException(
                            "Branch context is required before creating branch-scoped data.");
                    entry.Entity.BranchId = writeBranchId.Value;
                }
                else if (writeBranchId is > 0 && entry.Entity.BranchId != writeBranchId.Value)
                {
                    throw new UnauthorizedAccessException("Cross-branch write denied.");
                }
                else if (_branchContext is not null && !_branchContext.IsBranchAllowed(entry.Entity.BranchId))
                {
                    throw new UnauthorizedAccessException("Branch not allowed for current user.");
                }
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (writeBranchId is > 0 && entry.Entity.BranchId != writeBranchId.Value)
                    throw new UnauthorizedAccessException("Cross-branch mutation denied.");
                if (_branchContext is not null
                    && !BypassBranchFilter
                    && !_branchContext.IsBranchAllowed(entry.Entity.BranchId)
                    && _branchContext.AllowedBranchIds.Count > 0)
                {
                    throw new UnauthorizedAccessException("Branch not allowed for current user.");
                }
            }
        }
    }

    private int SaveChangesWithAudit() =>
        SaveChangesWithAuditAsync(CancellationToken.None).GetAwaiter().GetResult();

    private async Task<int> SaveChangesWithAuditAsync(CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService?.Username ?? "System";
        var userId = _currentUserService?.UserId;
        var branchId = _branchContext?.CurrentBranchId;

        // Collect audit entries BEFORE applying base entity changes
        var auditEntries = new List<AuditEntry>();
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            // Don't audit AuditLog itself or Permission
            if (entry.Entity is AuditLog || entry.Entity is Permission)
                continue;

            if (IsApplyingSyncPull)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                    if (userId.HasValue)
                        auditEntries.Add(new AuditEntry(entry, AuditAction.Add, userId.Value, branchId));
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
                        auditEntries.Add(new AuditEntry(
                            entry,
                            becameDeleted ? AuditAction.Delete : AuditAction.Edit,
                            userId.Value,
                            branchId));
                    }
                    break;

                case EntityState.Deleted:
                    if (userId.HasValue)
                        auditEntries.Add(new AuditEntry(entry, AuditAction.Delete, userId.Value, branchId));
                    break;
            }
        }

        // Add audit log records
        foreach (var audit in auditEntries)
        {
            var log = audit.ToAuditLog();
            if (log is not null)
                AuditLogs.Add(log);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Temporary audit entry holder used to capture old/new values before SaveChanges.
    /// </summary>
    private sealed class AuditEntry
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly HashSet<string> _excludedProps =
        [
            nameof(BaseEntity.CreatedAt), nameof(BaseEntity.CreatedBy),
            nameof(BaseEntity.UpdatedAt), nameof(BaseEntity.UpdatedBy),
            nameof(BaseEntity.DeletedAt), nameof(BaseEntity.DeletedBy),
            nameof(BaseEntity.IsDeleted)
        ];

        public EntityEntry Entry { get; }
        public AuditAction Action { get; }
        public int UserId { get; }
        public int? BranchId { get; }

        public AuditEntry(EntityEntry entry, AuditAction action, int userId, int? branchId)
        {
            Entry = entry;
            Action = action;
            UserId = userId;
            BranchId = branchId;
        }

        public AuditLog? ToAuditLog()
        {
            var entityName = Entry.Metadata.ClrType.Name;
            var entityId = 0;
            if (Entry.Entity is BaseEntity be)
                entityId = be.Id;

            var resolvedBranchId = BranchId
                ?? (Entry.Entity is IBranchEntity beBranch ? beBranch.BranchId : 0);

            string? oldValues = null;
            string? newValues = null;

            switch (Action)
            {
                case AuditAction.Add:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in Entry.Properties)
                    {
                        if (_excludedProps.Contains(prop.Metadata.Name)) continue;
                        if (prop.Metadata.IsPrimaryKey()) continue;
                        dict[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    newValues = JsonSerializer.Serialize(dict, _jsonOptions);
                    break;
                }
                case AuditAction.Edit:
                {
                    var oldDict = new Dictionary<string, object?>();
                    var newDict = new Dictionary<string, object?>();
                    foreach (var prop in Entry.Properties)
                    {
                        if (_excludedProps.Contains(prop.Metadata.Name)) continue;
                        if (prop.Metadata.IsPrimaryKey()) continue;
                        if (!prop.IsModified) continue;
                        oldDict[prop.Metadata.Name] = prop.OriginalValue;
                        newDict[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    if (oldDict.Count == 0) return null; // no real changes
                    oldValues = JsonSerializer.Serialize(oldDict, _jsonOptions);
                    newValues = JsonSerializer.Serialize(newDict, _jsonOptions);
                    break;
                }
                case AuditAction.Delete:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in Entry.Properties)
                    {
                        if (_excludedProps.Contains(prop.Metadata.Name)) continue;
                        if (prop.Metadata.IsPrimaryKey()) continue;
                        dict[prop.Metadata.Name] = prop.OriginalValue;
                    }
                    oldValues = JsonSerializer.Serialize(dict, _jsonOptions);
                    break;
                }
            }

            return new AuditLog
            {
                UserId = UserId,
                BranchId = resolvedBranchId > 0 ? resolvedBranchId : null,
                Action = Action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = DateTime.UtcNow,
                CreatedBy = "System"
            };
        }
    }
}
