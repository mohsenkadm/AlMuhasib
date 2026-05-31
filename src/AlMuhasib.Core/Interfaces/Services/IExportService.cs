namespace AlMuhasib.Core.Interfaces.Services;

public interface IExportService
{
    /// <summary>Exports data to an Excel file using ClosedXML. Returns the file bytes.</summary>
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1");

    /// <summary>Exports data to Excel and saves to the specified path.</summary>
    Task ExportToExcelFileAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1");

    /// <summary>Exports tabular data with custom column headers to an Excel file.</summary>
    void ExportToExcel(string filePath, string sheetName, string[] columns, IList<object[]> rows);

    /// <summary>Prints tabular data using a FlowDocument with custom title and columns.</summary>
    void PrintTable(string title, string[] columns, IList<object[]> rows, IList<string>? summaryLines = null);

    /// <summary>Prints a formatted invoice document.</summary>
    void PrintInvoice(InvoicePrintModel model);
}

/// <summary>Data passed to PrintInvoice.</summary>
public class InvoicePrintModel
{
    public string Title { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public string PartyName { get; set; } = string.Empty;       // Customer or Supplier
    public string PartyLabel { get; set; } = "العميل";
    public string WarehouseName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<InvoicePrintItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal GrandTotal { get; set; }
    // Installment extras
    public int? NumberOfInstallments { get; set; }
    public decimal? InstallmentAmount { get; set; }
    public decimal? CompanyFeeAmount { get; set; }
    public List<InstallmentPrintRow>? Schedule { get; set; }
    public string? FileNumber { get; set; }
}

public class InvoicePrintItem
{
    public int Number { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class InstallmentPrintRow
{
    public int Number { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
}
