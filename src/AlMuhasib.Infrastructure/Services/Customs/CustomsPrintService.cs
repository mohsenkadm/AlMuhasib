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
        var receipt = BuildReceiptData(detail);
        return new VoucherPrintModel
        {
            Title = receipt.Title,
            VoucherNumber = detail.ReceiptNumber,
            VoucherTypeLabel = receipt.PaymentMethodLabel,
            Date = detail.ReceiptDate,
            Amount = detail.Amount,
            CurrencyLabel = receipt.CurrencyLabel,
            PartyLabel = "استلمنا من السادة",
            PartyName = receipt.CustomerName,
            PartyPhone = detail.CustomerPhone,
            Notes = receipt.NoteText,
            IsCustomsReceipt = true,
            CustomsReceipt = receipt
        };
    }

    private static CustomsReceiptPrintData BuildReceiptData(CustomsReceiptDetail detail)
    {
        var isUsd = detail.Currency == AccountingCurrency.USD
                    || (detail.CurrencyLabel?.Contains('$') ?? false)
                    || (detail.CurrencyLabel?.Contains("دولار", StringComparison.Ordinal) ?? false);
        var currencyLabel = isUsd ? "$" : "د.ع";
        var wordsCurrency = isUsd ? "دولار أمريكي" : "دينار عراقي";
        var fraction = isUsd ? "سنت" : "فلس";
        var rawWords = ArabicAmountToWords.Convert(detail.Amount, wordsCurrency, fraction);
        var coreWords = rawWords
            .Replace(" فقط لا غير", string.Empty, StringComparison.Ordinal)
            .Trim();

        var amountDisplay = isUsd
            ? $"${detail.Amount:N0}"
            : $"{detail.Amount:N0} {currencyLabel}";
        var amountInWords = $"{amountDisplay} ({coreWords} لا غير)";

        var rows = detail.Allocations
            .Where(a => a.Amount > 0 || a.InvoiceId > 0)
            .Select(a => new CustomsReceiptPrintAllocationRow
            {
                AccountDetails = BuildAllocationAccountDetails(a),
                InvoiceTotalDisplay = FormatReceiptMoney(a.InvoiceTotalAmount, isUsd, currencyLabel),
                PaidAmountDisplay = FormatReceiptMoney(a.Amount, isUsd, currencyLabel),
                RemainingAmountDisplay = FormatReceiptMoney(a.InvoiceRemainingAmount, isUsd, currencyLabel)
            })
            .ToList();

        if (rows.Count == 0)
        {
            rows.Add(new CustomsReceiptPrintAllocationRow
            {
                AccountDetails = string.IsNullOrWhiteSpace(detail.Notes)
                    ? "دفعة على حساب العميل"
                    : detail.Notes.Trim(),
                InvoiceTotalDisplay = FormatReceiptMoney(detail.Amount, isUsd, currencyLabel),
                PaidAmountDisplay = FormatReceiptMoney(detail.Amount, isUsd, currencyLabel),
                RemainingAmountDisplay = FormatReceiptMoney(0m, isUsd, currencyLabel)
            });
        }

        var remainingSum = detail.Allocations.Sum(a => a.InvoiceRemainingAmount);
        var customerBase = (detail.CustomerName ?? string.Empty).Trim();
        var totalReceived = isUsd
            ? $"{FormatReceiptMoney(detail.Amount, true, currencyLabel)} USD"
            : FormatReceiptMoney(detail.Amount, false, currencyLabel);

        return new CustomsReceiptPrintData
        {
            Title = "وصل قبض مالي (Receipt Voucher)",
            ReceiptNumber = detail.ReceiptNumber,
            ReceiptDate = detail.ReceiptDate,
            CustomerName = AppendHonourific(customerBase),
            AmountDisplay = amountDisplay,
            AmountInWords = amountInWords,
            PaymentMethodLabel = CustomsPaymentMethodDisplay.ToReceiptArabic(detail.PaymentMethod),
            PurposeText = BuildReceiptPurpose(detail),
            CurrencyLabel = currencyLabel,
            IsUsd = isUsd,
            Amount = detail.Amount,
            Allocations = rows,
            TotalReceivedDisplay = totalReceived,
            NoteText = BuildAutoReceiptNote(detail.Amount, remainingSum, isUsd, currencyLabel),
            ReceiverSignatureLabel = "توقيع واستلام",
            PayerSignatureLabel = string.IsNullOrWhiteSpace(customerBase)
                ? "توقيع المسلّم"
                : $"توقيع المسلّم ({customerBase})"
        };
    }

    private static string BuildAllocationAccountDetails(CustomsReceiptAllocationDraft a)
    {
        var invoiceNo = string.IsNullOrWhiteSpace(a.InvoiceNumber) ? $"#{a.InvoiceId}" : a.InvoiceNumber;
        var service = CustomsServiceTypeDisplay.ToArabic(a.InvoiceServiceType, a.InvoiceTransportKind);
        var baseText = string.IsNullOrWhiteSpace(a.InvoiceDescription)
            ? service
            : a.InvoiceDescription.Trim();

        if (a.InvoiceVehicleCount > 0
            && !baseText.Contains("سيارة", StringComparison.Ordinal)
            && !baseText.Contains("شاحن", StringComparison.Ordinal))
        {
            return $"{baseText} {a.InvoiceVehicleCount} سيارات (فاتورة {invoiceNo})";
        }

        if (baseText.Contains("فاتورة", StringComparison.Ordinal))
            return baseText;
        return $"{baseText} (فاتورة {invoiceNo})";
    }

    private static string BuildReceiptPurpose(CustomsReceiptDetail detail)
    {
        if (!string.IsNullOrWhiteSpace(detail.Notes))
            return detail.Notes.Trim();

        if (detail.Allocations.Count == 0)
            return "دفعة على حساب العميل";

        var nums = string.Join("، ", detail.Allocations
            .Select(a => a.InvoiceNumber)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct());
        if (string.IsNullOrWhiteSpace(nums))
            return "دفعة لحساب الفاتورة";

        var first = detail.Allocations[0];
        var head = string.IsNullOrWhiteSpace(first.InvoiceDescription)
            ? CustomsServiceTypeDisplay.ToArabic(first.InvoiceServiceType, first.InvoiceTransportKind)
            : first.InvoiceDescription.Trim();
        return $"دفعة من {head} (دفعة لحساب الفاتورة {nums})";
    }

    private static string BuildAutoReceiptNote(
        decimal amount,
        decimal remaining,
        bool isUsd,
        string currencyLabel)
    {
        var paid = FormatReceiptMoney(amount, isUsd, currencyLabel);
        var remain = FormatReceiptMoney(remaining, isUsd, currencyLabel);
        return $"تم استلام مبلغ {paid} كدفعة أولى وتثبيت المتبقي ({remain}) في الحساب لحين التسديد النهائي.";
    }

    private static string FormatReceiptMoney(decimal amount, bool isUsd, string currencyLabel) =>
        isUsd ? $"${amount:N2}" : $"{amount:N0} {currencyLabel}";

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

    public StatementPrintModel BuildOutstandingBalanceStatement(CustomsOutstandingClaimDetail claim)
    {
        var data = BuildOutstandingPrintData(claim);
        return new StatementPrintModel
        {
            Title = data.Title,
            PartyName = data.CustomerName,
            PartyPhone = claim.CustomerPhone,
            FromDate = null,
            ToDate = null,
            Columns = [],
            Rows = [],
            SummaryLines =
            [
                data.BaseInvoiceTotalLabel + ": " + data.BaseInvoiceTotalDisplay,
                data.ReceiptsTotalLabel + ": " + data.ReceiptsTotalDisplay,
                data.RemainingLabel + ": " + data.RemainingDisplay,
                data.AmountInWords
            ],
            IsCustomsOutstandingStatement = true,
            CustomsOutstandingStatement = data
        };
    }

    public void PrintCustomerStatement(StatementPrintModel model)
    {
        _permissions.EnsurePrint(CustomsScreenNames.Statement);
        if (model.IsCustomsOutstandingStatement)
        {
            _exportService.PrintStatement(model);
            return;
        }

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

    private static CustomsOutstandingStatementPrintData BuildOutstandingPrintData(
        CustomsOutstandingClaimDetail claim)
    {
        var isUsd = claim.Currency == AccountingCurrency.USD;
        var currencyLabel = isUsd ? "$" : "د.ع";
        var invoices = claim.Invoices;
        var total = invoices.Sum(i => i.TotalAmount);
        var paid = invoices.Sum(i => i.PaidAmount);
        var remaining = invoices.Sum(i => i.RemainingAmount);

        var allReceipts = invoices
            .SelectMany(i => i.ReceiptNumbers)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var paidHeader = allReceipts.Count == 1
            ? $"المبلغ المسدد (وصل {allReceipts[0]})"
            : allReceipts.Count > 1
                ? $"المبلغ المسدد (وصولات {string.Join("، ", allReceipts)})"
                : "المبلغ المسدد (الواصل)";

        string invoiceRef;
        if (invoices.Count == 0)
            invoiceRef = "—";
        else if (invoices.Count == 1)
            invoiceRef = BuildInvoiceReference(invoices[0]);
        else
            invoiceRef = $"{invoices.Count} فواتير مفتوحة — " + string.Join("، ",
                invoices.Select(i => i.InvoiceNumber));

        var rows = invoices.Select((inv, idx) => new CustomsOutstandingStatementPrintRow
        {
            Number = idx + 1,
            Description = BuildRowDescription(inv),
            InvoiceTotalDisplay = FormatClaimMoney(inv.TotalAmount, isUsd, currencyLabel),
            PaidAmountDisplay = FormatClaimMoney(inv.PaidAmount, isUsd, currencyLabel),
            RemainingAmountDisplay = FormatClaimMoney(inv.RemainingAmount, isUsd, currencyLabel)
        }).ToList();

        var wordsCurrency = isUsd ? "دولار أمريكي" : "دينار عراقي";
        var fraction = isUsd ? "سنت" : "فلس";
        var rawWords = ArabicAmountToWords.Convert(remaining, wordsCurrency, fraction);
        var coreWords = rawWords
            .Replace(" فقط لا غير", string.Empty, StringComparison.Ordinal)
            .Trim();

        var baseLabel = invoices.Count == 1
            ? $"إجمالي الفاتورة الأساسية ({invoices[0].InvoiceNumber})"
            : "إجمالي الفواتير الأساسية";
        var receiptsLabel = allReceipts.Count == 1
            ? $"الواصل بموجب سند القبض ({allReceipts[0]})"
            : allReceipts.Count > 1
                ? $"الواصل بموجب سندات القبض ({string.Join("، ", allReceipts)})"
                : "الواصل بموجب سندات القبض";

        var customerBase = (claim.CustomerName ?? string.Empty).Trim();
        return new CustomsOutstandingStatementPrintData
        {
            Title = "مطالبة بدين متبقي (Outstanding Balance Statement)",
            DocumentNumber = string.IsNullOrWhiteSpace(claim.DocumentNumber)
                ? $"DN-{claim.DocumentDate:yyyy}-{claim.CustomerId:D4}"
                : claim.DocumentNumber,
            DocumentDate = claim.DocumentDate == default ? DateTime.Today : claim.DocumentDate,
            CustomerName = AppendHonourific(customerBase),
            InvoiceReferenceText = invoiceRef,
            AccountStatusText = remaining > 0 ? "دين متبقي مستحق الدفع" : "مسدد بالكامل",
            PaidColumnHeader = paidHeader,
            CurrencyLabel = currencyLabel,
            IsUsd = isUsd,
            Rows = rows,
            BaseInvoiceTotalLabel = baseLabel,
            BaseInvoiceTotalDisplay = FormatClaimMoney(total, isUsd, currencyLabel),
            ReceiptsTotalLabel = receiptsLabel,
            ReceiptsTotalDisplay = FormatClaimMoney(paid, isUsd, currencyLabel),
            RemainingLabel = "الرصيد المتبقي (دين مستحق الدفع)",
            RemainingDisplay = FormatClaimMoney(remaining, isUsd, currencyLabel),
            AmountInWords = $"المبلغ فقط وقدره: {coreWords} لا غير",
            BalanceDueDisplay = isUsd
                ? $"{FormatClaimMoney(remaining, true, currencyLabel)} USD"
                : FormatClaimMoney(remaining, false, currencyLabel),
            RemainingTotal = remaining,
            OrganizerSignatureLabel = "منظّم المطالبة",
            PayerSignatureLabel = string.IsNullOrWhiteSpace(customerBase)
                ? "إقرار وتعهد بالدفع"
                : $"إقرار وتعهد بالدفع ({customerBase})"
        };
    }

    private static string BuildInvoiceReference(CustomsOutstandingClaimInvoice inv)
    {
        var detail = BuildServiceDetail(inv);
        return string.IsNullOrWhiteSpace(detail)
            ? $"الفاتورة رقم {inv.InvoiceNumber}"
            : $"الفاتورة رقم {inv.InvoiceNumber} ({detail})";
    }

    private static string BuildRowDescription(CustomsOutstandingClaimInvoice inv)
    {
        var detail = BuildServiceDetail(inv);
        return string.IsNullOrWhiteSpace(detail)
            ? $"متبقي حساب الفاتورة {inv.InvoiceNumber}"
            : $"متبقي حساب الفاتورة {inv.InvoiceNumber} ({detail})";
    }

    private static string BuildServiceDetail(CustomsOutstandingClaimInvoice inv)
    {
        if (!string.IsNullOrWhiteSpace(inv.Description))
        {
            var d = inv.Description.Trim();
            if (inv.VehicleCount > 0
                && !d.Contains("سيارة", StringComparison.Ordinal)
                && !d.Contains("شاحن", StringComparison.Ordinal))
                return $"{d} {inv.VehicleCount} سيارات";
            return d;
        }

        if (inv.VehicleCount > 0)
            return $"{inv.ServiceLabel} {inv.VehicleCount} سيارات";
        return inv.ServiceLabel;
    }

    private static string FormatClaimMoney(decimal amount, bool isUsd, string currencyLabel) =>
        isUsd ? $"${amount:N2}" : $"{amount:N0} {currencyLabel}";

    public void PrintReportTable(
        string screenName,
        string title,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines = null,
        CustomsPrintDocumentKind? kind = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? partyName = null)
    {
        _permissions.EnsurePrint(screenName);
        _exportService.PrintStatement(BuildCustomsReportStatement(
            title,
            partyName: string.IsNullOrWhiteSpace(partyName) ? "نظام التخليص الكمركي" : partyName,
            fromDate,
            toDate,
            columns,
            rows,
            summaryLines,
            kind));
    }

    public string ExportReportPdf(
        string screenName,
        StatementPrintModel model,
        CustomsPrintDocumentKind? kind = null)
    {
        _permissions.EnsureExport(screenName);
        ApplyCustomsReportForm(model, kind);
        return _exportService.ExportStatementToPdf(model);
    }

    private static StatementPrintModel BuildCustomsReportStatement(
        string title,
        string partyName,
        DateTime? fromDate,
        DateTime? toDate,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines,
        CustomsPrintDocumentKind? kind)
    {
        var model = new StatementPrintModel
        {
            Title = title,
            PartyName = partyName,
            FromDate = fromDate,
            ToDate = toDate,
            Columns = columns,
            Rows = rows,
            SummaryLines = summaryLines
        };
        ApplyCustomsReportForm(model, kind);
        return model;
    }

    private static void ApplyCustomsReportForm(
        StatementPrintModel model,
        CustomsPrintDocumentKind? kind)
    {
        var resolvedKind = kind ?? model.CustomsReport?.Kind;
        var period = BuildReportPeriod(model.FromDate, model.ToDate);
        var subtitle = resolvedKind.HasValue
            ? CustomsPrintDocumentKindDisplay.ToArabic(resolvedKind)
            : (string.IsNullOrWhiteSpace(model.PartyName) ? "نظام التخليص الكمركي" : model.PartyName);

        model.IsCustomsReport = true;
        model.CustomsReport = new CustomsReportPrintData
        {
            Title = string.IsNullOrWhiteSpace(model.Title) ? "تقرير التخليص الكمركي" : model.Title,
            Subtitle = subtitle,
            DocumentNumber = $"CR-{DateTime.Now:yyyyMMdd-HHmm}",
            PrintedAt = DateTime.Now,
            PartyName = model.PartyName ?? string.Empty,
            PeriodLabel = period,
            Columns = model.Columns ?? [],
            Rows = model.Rows ?? [],
            SummaryLines = model.SummaryLines?.ToList() ?? [],
            Kind = resolvedKind,
            UseLandscape = (model.Columns?.Length ?? 0) >= 5
        };
    }

    private static string BuildReportPeriod(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return "جميع الفترات";
        if (from is not null && to is not null)
            return $"{from:yyyy/MM/dd} — {to:yyyy/MM/dd}";
        if (from is not null) return $"من {from:yyyy/MM/dd}";
        return $"حتى {to:yyyy/MM/dd}";
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
