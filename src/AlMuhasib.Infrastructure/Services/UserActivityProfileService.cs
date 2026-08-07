using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class UserActivityProfileService : IUserActivityProfileService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ISupervisoryReportService _supervisory;

    public UserActivityProfileService(
        IDbContextFactory<AppDbContext> contextFactory,
        ISupervisoryReportService supervisory)
    {
        _contextFactory = contextFactory;
        _supervisory = supervisory;
    }

    public async Task<UserActivityProfileInfo?> GetUserInfoAsync(int userId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var user = await ctx.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user is null) return null;

        var lastLogin = await ctx.UserLoginLogs.AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.LoginAt)
            .FirstOrDefaultAsync();

        return new UserActivityProfileInfo
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleDisplay = user.Role == UserRole.Admin ? "مدير النظام" : "مستخدم",
            IsActive = user.IsActive,
            LastLoginAt = lastLogin?.LoginAt,
            LastLoginMachine = lastLogin?.MachineName
        };
    }

    public async Task<UserActivityStats> GetStatsAsync(string username, DateTime? from, DateTime? to)
    {
        var filter = new SupervisoryQueryFilter
        {
            FromDate = from,
            ToDate = to,
            DeletedBy = username
        };

        var mods = await _supervisory.GetInvoiceModificationsAsync(filter, 1, 1);
        var delInvoices = await _supervisory.GetDeletedInvoicesAsync(filter, 1, 1);
        var delVouchers = await _supervisory.GetDeletedVouchersAsync(filter, 1, 1);
        var delProducts = await _supervisory.GetDeletedProductsAsync(filter, 1, 1);
        var delCustomers = await _supervisory.GetDeletedCustomersAsync(filter, 1, 1);
        var delSuppliers = await _supervisory.GetDeletedSuppliersAsync(filter, 1, 1);
        var delExpenses = await _supervisory.GetDeletedExpensesAsync(filter, 1, 1);

        var deletedTotal = delInvoices.TotalCount + delVouchers.TotalCount + delProducts.TotalCount
                           + delCustomers.TotalCount + delSuppliers.TotalCount + delExpenses.TotalCount;

        return new UserActivityStats
        {
            InvoiceModificationsCount = mods.TotalCount,
            DeletedInvoicesCount = delInvoices.TotalCount,
            DeletedRecordsCount = deletedTotal
        };
    }

    public Task<(IReadOnlyList<EntityChangeRow> Items, int TotalCount)> GetInvoiceModificationsAsync(
        string username, DateTime? from, DateTime? to, string? search, int page, int pageSize)
    {
        var filter = new SupervisoryQueryFilter
        {
            FromDate = from,
            ToDate = to,
            DeletedBy = username,
            SearchTerm = search
        };
        return _supervisory.GetInvoiceModificationsAsync(filter, page, pageSize);
    }

    public async Task<(IReadOnlyList<UserDeletedActivityRow> Items, int TotalCount)> GetDeletedActivitiesAsync(
        string username, DateTime? from, DateTime? to, string? search, string? entityKind, int page, int pageSize)
    {
        var filter = new SupervisoryQueryFilter
        {
            FromDate = from,
            ToDate = to,
            DeletedBy = username,
            SearchTerm = search
        };

        var all = new List<UserDeletedActivityRow>();

        async Task AddInvoices()
        {
            // Load enough pages to merge; for profile we fetch a capped set then page in-memory when mixing kinds.
            var (items, _) = await _supervisory.GetDeletedInvoicesAsync(filter, 1, 500);
            foreach (var i in items)
            {
                all.Add(new UserDeletedActivityRow
                {
                    EntityKind = "Invoice",
                    EntityKindDisplay = "فاتورة",
                    EntityId = i.Id,
                    Title = i.InvoiceNumber,
                    Subtitle = $"{i.InvoiceTypeDisplay} — {i.PartyName}",
                    Amount = i.NetAmount,
                    EntityDate = i.InvoiceDate,
                    DeletedAt = i.DeletedAt,
                    DeletedBy = i.DeletedBy,
                    DetailsSummary = i.DetailsSummary,
                    InvoiceType = i.InvoiceType
                });
            }
        }

        async Task AddVouchers()
        {
            var (items, _) = await _supervisory.GetDeletedVouchersAsync(filter, 1, 500);
            foreach (var v in items)
            {
                all.Add(new UserDeletedActivityRow
                {
                    EntityKind = "Voucher",
                    EntityKindDisplay = "سند",
                    EntityId = v.Id,
                    Title = v.VoucherNumber,
                    Subtitle = $"{v.VoucherTypeDisplay} — {v.PartyName}",
                    Amount = v.Amount,
                    EntityDate = v.VoucherDate,
                    DeletedAt = v.DeletedAt,
                    DeletedBy = v.DeletedBy,
                    DetailsSummary = v.DetailsSummary
                });
            }
        }

        async Task AddProducts()
        {
            var (items, _) = await _supervisory.GetDeletedProductsAsync(filter, 1, 500);
            foreach (var p in items)
            {
                all.Add(new UserDeletedActivityRow
                {
                    EntityKind = "Product",
                    EntityKindDisplay = "منتج",
                    EntityId = p.Id,
                    Title = p.Name,
                    Subtitle = p.CategoryName,
                    DeletedAt = p.DeletedAt,
                    DeletedBy = p.DeletedBy,
                    DetailsSummary = p.DetailsSummary
                });
            }
        }

        async Task AddCustomers()
        {
            var (items, _) = await _supervisory.GetDeletedCustomersAsync(filter, 1, 500);
            foreach (var c in items)
            {
                all.Add(new UserDeletedActivityRow
                {
                    EntityKind = "Customer",
                    EntityKindDisplay = "عميل",
                    EntityId = c.Id,
                    Title = c.Name,
                    Subtitle = c.Phone ?? "—",
                    DeletedAt = c.DeletedAt,
                    DeletedBy = c.DeletedBy,
                    DetailsSummary = c.DetailsSummary
                });
            }
        }

        async Task AddSuppliers()
        {
            var (items, _) = await _supervisory.GetDeletedSuppliersAsync(filter, 1, 500);
            foreach (var s in items)
            {
                all.Add(new UserDeletedActivityRow
                {
                    EntityKind = "Supplier",
                    EntityKindDisplay = "مورد",
                    EntityId = s.Id,
                    Title = s.Name,
                    Subtitle = s.Phone ?? "—",
                    DeletedAt = s.DeletedAt,
                    DeletedBy = s.DeletedBy,
                    DetailsSummary = s.DetailsSummary
                });
            }
        }

        async Task AddExpenses()
        {
            var (items, _) = await _supervisory.GetDeletedExpensesAsync(filter, 1, 500);
            foreach (var e in items)
            {
                all.Add(new UserDeletedActivityRow
                {
                    EntityKind = "Expense",
                    EntityKindDisplay = "مصروف",
                    EntityId = e.Id,
                    Title = e.ExpenseTypeName,
                    Subtitle = e.CashBoxName,
                    Amount = e.Amount,
                    EntityDate = e.ExpenseDate,
                    DeletedAt = e.DeletedAt,
                    DeletedBy = e.DeletedBy,
                    DetailsSummary = e.DetailsSummary
                });
            }
        }

        var kind = string.IsNullOrWhiteSpace(entityKind) || entityKind == "الكل" ? null : entityKind;
        if (kind is null || kind == "Invoice") await AddInvoices();
        if (kind is null || kind == "Voucher") await AddVouchers();
        if (kind is null || kind == "Product") await AddProducts();
        if (kind is null || kind == "Customer") await AddCustomers();
        if (kind is null || kind == "Supplier") await AddSuppliers();
        if (kind is null || kind == "Expense") await AddExpenses();

        var ordered = all.OrderByDescending(x => x.DeletedAt ?? DateTime.MinValue).ThenByDescending(x => x.EntityId).ToList();
        var total = ordered.Count;
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (pageItems, total);
    }

    public async Task<Invoice?> GetInvoiceIncludingDeletedAsync(int invoiceId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.Invoices
            .IgnoreQueryFilters()
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Driver)
            .Include(i => i.Warehouse)
            .Include(i => i.CashBox)
            .Include(i => i.InstallmentPlans)
                .ThenInclude(p => p.Installments)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }
}
