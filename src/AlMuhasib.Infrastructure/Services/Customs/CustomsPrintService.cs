using System.Globalization;
using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Core.Utilities;

namespace AlMuhasib.Infrastructure.Services.Customs;

/// <summary>
/// Builds Customs print models and delegates to <see cref="IExportService"/>.
/// Daily invoice uses the official landscape form; customer notes only (no InternalNotes / profit).
/// </summary>
public sealed class CustomsPrintService : ICustomsPrintService
{
    private readonly IExportService _exportService;
    private readonly CustomsPermissionGuard _permissions;

    public CustomsPrintService(IExportService exportService, CustomsPermissionGuard permissions)
    {
        _exportService = exportService;
        _permissions = permissions;
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

    /// <summary>Clearance / ClearanceAndTransport — فاتورة تخليص يومية (نسخة العميل بالشكل الرسمي).</summary>
    public InvoicePrintModel BuildDailyInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null)
    {
        var title = detail.ServiceType == CustomsServiceType.ClearanceAndTransport
            ? "فاتورة تخليص يومية — تخليص ونقل"
            : "فاتورة تخليص يومية";
        return BuildDailyForm(detail, title, customerPhone);
    }

    /// <summary>
    /// Transport — portrait internal/external forms matching company paper invoices.
    /// Branding header from print settings is applied by the document/PDF builders.
    /// </summary>
    public InvoicePrintModel BuildTransportInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null)
    {
        var kind = detail.TransportKind ?? CustomsTransportKind.Internal;
        var currency = string.IsNullOrWhiteSpace(detail.CurrencyLabel)
            ? AccountingCurrencyHelper.GetLabel(detail.Currency)
            : detail.CurrencyLabel;
        var transport = BuildTransportData(detail, kind, currency);
        var items = BuildLegacyItems(detail);
        var subtotal = detail.VehiclesSum > 0
            ? detail.VehiclesSum + detail.DelayPenaltiesSum
            : detail.TotalAmount + detail.DiscountAmount;

        return new InvoicePrintModel
        {
            Title = transport.Title,
            InvoiceNumber = detail.InvoiceNumber,
            Date = detail.InvoiceDate,
            PartyLabel = kind == CustomsTransportKind.External ? "المستورد / الشركة" : "المطلوب من السادة",
            PartyName = detail.CustomerName,
            PartyPhone = customerPhone,
            PaymentMethod = CustomsServiceTypeDisplay.ToArabic(detail.ServiceType, kind),
            Notes = string.IsNullOrWhiteSpace(detail.CustomerNotes) ? null : detail.CustomerNotes,
            Items = items,
            Subtotal = subtotal,
            DiscountAmount = detail.DiscountAmount,
            GrandTotal = detail.TotalAmount,
            PaidAmount = detail.PaidAmount,
            RemainingAmount = detail.RemainingAmount,
            CurrencyLabel = currency,
            FxRate = detail.FxRate,
            ShowCarShowroomFields = detail.Vehicles.Count > 0,
            HideAmounts = false,
            IsCustomsTransportInvoice = true,
            CustomsTransport = transport,
            PaperSize = "A4"
        };
    }

    public void PrintCustomerInvoice(
        CustomsInvoiceDetail detail,
        string? customerPhone = null)
    {
        _permissions.EnsurePrint(CustomsScreenNames.Invoices);
        _exportService.PrintInvoice(BuildCustomerInvoice(detail, customerPhone));
    }

    public string ExportCustomerInvoicePdf(
        CustomsInvoiceDetail detail,
        string? customerPhone = null)
    {
        _permissions.EnsureExport(CustomsScreenNames.Invoices);
        return _exportService.ExportInvoiceToPdf(BuildCustomerInvoice(detail, customerPhone));
    }

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

    public string ExportReceiptPdf(CustomsReceiptDetail detail)
    {
        _permissions.EnsurePrintOrExport(CustomsScreenNames.Receipts);
        return _exportService.ExportVoucherToPdf(BuildReceipt(detail));
    }

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

    public void PrintCustomerStatement(StatementPrintModel model)
    {
        _permissions.EnsurePrint(CustomsScreenNames.Statement);
        _exportService.PrintTable(
            model.Title,
            model.Columns,
            model.Rows,
            model.SummaryLines);
    }

    public string ExportCustomerStatementPdf(StatementPrintModel model)
    {
        _permissions.EnsureExport(CustomsScreenNames.Statement);
        return _exportService.ExportStatementToPdf(model);
    }

    public void PrintReportTable(
        string screenName,
        string title,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines = null,
        CustomsPrintDocumentKind? kind = null)
    {
        _ = kind;
        _permissions.EnsurePrint(screenName);
        _exportService.PrintTable(title, columns, rows, summaryLines);
    }

    public string ExportReportPdf(
        string screenName,
        StatementPrintModel model,
        CustomsPrintDocumentKind? kind = null)
    {
        _ = kind;
        _permissions.EnsureExport(screenName);
        return _exportService.ExportStatementToPdf(model);
    }

    public void ExportReportExcel(
        string screenName,
        string filePath,
        string sheetName,
        string[] columns,
        IList<object[]> rows)
    {
        _permissions.EnsureExport(screenName);
        _exportService.ExportToExcel(filePath, sheetName, columns, rows);
    }

    private static InvoicePrintModel BuildDailyForm(
        CustomsInvoiceDetail detail,
        string title,
        string? customerPhone)
    {
        var currency = string.IsNullOrWhiteSpace(detail.CurrencyLabel)
            ? AccountingCurrencyHelper.GetLabel(detail.Currency)
            : detail.CurrencyLabel;

        var daily = BuildDailyData(detail, currency);
        var items = BuildLegacyItems(detail);

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
            CurrencyLabel = currency,
            FxRate = detail.FxRate,
            ShowCarShowroomFields = detail.Vehicles.Count > 0,
            HideAmounts = false,
            IsCustomsDailyInvoice = true,
            CustomsDaily = daily,
            PaperSize = "A4Landscape"
            // InternalNotes / InternalProfit intentionally omitted
        };
    }

    private static CustomsTransportInvoicePrintData BuildTransportData(
        CustomsInvoiceDetail detail,
        CustomsTransportKind kind,
        string currencyLabel)
    {
        var isUsd = detail.Currency == AccountingCurrency.USD
                    || currencyLabel.Contains('$')
                    || currencyLabel.Contains("دولار", StringComparison.Ordinal);
        var rows = new List<CustomsTransportInvoicePrintRow>();
        var n = 1;
        foreach (var v in detail.Vehicles)
        {
            rows.Add(new CustomsTransportInvoicePrintRow
            {
                Number = n++,
                ReferenceNumber = v.SerialNumber?.Trim() ?? string.Empty,
                SyrianDriver = v.SyrianDriver?.Trim() ?? string.Empty,
                SyrianPlate = v.SyrianPlate?.Trim() ?? string.Empty,
                IraqiDriver = v.IraqiDriver?.Trim() ?? string.Empty,
                IraqiPlate = v.IraqiPlate?.Trim() ?? string.Empty,
                CargoType = v.CargoType?.Trim() ?? string.Empty,
                LoadingDate = v.LoadingDate?.ToString("yyyy/M/d", CultureInfo.InvariantCulture) ?? string.Empty,
                UnloadingDate = v.UnloadingDate?.ToString("yyyy/M/d", CultureInfo.InvariantCulture) ?? string.Empty,
                DelayPenaltyAmount = v.DelayPenaltyAmount,
                VehiclePrice = v.VehiclePrice,
                Notes = v.Notes?.Trim() ?? string.Empty
            });
        }

        var vehiclesSubtotal = rows.Sum(r => r.VehiclePrice);
        var delayTotal = kind == CustomsTransportKind.Internal
            ? rows.Sum(r => r.DelayPenaltyAmount)
            : 0m;
        var grand = detail.TotalAmount;

        var priceGroups = rows
            .GroupBy(r => r.VehiclePrice)
            .OrderByDescending(g => g.Key)
            .Select(g => new CustomsTransportPriceGroup
            {
                VehicleCount = g.Count(),
                UnitPrice = g.Key,
                LineTotal = g.Sum(x => x.VehiclePrice)
            })
            .ToList();

        var wordsCurrency = isUsd ? "دولار أمريكي" : "دينار عراقي";
        var fraction = isUsd ? "سنت" : "فلس";
        var rawWords = ArabicAmountToWords.Convert(grand, wordsCurrency, fraction);
        var coreWords = rawWords
            .Replace(" فقط لا غير", string.Empty, StringComparison.Ordinal)
            .Trim();
        var amountInWords = kind == CustomsTransportKind.Internal
            ? $"المبلغ فقط وقدره: {coreWords} لا غير."
            : $"فقط {coreWords} لا غير";

        return new CustomsTransportInvoicePrintData
        {
            TransportKind = kind,
            Title = CustomsTransportKindDisplay.InvoiceTitle(kind),
            InvoiceNumber = detail.InvoiceNumber,
            InvoiceDate = detail.InvoiceDate,
            CustomerName = AppendHonourific(detail.CustomerName),
            TransportRoute = detail.TransportRoute?.Trim() ?? string.Empty,
            ServiceDescription = string.IsNullOrWhiteSpace(detail.Description)
                ? (kind == CustomsTransportKind.Internal
                    ? $"شحن ونقل داخلي ({rows.Count} شاحنات)"
                    : string.Empty)
                : detail.Description.Trim(),
            CurrencyLabel = isUsd ? "$" : "د.ع",
            IsUsd = isUsd,
            Rows = rows,
            PriceGroups = priceGroups,
            VehiclesSubtotal = vehiclesSubtotal,
            DelayPenaltiesTotal = delayTotal,
            GrandTotal = grand,
            AmountInWords = amountInWords,
            OrganizerLabel = kind == CustomsTransportKind.External
                ? "توقيع وختم المنظم"
                : "منظم الفاتورة",
            RecipientLabel = kind == CustomsTransportKind.External
                ? "توقيع المستلم"
                : "المصادقة والإستلام"
        };
    }

    private static string AppendHonourific(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;
        if (trimmed.Contains("المحترمين", StringComparison.Ordinal)
            || trimmed.Contains("المحترم", StringComparison.Ordinal))
            return trimmed;
        return trimmed + " المحترمين";
    }

    private static CustomsDailyInvoicePrintData BuildDailyData(
        CustomsInvoiceDetail detail,
        string currency)
    {
        var classified = ClassifyExpenses(detail.Expenses);
        var rows = new List<CustomsDailyInvoicePrintRow>();
        var n = 1;

        foreach (var v in detail.Vehicles)
        {
            var driver = FirstNonEmpty(v.IraqiDriver, v.SyrianDriver);
            var plate = FirstNonEmpty(v.IraqiPlate, v.SyrianPlate);
            var consignment = string.IsNullOrWhiteSpace(v.SerialNumber)
                ? (v.CargoType ?? string.Empty)
                : string.IsNullOrWhiteSpace(v.CargoType)
                    ? v.SerialNumber
                    : $"{v.SerialNumber} — {v.CargoType}";

            decimal? clearance = null;
            decimal? land = null;
            if (v.VehiclePrice != 0)
            {
                if (detail.ServiceType == CustomsServiceType.Transport)
                    land = v.VehiclePrice;
                else
                    clearance = v.VehiclePrice;
            }

            rows.Add(new CustomsDailyInvoicePrintRow
            {
                Number = n++,
                DriverName = driver,
                WheelNumber = plate,
                UnloadingDate = v.UnloadingDate?.ToString("yyyy/MM/dd") ?? string.Empty,
                Consignment = consignment,
                ClearanceFees = clearance,
                LandTransport = land,
                Notes = v.Notes ?? string.Empty
            });
        }

        // Invoice-level fee types are not stored per vehicle — place classified
        // amounts on the first data row so the grid matches the reference form.
        if (rows.Count == 0)
        {
            rows.Add(new CustomsDailyInvoicePrintRow { Number = 1 });
        }

        var first = rows[0];
        first.Seal = AddNullable(first.Seal, classified.Seal);
        first.Radiation = AddNullable(first.Radiation, classified.Radiation);
        first.Duty = AddNullable(first.Duty, classified.Duty);
        first.QualityControl = AddNullable(first.QualityControl, classified.QualityControl);
        first.LandTransport = AddNullable(first.LandTransport, classified.LandTransport);
        first.ClearanceFees = AddNullable(first.ClearanceFees, classified.ClearanceFees);
        first.Expenses = AddNullable(first.Expenses, classified.OtherExpenses);

        var totalClearance = rows.Sum(r => r.ClearanceFees ?? 0m);
        var totalLand = rows.Sum(r => r.LandTransport ?? 0m);
        var totalDuties = rows.Sum(r => r.Duty ?? 0m);
        var totalInspections = rows.Sum(r => (r.QualityControl ?? 0m) + (r.Radiation ?? 0m) + (r.Seal ?? 0m));
        var totalExpenses = rows.Sum(r => r.Expenses ?? 0m);

        return new CustomsDailyInvoicePrintData
        {
            CustomerName = detail.CustomerName,
            InvoiceNumber = detail.InvoiceNumber,
            InvoiceDate = detail.InvoiceDate,
            CurrencyLabel = currency,
            Rows = rows,
            TotalDuties = totalDuties,
            TotalInspections = totalInspections,
            TotalLandTransport = totalLand,
            TotalClearanceFees = totalClearance,
            TotalExpenses = totalExpenses,
            InvoiceTotal = detail.TotalAmount,
            PaidAmount = detail.PaidAmount,
            RemainingAmount = detail.RemainingAmount,
            MinRows = CustomsDailyInvoicePrintData.DefaultMinRows
        };
    }

    private static List<InvoicePrintItem> BuildLegacyItems(CustomsInvoiceDetail detail)
    {
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

        return items;
    }

    private static ClassifiedExpenses ClassifyExpenses(IEnumerable<CustomsInvoiceExpenseDraft> expenses)
    {
        var result = new ClassifiedExpenses();
        foreach (var e in expenses)
        {
            var name = e.ExpenseTypeName ?? string.Empty;
            if (ContainsAny(name, "سيل"))
                result.Seal += e.Amount;
            else if (ContainsAny(name, "إشعاع", "اشعاع"))
                result.Radiation += e.Amount;
            else if (ContainsAny(name, "فحص", "سيطرة", "نوعية"))
                result.QualityControl += e.Amount;
            else if (ContainsAny(name, "نقل", "مانيفست", "مانيفيست", "مانيڤست", "بري"))
                result.LandTransport += e.Amount;
            else if (ContainsAny(name, "تخليص", "أتعاب", "اتعاب"))
                result.ClearanceFees += e.Amount;
            else if (ContainsAny(name, "رسم", "رسوم"))
                result.Duty += e.Amount;
            else
                result.OtherExpenses += e.Amount;
        }

        return result;
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

    private static decimal? AddNullable(decimal? current, decimal add) =>
        add == 0 ? current : (current ?? 0m) + add;

    private sealed class ClassifiedExpenses
    {
        public decimal Seal;
        public decimal Radiation;
        public decimal Duty;
        public decimal QualityControl;
        public decimal LandTransport;
        public decimal ClearanceFees;
        public decimal OtherExpenses;
    }
}
