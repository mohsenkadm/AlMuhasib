using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ISupervisoryReportService
{
    Task<(IReadOnlyList<DeletedInvoiceRow> Items, int TotalCount)> GetDeletedInvoicesAsync(
        SupervisoryQueryFilter filter, int page, int pageSize, InvoiceType? invoiceType = null);

    Task<(IReadOnlyList<DeletedVoucherRow> Items, int TotalCount)> GetDeletedVouchersAsync(
        SupervisoryQueryFilter filter, int page, int pageSize, VoucherType? voucherType = null);

    Task<(IReadOnlyList<DeletedProductRow> Items, int TotalCount)> GetDeletedProductsAsync(
        SupervisoryQueryFilter filter, int page, int pageSize);

    Task<(IReadOnlyList<DeletedCustomerRow> Items, int TotalCount)> GetDeletedCustomersAsync(
        SupervisoryQueryFilter filter, int page, int pageSize);

    Task<(IReadOnlyList<DeletedSupplierRow> Items, int TotalCount)> GetDeletedSuppliersAsync(
        SupervisoryQueryFilter filter, int page, int pageSize);

    Task<(IReadOnlyList<DeletedExpenseRow> Items, int TotalCount)> GetDeletedExpensesAsync(
        SupervisoryQueryFilter filter, int page, int pageSize);

    Task<(IReadOnlyList<EntityChangeRow> Items, int TotalCount)> GetInvoiceModificationsAsync(
        SupervisoryQueryFilter filter, int page, int pageSize);

    Task<(IReadOnlyList<EntityChangeRow> Items, int TotalCount)> GetProductModificationsAsync(
        SupervisoryQueryFilter filter, int page, int pageSize);

    Task<IReadOnlyList<string>> GetDeletedByUsernamesAsync();
    Task<IReadOnlyList<string>> GetModifierUsernamesAsync(string entityName);
}

public class SupervisoryQueryFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? DeletedBy { get; set; }
    public string? SearchTerm { get; set; }
}

public class DeletedInvoiceRow
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceTypeDisplay { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string DetailsSummary { get; set; } = string.Empty;
}

public class DeletedVoucherRow
{
    public int Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string VoucherTypeDisplay { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public decimal Amount { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string CashBoxName { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string DetailsSummary { get; set; } = string.Empty;
}

public class DeletedProductRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string DetailsSummary { get; set; } = string.Empty;
}

public class DeletedCustomerRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? FileNumber { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string DetailsSummary { get; set; } = string.Empty;
}

public class DeletedSupplierRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string DetailsSummary { get; set; } = string.Empty;
}

public class DeletedExpenseRow
{
    public int Id { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CashBoxName { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string DetailsSummary { get; set; } = string.Empty;
}

public class EntityChangeRow
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string EntityKey { get; set; } = string.Empty;
    public string EntityTitle { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public IReadOnlyList<ChangeFieldDiff> Diffs { get; set; } = [];
}

public class ChangeFieldDiff
{
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
