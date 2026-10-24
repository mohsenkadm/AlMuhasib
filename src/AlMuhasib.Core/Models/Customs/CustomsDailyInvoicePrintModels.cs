namespace AlMuhasib.Core.Models.Customs;

/// <summary>
/// Print payload for the Customs «فاتورة / كشف يومي» landscape form
/// (driver / plate / fee columns + summary cards). UI/print only — not persisted.
/// </summary>
public sealed class CustomsDailyInvoicePrintData
{
    public const int DefaultMinRows = 10;

    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CurrencyLabel { get; set; } = "د.ع";

    public List<CustomsDailyInvoicePrintRow> Rows { get; set; } = [];

    public decimal TotalDuties { get; set; }
    public decimal TotalInspections { get; set; }
    public decimal TotalLandTransport { get; set; }
    public decimal TotalClearanceFees { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public int MinRows { get; set; } = DefaultMinRows;
}

public sealed class CustomsDailyInvoicePrintRow
{
    public int Number { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string WheelNumber { get; set; } = string.Empty;
    public string UnloadingDate { get; set; } = string.Empty;
    public string Consignment { get; set; } = string.Empty;

    public decimal? Seal { get; set; }
    public decimal? Radiation { get; set; }
    public decimal? Duty { get; set; }
    public decimal? QualityControl { get; set; }
    public decimal? LandTransport { get; set; }
    public decimal? ClearanceFees { get; set; }
    public decimal? Expenses { get; set; }

    public string Notes { get; set; } = string.Empty;
}
