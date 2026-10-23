using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Infrastructure.Services.Customs;

/// <summary>
/// Builds Customs print models and delegates to <see cref="IExportService"/>.
/// Customer invoice/receipt/statement builders intentionally omit expenses,
/// InternalProfit, InternalNotes, and other internal financial details.
/// </summary>
public sealed class CustomsPrintService : ICustomsPrintService
{
    private readonly IExportService _exportService;

    public CustomsPrintService(IExportService exportService)
    {
        _exportService = exportService;
    }

    public CustomsPrintDocumentKind ResolveInvoiceKind(CustomsServiceType serviceType) =>
        serviceType == CustomsServiceType.Transport
            ? CustomsPrintDocumentKind.TransportInvoice
            : CustomsPrintDocumentKind.DailyInvoice;

    public InvoicePrintModel BuildCustomerInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null)
    {
        var kind = ResolveInvoiceKind(detail.ServiceType);
        return kind == CustomsPrintDocumentKind.TransportInvoice
            ? BuildTransportInvoice(detail, customerPhone)
            : BuildDailyInvoice(detail, customerPhone);
    }

    /// <summary>Clearance / ClearanceAndTransport — فاتورة تخليص يومية (نسخة عميل).</summary>
    public InvoicePrintModel BuildDailyInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null)
    {
        var title = detail.ServiceType == CustomsServiceType.ClearanceAndTransport
            ? "فاتورة تخليص يومية — تخليص ونقل"
            : "فاتورة تخليص يومية";
        return BuildCustomerInvoiceCore(detail, title, customerPhone);
    }

    /// <summary>Transport — فاتورة نقل (نسخة عميل).</summary>
    public InvoicePrintModel BuildTransportInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null) =>
        BuildCustomerInvoiceCore(detail, "فاتورة نقل", customerPhone);

    public void PrintCustomerInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null) =>
        _exportService.PrintInvoice(BuildCustomerInvoice(detail, customerPhone));

    public string ExportCustomerInvoicePdf(
        CustomsInvoiceDetail detail,
        string? customerPhone = null) =>
        _exportService.ExportInvoiceToPdf(BuildCustomerInvoice(detail, customerPhone));

    public VoucherPrintModel BuildReceipt(CustomsReceiptDetail detail)
    {
        var allocNotes = detail.Allocations.Count == 0
            ? detail.Notes
            : string.Join(" | ", detail.Allocations.Select(a => $"{a.InvoiceNumber}:{a.Amount:N0}"))
              + (string.IsNullOrWhiteSpace(detail.Notes) ? string.Empty : $" — {detail.Notes}");

        return new VoucherPrintModel
        {
            Title = "وصل قبض — التخليص الكمركي",
            VoucherNumber = detail.ReceiptNumber,
            VoucherTypeLabel = detail.PaymentMethodLabel,
            Date = detail.ReceiptDate,
            Amount = detail.Amount,
            CurrencyLabel = detail.CurrencyLabel,
            PartyLabel = "العميل",
            PartyName = detail.CustomerName,
            PartyPhone = detail.CustomerPhone,
            Notes = allocNotes
        };
    }

    public string ExportReceiptPdf(CustomsReceiptDetail detail) =>
        _exportService.ExportVoucherToPdf(BuildReceipt(detail));

    public StatementPrintModel BuildCustomerStatement(
        string customerName,
        string? customerPhone,
        DateTime? fromDate,
        DateTime? toDate,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines) =>
        new()
        {
            Title = $"كشف حساب تخليص — {customerName}",
            PartyName = customerName,
            PartyPhone = customerPhone,
            FromDate = fromDate,
            ToDate = toDate,
            Columns = columns,
            Rows = rows,
            SummaryLines = summaryLines
        };

    public void PrintCustomerStatement(StatementPrintModel model) =>
        _exportService.PrintTable(
            model.Title,
            model.Columns,
            model.Rows,
            model.SummaryLines);

    public string ExportCustomerStatementPdf(StatementPrintModel model) =>
        _exportService.ExportStatementToPdf(model);

    public void PrintReportTable(
        CustomsPrintDocumentKind kind,
        string title,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines = null)
    {
        _ = kind; // reserved for future official report templates
        _exportService.PrintTable(title, columns, rows, summaryLines);
    }

    public string ExportReportPdf(
        CustomsPrintDocumentKind kind,
        StatementPrintModel model)
    {
        _ = kind;
        return _exportService.ExportStatementToPdf(model);
    }

    private static InvoicePrintModel BuildCustomerInvoiceCore(
        CustomsInvoiceDetail detail,
        string title,
        string? customerPhone)
    {
        // Customer copy: vehicles as billable lines only — never expenses / InternalNotes / profit.
        var items = new List<InvoicePrintItem>();
        var n = 1;
        foreach (var v in detail.Vehicles)
        {
            var plate = string.Join(" / ", new[] { v.IraqiPlate, v.SyrianPlate }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            var name = string.IsNullOrWhiteSpace(v.CargoType)
                ? (string.IsNullOrWhiteSpace(plate) ? $"سيارة {n}" : $"سيارة — {plate}")
                : v.CargoType;

            items.Add(new InvoicePrintItem
            {
                Number = n++,
                ItemName = name,
                Quantity = 1,
                UnitPrice = v.VehiclePrice,
                TotalPrice = v.VehiclePrice,
                PlateNumber = string.IsNullOrWhiteSpace(plate) ? null : plate,
                ChassisNumber = string.IsNullOrWhiteSpace(v.SerialNumber) ? null : v.SerialNumber,
                VehicleType = CustomsVehicleStatusDisplay.ToArabic(v.Status),
                CarModel = string.IsNullOrWhiteSpace(v.IraqiDriver) && string.IsNullOrWhiteSpace(v.SyrianDriver)
                    ? null
                    : string.Join(" / ", new[] { v.IraqiDriver, v.SyrianDriver }
                        .Where(s => !string.IsNullOrWhiteSpace(s)))
            });
        }

        if (items.Count == 0 && detail.TotalAmount != 0)
        {
            items.Add(new InvoicePrintItem
            {
                Number = 1,
                ItemName = string.IsNullOrWhiteSpace(detail.Description) ? "خدمة تخليص" : detail.Description,
                Quantity = 1,
                UnitPrice = detail.TotalAmount + detail.DiscountAmount,
                TotalPrice = detail.TotalAmount + detail.DiscountAmount
            });
        }

        var subtotal = detail.VehiclesSum > 0
            ? detail.VehiclesSum
            : detail.TotalAmount + detail.DiscountAmount;

        return new InvoicePrintModel
        {
            Title = title,
            InvoiceNumber = detail.InvoiceNumber,
            Date = detail.InvoiceDate,
            PartyLabel = "العميل",
            PartyName = detail.CustomerName,
            PartyPhone = customerPhone,
            PaymentMethod = detail.ServiceTypeLabel,
            Notes = string.IsNullOrWhiteSpace(detail.CustomerNotes) ? null : detail.CustomerNotes,
            Items = items,
            Subtotal = subtotal,
            DiscountAmount = detail.DiscountAmount,
            GrandTotal = detail.TotalAmount,
            PaidAmount = detail.PaidAmount,
            RemainingAmount = detail.RemainingAmount,
            CurrencyLabel = string.IsNullOrWhiteSpace(detail.CurrencyLabel)
                ? AccountingCurrencyHelper.GetLabel(detail.Currency)
                : detail.CurrencyLabel,
            FxRate = detail.FxRate,
            ShowCarShowroomFields = detail.Vehicles.Count > 0,
            HideAmounts = false
            // Intentionally omitted: expenses, InternalProfit, InternalNotes
        };
    }
}
