using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Models.Customs;

/// <summary>
/// Portrait transport invoice payload (internal or external) matching company paper forms.
/// Branding header from print settings is applied by the document/PDF builders.
/// </summary>
public sealed class CustomsTransportInvoicePrintData
{
    public CustomsTransportKind TransportKind { get; set; } = CustomsTransportKind.Internal;
    public string Title { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Internal only — مسار النقل الداخلي.</summary>
    public string TransportRoute { get; set; } = string.Empty;
    /// <summary>Internal only — نوع الخدمة text (from invoice Description).</summary>
    public string ServiceDescription { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = "د.ع";
    public bool IsUsd { get; set; }
    public List<CustomsTransportInvoicePrintRow> Rows { get; set; } = [];
    public List<CustomsTransportPriceGroup> PriceGroups { get; set; } = [];
    public decimal VehiclesSubtotal { get; set; }
    public decimal DelayPenaltiesTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string AmountInWords { get; set; } = string.Empty;
    public string OrganizerLabel { get; set; } = "منظم الفاتورة";
    public string RecipientLabel { get; set; } = "المصادقة والإستلام";
}

public sealed class CustomsTransportInvoicePrintRow
{
    public int Number { get; set; }
    /// <summary>رقم الفاتورة / المرجع في صف السيارة (SerialNumber).</summary>
    public string ReferenceNumber { get; set; } = string.Empty;
    public string SyrianDriver { get; set; } = string.Empty;
    public string SyrianPlate { get; set; } = string.Empty;
    public string IraqiDriver { get; set; } = string.Empty;
    public string IraqiPlate { get; set; } = string.Empty;
    public string CargoType { get; set; } = string.Empty;
    public string LoadingDate { get; set; } = string.Empty;
    public string UnloadingDate { get; set; } = string.Empty;
    public decimal DelayPenaltyAmount { get; set; }
    public decimal VehiclePrice { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class CustomsTransportPriceGroup
{
    public int VehicleCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
