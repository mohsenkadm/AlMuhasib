namespace AlMuhasib.Core.Enums.Customs;

/// <summary>
/// Stable document keys for Customs printing. Replace builder bodies per kind
/// when the company supplies official form templates — call sites stay unchanged.
/// </summary>
public enum CustomsPrintDocumentKind
{
    DailyInvoice = 0,
    TransportInvoice = 1,
    Receipt = 2,
    CustomerStatement = 3,
    MonthlyReport = 4,
    VehicleReport = 5,
    DailyReport = 6,
    DebtsReport = 7,
    InvoiceReport = 8,
    CustomerReport = 9,
    ProfitReport = 10,
    CustomersList = 11,
    InvoicesList = 12,
    ReceiptsList = 13
}

public static class CustomsPrintDocumentKindDisplay
{
    public static string ToArabic(CustomsPrintDocumentKind? kind) => kind switch
    {
        CustomsPrintDocumentKind.DailyInvoice => "فاتورة تخليص يومية",
        CustomsPrintDocumentKind.TransportInvoice => "فاتورة نقل",
        CustomsPrintDocumentKind.Receipt => "وصل قبض مالي",
        CustomsPrintDocumentKind.CustomerStatement => "مطالبة بدين متبقي",
        CustomsPrintDocumentKind.MonthlyReport => "التقرير الشهري",
        CustomsPrintDocumentKind.VehicleReport => "تقرير السيارات",
        CustomsPrintDocumentKind.DailyReport => "التقرير اليومي",
        CustomsPrintDocumentKind.DebtsReport => "تقرير الديون",
        CustomsPrintDocumentKind.InvoiceReport => "تقرير الفواتير",
        CustomsPrintDocumentKind.CustomerReport => "تقرير العملاء",
        CustomsPrintDocumentKind.ProfitReport => "تقرير الربح",
        CustomsPrintDocumentKind.CustomersList => "قائمة العملاء",
        CustomsPrintDocumentKind.InvoicesList => "قائمة الفواتير",
        CustomsPrintDocumentKind.ReceiptsList => "قائمة وصولات القبض",
        _ => "تقرير التخليص الكمركي"
    };
}
