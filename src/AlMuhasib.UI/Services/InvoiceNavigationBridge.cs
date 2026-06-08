using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

/// <summary>ربط تقارير الفواتير بفتح شاشة فاتورة جديدة مع نسخ بنود فاتورة سابقة.</summary>
public static class InvoiceNavigationBridge
{
    public static int? PendingSalesCopyInvoiceId { get; set; }
    public static int? PendingPurchaseCopyInvoiceId { get; set; }
    public static int? PendingSalesReturnFromInvoiceId { get; set; }
    public static InvoiceQueueKind? PendingOpenQueueKind { get; set; }

    public static Func<int, Task>? CopyToSalesInvoiceAsync { get; set; }
    public static Func<int, Task>? CopyToPurchaseInvoiceAsync { get; set; }
    public static Func<int, Task>? ReturnSalesInvoiceAsync { get; set; }
}
