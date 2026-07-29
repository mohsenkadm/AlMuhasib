namespace AlMuhasib.Core.Models.Ux;

public enum SmartAlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum SmartAlertAction
{
    None,
    OpenInstallments,
    OpenOverdueReport,
    OpenUnpaidSales,
    OpenUnpaidPurchases,
    OpenProducts,
    OpenWarehouseReport,
    OpenStockHealthReport,
    OpenExpiryReport,
    OpenVouchers,
    OpenSalesInvoiceQueue,
    OpenPurchaseInvoiceQueue,
    OpenInstallmentInvoiceQueue,
    OpenCollectionDashboard,
    OpenHotelCheckInOut,
    OpenHotelRooms,
    OpenHotelHousekeeping
}

public class SmartAlert
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public SmartAlertSeverity Severity { get; init; }
    public SmartAlertAction Action { get; init; }
    public int Count { get; init; }
    public decimal? Amount { get; init; }
}

public class DailyTaskItem
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public SmartAlertAction Action { get; init; }
    public int Priority { get; init; }
}

public class SmartAlertSummary
{
    public List<SmartAlert> Alerts { get; init; } = [];
    public List<DailyTaskItem> DailyTasks { get; init; } = [];
    public int TotalTaskCount => DailyTasks.Count;
}
