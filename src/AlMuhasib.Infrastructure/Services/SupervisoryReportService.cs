using System.Text.Json;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class SupervisoryReportService : ISupervisoryReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public SupervisoryReportService(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<(IReadOnlyList<DeletedInvoiceRow> Items, int TotalCount)> GetDeletedInvoicesAsync(
        SupervisoryQueryFilter filter, int page, int pageSize, InvoiceType? invoiceType = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Where(i => i.IsDeleted);

        if (invoiceType.HasValue)
            query = query.Where(i => i.InvoiceType == invoiceType.Value);

        query = ApplyDeletedFilters(query, filter, i =>
            i.InvoiceNumber.Contains(filter.SearchTerm!) ||
            (i.Customer != null && i.Customer.Name.Contains(filter.SearchTerm!)) ||
            (i.Supplier != null && i.Supplier.Name.Contains(filter.SearchTerm!)) ||
            (i.Notes != null && i.Notes.Contains(filter.SearchTerm!)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.DeletedAt ?? i.UpdatedAt)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = items.Select(i =>
        {
            var displayNumber = StripDeleteSuffix(i.InvoiceNumber);
            var party = i.Customer?.Name ?? i.Supplier?.Name ?? "—";
            return new DeletedInvoiceRow
            {
                Id = i.Id,
                InvoiceNumber = displayNumber,
                InvoiceType = i.InvoiceType,
                InvoiceTypeDisplay = InvoiceTypeDisplay(i.InvoiceType),
                PartyName = party,
                WarehouseName = i.Warehouse?.Name ?? "—",
                NetAmount = i.NetAmount,
                InvoiceDate = i.Date,
                DeletedAt = i.DeletedAt,
                DeletedBy = i.DeletedBy ?? "—",
                Notes = i.Notes,
                DetailsSummary =
                    $"فاتورة {InvoiceTypeDisplay(i.InvoiceType)} رقم {displayNumber} | الطرف: {party} | المبلغ: {i.NetAmount:N0} | المخزن: {i.Warehouse?.Name ?? "—"}"
            };
        }).ToList();

        return (rows, totalCount);
    }

    public async Task<(IReadOnlyList<DeletedVoucherRow> Items, int TotalCount)> GetDeletedVouchersAsync(
        SupervisoryQueryFilter filter, int page, int pageSize, VoucherType? voucherType = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Vouchers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(v => v.Customer)
            .Include(v => v.Investor)
            .Include(v => v.CashBox)
            .Include(v => v.BankAccount)
            .Where(v => v.IsDeleted);

        if (voucherType.HasValue)
            query = query.Where(v => v.VoucherType == voucherType.Value);

        query = ApplyDeletedFilters(query, filter, v =>
            v.VoucherNumber.Contains(filter.SearchTerm!) ||
            (v.Customer != null && v.Customer.Name.Contains(filter.SearchTerm!)) ||
            (v.Investor != null && v.Investor.Name.Contains(filter.SearchTerm!)) ||
            (v.Notes != null && v.Notes.Contains(filter.SearchTerm!)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.DeletedAt ?? v.UpdatedAt)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = items.Select(v =>
        {
            var party = v.Customer?.Name ?? v.Investor?.Name ?? "—";
            return new DeletedVoucherRow
            {
                Id = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType,
                VoucherTypeDisplay = VoucherTypeDisplay(v.VoucherType),
                Amount = v.Amount,
                PartyName = party,
                CashBoxName = v.CashBox?.Name ?? "—",
                VoucherDate = v.Date,
                DeletedAt = v.DeletedAt,
                DeletedBy = v.DeletedBy ?? "—",
                Notes = v.Notes,
                DetailsSummary =
                    $"سند {VoucherTypeDisplay(v.VoucherType)} رقم {v.VoucherNumber} | الطرف: {party} | المبلغ: {v.Amount:N0} | القاصة: {v.CashBox?.Name ?? "—"}"
            };
        }).ToList();

        return (rows, totalCount);
    }

    public async Task<(IReadOnlyList<DeletedProductRow> Items, int TotalCount)> GetDeletedProductsAsync(
        SupervisoryQueryFilter filter, int page, int pageSize)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsDeleted);

        query = ApplyDeletedFilters(query, filter, p =>
            p.Name.Contains(filter.SearchTerm!) ||
            (p.Barcode != null && p.Barcode.Contains(filter.SearchTerm!)) ||
            (p.ScientificName != null && p.ScientificName.Contains(filter.SearchTerm!)) ||
            (p.Description != null && p.Description.Contains(filter.SearchTerm!)) ||
            (p.Category != null && p.Category.Name.Contains(filter.SearchTerm!)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.DeletedAt ?? p.UpdatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = items.Select(p => new DeletedProductRow
        {
            Id = p.Id,
            Name = p.Name,
            Barcode = p.Barcode,
            CategoryName = p.Category?.Name ?? "—",
            Description = p.Description,
            DeletedAt = p.DeletedAt,
            DeletedBy = p.DeletedBy ?? "—",
            DetailsSummary = $"منتج: {p.Name} | التصنيف: {p.Category?.Name ?? "—"} | الباركود: {p.Barcode ?? "—"}"
        }).ToList();

        return (rows, totalCount);
    }

    public async Task<(IReadOnlyList<DeletedCustomerRow> Items, int TotalCount)> GetDeletedCustomersAsync(
        SupervisoryQueryFilter filter, int page, int pageSize)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.IsDeleted);

        query = ApplyDeletedFilters(query, filter, c =>
            c.Name.Contains(filter.SearchTerm!) ||
            (c.Phone != null && c.Phone.Contains(filter.SearchTerm!)) ||
            (c.FileNumber != null && c.FileNumber.Contains(filter.SearchTerm!)) ||
            (c.Address != null && c.Address.Contains(filter.SearchTerm!)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.DeletedAt ?? c.UpdatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = items.Select(c => new DeletedCustomerRow
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Address = c.Address,
            FileNumber = c.FileNumber,
            DeletedAt = c.DeletedAt,
            DeletedBy = c.DeletedBy ?? "—",
            DetailsSummary = $"عميل: {c.Name} | الهاتف: {c.Phone ?? "—"} | الملف: {c.FileNumber ?? "—"} | العنوان: {c.Address ?? "—"}"
        }).ToList();

        return (rows, totalCount);
    }

    public async Task<(IReadOnlyList<DeletedSupplierRow> Items, int TotalCount)> GetDeletedSuppliersAsync(
        SupervisoryQueryFilter filter, int page, int pageSize)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Suppliers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.IsDeleted);

        query = ApplyDeletedFilters(query, filter, s =>
            s.Name.Contains(filter.SearchTerm!) ||
            (s.Phone != null && s.Phone.Contains(filter.SearchTerm!)) ||
            (s.Address != null && s.Address.Contains(filter.SearchTerm!)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.DeletedAt ?? s.UpdatedAt)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = items.Select(s => new DeletedSupplierRow
        {
            Id = s.Id,
            Name = s.Name,
            Phone = s.Phone,
            Address = s.Address,
            DeletedAt = s.DeletedAt,
            DeletedBy = s.DeletedBy ?? "—",
            DetailsSummary = $"مورد: {s.Name} | الهاتف: {s.Phone ?? "—"} | العنوان: {s.Address ?? "—"}"
        }).ToList();

        return (rows, totalCount);
    }

    public async Task<(IReadOnlyList<DeletedExpenseRow> Items, int TotalCount)> GetDeletedExpensesAsync(
        SupervisoryQueryFilter filter, int page, int pageSize)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Expenses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(e => e.ExpenseType)
            .Include(e => e.CashBox)
            .Where(e => e.IsDeleted);

        query = ApplyDeletedFilters(query, filter, e =>
            (e.ExpenseType != null && e.ExpenseType.Name.Contains(filter.SearchTerm!)) ||
            (e.CashBox != null && e.CashBox.Name.Contains(filter.SearchTerm!)) ||
            (e.Notes != null && e.Notes.Contains(filter.SearchTerm!)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.DeletedAt ?? e.UpdatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = items.Select(e => new DeletedExpenseRow
        {
            Id = e.Id,
            ExpenseTypeName = e.ExpenseType?.Name ?? "—",
            Amount = e.Amount,
            CashBoxName = e.CashBox?.Name ?? "—",
            ExpenseDate = e.Date,
            Notes = e.Notes,
            DeletedAt = e.DeletedAt,
            DeletedBy = e.DeletedBy ?? "—",
            DetailsSummary =
                $"مصروف: {e.ExpenseType?.Name ?? "—"} | المبلغ: {e.Amount:N0} | القاصة: {e.CashBox?.Name ?? "—"} | ملاحظات: {e.Notes ?? "—"}"
        }).ToList();

        return (rows, totalCount);
    }

    public async Task<(IReadOnlyList<EntityChangeRow> Items, int TotalCount)> GetInvoiceModificationsAsync(
        SupervisoryQueryFilter filter, int page, int pageSize)
        => await GetEntityChangesAsync("InvoiceRevision", filter, page, pageSize,
            extractKey: ParseInvoiceKey,
            fieldLabels: InvoiceFieldLabels);

    public async Task<(IReadOnlyList<EntityChangeRow> Items, int TotalCount)> GetProductModificationsAsync(
        SupervisoryQueryFilter filter, int page, int pageSize)
        => await GetEntityChangesAsync("Product", filter, page, pageSize,
            extractKey: ParseProductKey,
            fieldLabels: ProductFieldLabels,
            action: AuditAction.Edit);

    public async Task<IReadOnlyList<string>> GetDeletedByUsernamesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var fromInvoices = await context.Invoices.IgnoreQueryFilters().Where(x => x.IsDeleted && x.DeletedBy != null).Select(x => x.DeletedBy!).Distinct().ToListAsync();
        var fromVouchers = await context.Vouchers.IgnoreQueryFilters().Where(x => x.IsDeleted && x.DeletedBy != null).Select(x => x.DeletedBy!).Distinct().ToListAsync();
        var fromProducts = await context.Products.IgnoreQueryFilters().Where(x => x.IsDeleted && x.DeletedBy != null).Select(x => x.DeletedBy!).Distinct().ToListAsync();
        var fromCustomers = await context.Customers.IgnoreQueryFilters().Where(x => x.IsDeleted && x.DeletedBy != null).Select(x => x.DeletedBy!).Distinct().ToListAsync();
        var fromSuppliers = await context.Suppliers.IgnoreQueryFilters().Where(x => x.IsDeleted && x.DeletedBy != null).Select(x => x.DeletedBy!).Distinct().ToListAsync();
        var fromExpenses = await context.Expenses.IgnoreQueryFilters().Where(x => x.IsDeleted && x.DeletedBy != null).Select(x => x.DeletedBy!).Distinct().ToListAsync();

        return fromInvoices
            .Concat(fromVouchers)
            .Concat(fromProducts)
            .Concat(fromCustomers)
            .Concat(fromSuppliers)
            .Concat(fromExpenses)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetModifierUsernamesAsync(string entityName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.EntityName == entityName && a.Action == AuditAction.Edit)
            .Select(a => a.User.FullName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    private async Task<(IReadOnlyList<EntityChangeRow> Items, int TotalCount)> GetEntityChangesAsync(
        string entityName,
        SupervisoryQueryFilter filter,
        int page,
        int pageSize,
        Func<string?, string?, int, (string Key, string Title, string Summary)> extractKey,
        IReadOnlyDictionary<string, string> fieldLabels,
        AuditAction action = AuditAction.Edit)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.EntityName == entityName && a.Action == action);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.Timestamp >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(a => a.Timestamp < filter.ToDate.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(filter.DeletedBy))
            query = query.Where(a => a.User.FullName == filter.DeletedBy || a.User.Username == filter.DeletedBy || a.CreatedBy == filter.DeletedBy);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(a =>
                (a.OldValues != null && a.OldValues.Contains(term)) ||
                (a.NewValues != null && a.NewValues.Contains(term)) ||
                a.User.FullName.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var rows = items.Select(a =>
        {
            var (key, title, summary) = extractKey(a.OldValues, a.NewValues, a.EntityId);
            var diffs = BuildDiffs(a.OldValues, a.NewValues, fieldLabels);
            if (string.IsNullOrWhiteSpace(summary) && diffs.Count > 0)
                summary = string.Join(" | ", diffs.Select(d => $"{d.Field}: {d.OldValue ?? "—"} ← {d.NewValue ?? "—"}"));

            return new EntityChangeRow
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                ModifiedBy = a.User?.FullName ?? a.CreatedBy ?? "—",
                EntityId = a.EntityId,
                EntityKey = key,
                EntityTitle = title,
                ChangeSummary = summary,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                Diffs = diffs
            };
        }).ToList();

        return (rows, totalCount);
    }

    private static IQueryable<T> ApplyDeletedFilters<T>(
        IQueryable<T> query,
        SupervisoryQueryFilter filter,
        System.Linq.Expressions.Expression<Func<T, bool>> searchPredicate)
        where T : BaseEntity
    {
        if (filter.FromDate.HasValue)
            query = query.Where(e => e.DeletedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
        {
            var to = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(e => e.DeletedAt < to);
        }
        if (!string.IsNullOrWhiteSpace(filter.DeletedBy))
            query = query.Where(e => e.DeletedBy == filter.DeletedBy);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(searchPredicate);
        return query;
    }

    private static List<ChangeFieldDiff> BuildDiffs(
        string? oldJson,
        string? newJson,
        IReadOnlyDictionary<string, string> fieldLabels)
    {
        var oldDict = TryParseDict(oldJson);
        var newDict = TryParseDict(newJson);
        if (oldDict.Count == 0 && newDict.Count == 0)
            return [];

        var keys = oldDict.Keys.Union(newDict.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        var diffs = new List<ChangeFieldDiff>();
        foreach (var key in keys)
        {
            oldDict.TryGetValue(key, out var oldVal);
            newDict.TryGetValue(key, out var newVal);
            var oldText = FormatValue(oldVal);
            var newText = FormatValue(newVal);
            if (string.Equals(oldText, newText, StringComparison.Ordinal))
                continue;

            diffs.Add(new ChangeFieldDiff
            {
                Field = fieldLabels.TryGetValue(key, out var label) ? label : key,
                OldValue = oldText,
                NewValue = newText
            });
        }

        return diffs;
    }

    private static Dictionary<string, object?> TryParseDict(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith('{'))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions)
                   ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string FormatValue(object? value)
    {
        if (value is null) return "—";
        if (value is JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Null => "—",
                JsonValueKind.String => el.GetString() ?? "—",
                JsonValueKind.Number => el.TryGetDecimal(out var d) ? d.ToString("N0") : el.ToString(),
                JsonValueKind.True => "نعم",
                JsonValueKind.False => "لا",
                _ => el.ToString()
            };
        }

        return value switch
        {
            decimal d => d.ToString("N0"),
            double dbl => dbl.ToString("N0"),
            float f => f.ToString("N0"),
            DateTime dt => dt.ToString("yyyy/MM/dd HH:mm"),
            bool b => b ? "نعم" : "لا",
            _ => value.ToString() ?? "—"
        };
    }

    private static (string Key, string Title, string Summary) ParseInvoiceKey(string? oldJson, string? newJson, int entityId)
    {
        var dict = TryParseDict(newJson);
        if (dict.Count == 0) dict = TryParseDict(oldJson);

        var number = GetString(dict, "InvoiceNumber") ?? $"#{entityId}";
        var type = GetString(dict, "InvoiceTypeDisplay") ?? GetString(dict, "InvoiceType") ?? "فاتورة";
        var party = GetString(dict, "PartyName") ?? "—";
        var net = GetString(dict, "NetAmount") ?? "—";
        var summary = GetString(dict, "Summary")
                      ?? $"تعديل فاتورة {number} | الطرف: {party} | المبلغ: {net}";
        return (number, $"فاتورة {type} — {number}", summary);
    }

    private static (string Key, string Title, string Summary) ParseProductKey(string? oldJson, string? newJson, int entityId)
    {
        var newDict = TryParseDict(newJson);
        var oldDict = TryParseDict(oldJson);
        var name = GetString(newDict, "Name") ?? GetString(oldDict, "Name") ?? $"منتج #{entityId}";
        var barcode = GetString(newDict, "Barcode") ?? GetString(oldDict, "Barcode");
        var title = string.IsNullOrWhiteSpace(barcode) ? name : $"{name} ({barcode})";
        return (name, title, string.Empty);
    }

    private static string? GetString(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is null) return null;
        return FormatValue(value) is { } text && text != "—" ? text : null;
    }

    private static string StripDeleteSuffix(string invoiceNumber)
    {
        var idx = invoiceNumber.LastIndexOf("-D", StringComparison.Ordinal);
        if (idx <= 0) return invoiceNumber;
        var suffix = invoiceNumber[(idx + 2)..];
        return int.TryParse(suffix, out _) ? invoiceNumber[..idx] : invoiceNumber;
    }

    private static string InvoiceTypeDisplay(InvoiceType type) => type switch
    {
        InvoiceType.Sale => "مبيعات",
        InvoiceType.Purchase => "مشتريات",
        InvoiceType.Installment => "أقساط",
        InvoiceType.PurchaseReturn => "مرتجع مشتريات",
        _ => type.ToString()
    };

    private static string VoucherTypeDisplay(VoucherType type) => type switch
    {
        VoucherType.Receipt => "قبض",
        VoucherType.Payment => "صرف",
        VoucherType.BankReceipt => "قبض مصرفي",
        VoucherType.InvestorDeposit => "إيداع مستثمر",
        VoucherType.InvestorWithdrawal => "سحب مستثمر",
        VoucherType.DebtReceipt => "قبض دين",
        _ => type.ToString()
    };

    private static readonly Dictionary<string, string> InvoiceFieldLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["InvoiceNumber"] = "رقم الفاتورة",
        ["InvoiceType"] = "نوع الفاتورة",
        ["InvoiceTypeDisplay"] = "نوع الفاتورة",
        ["PartyName"] = "الطرف",
        ["CustomerId"] = "العميل",
        ["SupplierId"] = "المورد",
        ["WarehouseId"] = "المخزن",
        ["WarehouseName"] = "المخزن",
        ["PaymentMethod"] = "طريقة الدفع",
        ["TotalAmount"] = "الإجمالي",
        ["DiscountAmount"] = "الخصم",
        ["NetAmount"] = "الصافي",
        ["Date"] = "التاريخ",
        ["Notes"] = "ملاحظات",
        ["ItemsCount"] = "عدد البنود",
        ["ItemsSummary"] = "ملخص البنود",
        ["Summary"] = "الملخص"
    };

    private static readonly Dictionary<string, string> ProductFieldLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "الاسم",
        ["ScientificName"] = "الاسم العلمي",
        ["Barcode"] = "الباركود",
        ["Description"] = "الوصف",
        ["CategoryId"] = "التصنيف",
        ["CategoryName"] = "التصنيف"
    };
}
