using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

/// <summary>ربط تقارير الفواتير بفتح شاشة فاتورة جديدة مع نسخ بنود فاتورة سابقة.</summary>
public static class InvoiceNavigationBridge
{
    public static int? PendingSalesCopyInvoiceId { get; set; }
    public static int? PendingPurchaseCopyInvoiceId { get; set; }
    public static int? PendingSalesReturnFromInvoiceId { get; set; }
    public static int? PendingPurchaseReturnFromInvoiceId { get; set; }
    /// <summary>فتح شاشة المشتريات مباشرة في وضع المرتجع (بدون فاتورة مصدر بعد).</summary>
    public static bool PendingPurchaseReturnMode { get; set; }
    /// <summary>فتح شاشة المبيعات مباشرة في وضع المرتجع.</summary>
    public static bool PendingSalesReturnMode { get; set; }
    public static int? PendingSalesEditInvoiceId { get; set; }
    public static int? PendingPurchaseEditInvoiceId { get; set; }
    public static int? PendingInstallmentEditInvoiceId { get; set; }
    public static InvoiceQueueKind? PendingOpenQueueKind { get; set; }

    public static Func<int, Task>? CopyToSalesInvoiceAsync { get; set; }
    public static Func<int, Task>? CopyToPurchaseInvoiceAsync { get; set; }
    public static Func<int, Task>? ReturnSalesInvoiceAsync { get; set; }
    public static Func<int, Task>? ReturnPurchaseInvoiceAsync { get; set; }
    public static Func<int, Task>? EditSalesInvoiceAsync { get; set; }
    public static Func<int, Task>? EditPurchaseInvoiceAsync { get; set; }
    public static Func<int, Task>? EditInstallmentInvoiceAsync { get; set; }
}
