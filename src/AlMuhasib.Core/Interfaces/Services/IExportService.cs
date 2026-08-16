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

    /// <summary>Exports invoice to a PDF file and returns the full path.</summary>
    string ExportInvoiceToPdf(InvoicePrintModel model);

    /// <summary>Exports installment payment receipt to PDF and returns the full path.</summary>
    string ExportInstallmentPaymentReceiptToPdf(InstallmentPaymentReceiptPrintModel model);

    /// <summary>تصدير سند قبض/صرف إلى PDF.</summary>
    string ExportVoucherToPdf(VoucherPrintModel model);

    /// <summary>تصدير إيصال حركة مستثمر (إيداع/سحب/توزيع أرباح) إلى PDF.</summary>
    string ExportInvestorTransactionToPdf(InvestorTransactionPrintModel model);

    /// <summary>تصدير كشف حساب جدولي إلى PDF.</summary>
    string ExportStatementToPdf(StatementPrintModel model);

    /// <summary>طباعة إيصال كاشير حسب حجم الورق (A4 / 80mm / 58mm / 50mm).</summary>
    void PrintThermalReceipt(InvoicePrintModel model);

    /// <summary>تصدير عقد تقسيط PDF.</summary>
    string ExportInstallmentContractToPdf(InvoicePrintModel model);

    /// <summary>طباعة جدول أقساط للعميل.</summary>
    void PrintInstallmentSchedule(InvoicePrintModel model);

    /// <summary>طباعة كشف تفصيلي لخطة أقساط واحدة.</summary>
    void PrintInstallmentPlanDetail(InstallmentPlanDetailPrintModel model);

    /// <summary>طباعة عدة خطط أقساط مع ملخص كلي.</summary>
    void PrintInstallmentMultiPlanDetail(IReadOnlyList<InstallmentPlanDetailPrintModel> plans, string title);

    /// <summary>طباعة كشف الأقساط العام مع بطاقات إحصائيات.</summary>
    void PrintInstallmentPlansSummary(InstallmentPlansSummaryPrintModel model);
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
    public string? PartyPhone { get; set; }
    public string? PartyAddress { get; set; }
    public string? PartyEmail { get; set; }
    public string? DriverName { get; set; }
    public string? SalesRepresentativeName { get; set; }
    public string? SalesRepresentativePhone { get; set; }
    public string? SalesRepresentativeEmail { get; set; }
    /// <summary>نسخة مخزن — بدون أسعار أو مجاميع</summary>
    public bool HideAmounts { get; set; }
    public List<InvoicePrintItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal TransportFeeAmount { get; set; }
    public decimal GrandTotal { get; set; }
    // Installment extras
    public int? NumberOfInstallments { get; set; }
    public decimal? InstallmentAmount { get; set; }
    public decimal? CompanyFeeAmount { get; set; }
    public List<InstallmentPrintRow>? Schedule { get; set; }
    public string? FileNumber { get; set; }
    /// <summary>طباعة إيصال صيدلية يتضمن طريقة الاستخدام تحت كل صنف.</summary>
    public bool PharmacyUsageReceipt { get; set; }

    /// <summary>When true, A4/thermal printers render gold-specific columns and totals.</summary>
    public bool IsGoldInvoice { get; set; }
    public decimal FxRate { get; set; }
    public string? PricingCurrencyLabel { get; set; }
    public string? PaymentCurrencyLabel { get; set; }
    public decimal TotalGoldValue { get; set; }
    public decimal TotalMakingCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public string CurrencyLabel { get; set; } = "د.ع";
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal TotalAmountIqd { get; set; }
    public decimal TotalAmountUsd { get; set; }

    /// <summary>A4 layout template id: Classic, Compact, or Modern. Null = Classic.</summary>
    public string? A4TemplateId { get; set; }
}

public class InvoicePrintItem
{
    public int Number { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    /// <summary>طريقة استخدام الدواء — تُطبع في إيصال الصيدلية.</summary>
    public string? UsageInstructions { get; set; }

    public int? KaratValue { get; set; }
    public decimal? WeightGrams { get; set; }
    public decimal? MithqalPrice { get; set; }
    public decimal? PricePerGram { get; set; }
    public decimal? GoldValue { get; set; }
    public decimal? MakingCharge { get; set; }
    public string? LineDirectionLabel { get; set; }
}

public class InstallmentPrintRow
{
    public int Number { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string StatusText { get; set; } = "معلق";
    public int? DelayDays { get; set; }
}

public class InstallmentPlanDetailPrintModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? FileNumber { get; set; }
    public DateTime StartDate { get; set; }
    public string InstallmentTypeLabel { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public IReadOnlyList<InstallmentPrintRow> Schedule { get; set; } = [];
}

public class InstallmentPlansSummaryPrintModel
{
    public string Title { get; set; } = "كشف الأقساط العام";
    public string[] Columns { get; set; } = [];
    public IList<object[]> Rows { get; set; } = [];
    public int PlanCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int PaidInstallmentCount { get; set; }
}

/// <summary>إيصال تسديد قسط (فردي أو جماعي) للطباعة/واتساب.</summary>
public class InstallmentPaymentReceiptPrintModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? FileNumber { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string? CashBoxName { get; set; }
    public string? Notes { get; set; }
    public List<InstallmentPaymentReceiptLine> Lines { get; set; } = [];
    public decimal TotalPaid { get; set; }
    public decimal? PlanRemainingTotal { get; set; }
}

public class InstallmentPaymentReceiptLine
{
    public int SequenceNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAfter { get; set; }
    public string StatusText { get; set; } = string.Empty;
}

/// <summary>سند قبض/صرف للطباعة وواتساب.</summary>
public class VoucherPrintModel
{
    public string Title { get; set; } = "سند";
    public string VoucherNumber { get; set; } = string.Empty;
    public string VoucherTypeLabel { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public decimal BankFees { get; set; }
    public string? PartyLabel { get; set; }
    public string? PartyName { get; set; }
    public string? PartyPhone { get; set; }
    public string? CashBoxName { get; set; }
    public string? BankAccountName { get; set; }
    public string? Notes { get; set; }
}

/// <summary>إيصال حركة مستثمر (إيداع / سحب / توزيع أرباح).</summary>
public class InvestorTransactionPrintModel
{
    public string Title { get; set; } = "إيصال مستثمر";
    public string TransactionTypeLabel { get; set; } = string.Empty;
    public string InvestorName { get; set; } = string.Empty;
    public string? InvestorPhone { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public string? CashBoxName { get; set; }
    public string? Notes { get; set; }
    public decimal? BalanceAfter { get; set; }
}

/// <summary>كشف حساب جدولي للطباعة وواتساب.</summary>
public class StatementPrintModel
{
    public string Title { get; set; } = "كشف حساب";
    public string PartyName { get; set; } = string.Empty;
    public string? PartyPhone { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string[] Columns { get; set; } = [];
    public IList<object[]> Rows { get; set; } = [];
    public IList<string>? SummaryLines { get; set; }
}
