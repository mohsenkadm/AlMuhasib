using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldPrintService : IGoldPrintService
{
    private readonly IExportService _exportService;
    private readonly IBarcodeLabelService? _barcodeLabelService;

    public GoldPrintService(
        IExportService exportService,
        IBarcodeLabelService? barcodeLabelService = null)
    {
        _exportService = exportService;
        _barcodeLabelService = barcodeLabelService;
    }

    public InvoicePrintModel BuildInvoicePrintModel(GoldInvoice invoice) => BuildPrintModel(invoice);

    public Task PrintInvoiceAsync(GoldInvoice invoice, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var model = BuildInvoicePrintModel(invoice);

        // Routing A4 vs thermal is handled by the export layer using GoldReceiptPaperSize
        // (thermal → PrintThermalReceipt; otherwise PrintInvoice).
        _exportService.PrintInvoice(model);
        return Task.CompletedTask;
    }

    public Task PrintItemLabelAsync(GoldItem item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_barcodeLabelService is not null && !string.IsNullOrWhiteSpace(item.Barcode))
        {
            _barcodeLabelService.PrintLabels(
            [
                new BarcodeLabelItem
                {
                    ProductName = item.Name,
                    Barcode = item.Barcode.Trim(),
                    KaratValue = item.KaratValue,
                    WeightGrams = item.WeightGrams
                }
            ]);
            return Task.CompletedTask;
        }

        var columns = new[] { "البيان", "القيمة" };
        var rows = new List<object[]>
        {
            new object[] { "الاسم", item.Name },
            new object[] { "الباركود", string.IsNullOrWhiteSpace(item.Barcode) ? "—" : item.Barcode },
            new object[] { "العيار", item.KaratValue.ToString() },
            new object[] { "الوزن (غ)", item.WeightGrams.ToString("0.###") },
            new object[] { "أجرة مقترحة", item.SuggestedMakingCharge.ToString("0.##") }
        };

        _exportService.PrintTable($"ملصق قطعة ذهب — {item.Name}", columns, rows);
        return Task.CompletedTask;
    }

    public Task PrintItemLabelsAsync(IEnumerable<GoldItem> items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = items.Where(i => i is not null).ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("لا توجد قطع للطباعة");

        if (_barcodeLabelService is not null)
        {
            var labels = list
                .Where(i => !string.IsNullOrWhiteSpace(i.Barcode))
                .Select(i => new BarcodeLabelItem
                {
                    ProductName = i.Name,
                    Barcode = i.Barcode.Trim(),
                    KaratValue = i.KaratValue,
                    WeightGrams = i.WeightGrams
                })
                .ToList();

            if (labels.Count == 0)
                throw new InvalidOperationException("القطع المحددة بلا باركود — عيّن باركوداً أولاً");

            _barcodeLabelService.PrintLabels(labels);
            return Task.CompletedTask;
        }

        foreach (var item in list)
            _ = PrintItemLabelAsync(item, cancellationToken);

        return Task.CompletedTask;
    }

    internal static InvoicePrintModel BuildPrintModel(GoldInvoice invoice)
    {
        var typeLabel = invoice.InvoiceType switch
        {
            GoldInvoiceType.Sale => "فاتورة بيع ذهب",
            GoldInvoiceType.Purchase => "فاتورة شراء ذهب",
            GoldInvoiceType.Exchange => "فاتورة مبادلة ذهب",
            GoldInvoiceType.SaleReturn => "مرتجع بيع ذهب",
            _ => "فاتورة ذهب"
        };

        var paymentLabel = invoice.PaymentMethod switch
        {
            GoldPaymentMethod.Cash => "نقدي",
            GoldPaymentMethod.Credit => "آجل",
            _ => invoice.PaymentMethod.ToString()
        };

        return new InvoicePrintModel
        {
            Title = typeLabel,
            InvoiceNumber = invoice.InvoiceNumber,
            Date = invoice.InvoiceDate,
            PartyName = invoice.Customer?.Name
                ?? invoice.Supplier?.Name
                ?? string.Empty,
            PartyLabel = invoice.SupplierId.HasValue ? "المورد" : "الزبون",
            WarehouseName = invoice.Warehouse?.Name ?? string.Empty,
            PaymentMethod = paymentLabel,
            Notes = invoice.Notes,
            PartyPhone = invoice.Customer?.Phone ?? invoice.Supplier?.Phone,
            PartyAddress = invoice.Customer?.Address ?? invoice.Supplier?.Address,
            Subtotal = invoice.TotalGoldValue + invoice.TotalMakingCharge,
            GrandTotal = invoice.IsExchange
                ? invoice.ExchangeCashDifference
                : invoice.TotalAmount,
            IsGoldInvoice = true,
            FxRate = invoice.FxRate,
            PricingCurrencyLabel = FormatCurrency(invoice.PricingCurrency),
            PaymentCurrencyLabel = FormatCurrency(invoice.PaymentCurrency),
            TotalGoldValue = invoice.TotalGoldValue,
            TotalMakingCharge = invoice.TotalMakingCharge,
            DiscountAmount = invoice.DiscountAmount,
            PaidAmount = invoice.PaidAmount,
            RemainingAmount = invoice.RemainingAmount,
            TotalAmountIqd = invoice.TotalAmountIqd,
            TotalAmountUsd = invoice.TotalAmountUsd,
            Items = invoice.Lines
                .OrderBy(l => l.Id)
                .Select((l, idx) => new InvoicePrintItem
                {
                    Number = idx + 1,
                    ItemName = string.IsNullOrWhiteSpace(l.Description)
                        ? $"عيار {l.KaratValue}"
                        : l.Description,
                    Quantity = l.WeightGrams,
                    UnitPrice = l.PricePerGram,
                    TotalPrice = l.LineTotal,
                    KaratValue = l.KaratValue,
                    WeightGrams = l.WeightGrams,
                    MithqalPrice = l.MithqalPrice,
                    PricePerGram = l.PricePerGram,
                    GoldValue = l.GoldValue,
                    MakingCharge = l.MakingCharge,
                    LineDirectionLabel = l.LineDirection == GoldInvoiceLineDirection.In ? "وارد" : "صادر"
                })
                .ToList()
        };
    }

    private static string FormatCurrency(GoldCurrency currency) => currency switch
    {
        GoldCurrency.IQD => "د.ع",
        GoldCurrency.USD => "USD",
        _ => currency.ToString()
    };
}
