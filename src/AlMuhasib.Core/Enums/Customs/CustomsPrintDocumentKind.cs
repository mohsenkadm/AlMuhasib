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
    VehicleReport = 5
}
