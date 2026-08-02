using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldPrintService : IGoldPrintService
{
    private readonly IExportService _exportService;

    public GoldPrintService(IExportService exportService)
    {
        _exportService = exportService;
    }

    public Task PrintInvoiceAsync(GoldInvoice invoice, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var typeLabel = invoice.InvoiceType switch
        {
            GoldInvoiceType.Sale => "فاتورة بيع ذهب",
            GoldInvoiceType.Purchase => "فاتورة شراء ذهب",
            GoldInvoiceType.Exchange => "فاتورة مبادلة ذهب",
            _ => "فاتورة ذهب"
        };

        var model = new InvoicePrintModel
        {
            Title = typeLabel,
            InvoiceNumber = invoice.InvoiceNumber,
            Date = invoice.InvoiceDate,
            PartyName = invoice.Customer?.Name
                ?? invoice.Supplier?.Name
                ?? string.Empty,
            PartyLabel = invoice.SupplierId.HasValue ? "المورد" : "الزبون",
            WarehouseName = invoice.Warehouse?.Name ?? string.Empty,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            Notes = invoice.Notes,
            PartyPhone = invoice.Customer?.Phone ?? invoice.Supplier?.Phone,
            PartyAddress = invoice.Customer?.Address ?? invoice.Supplier?.Address,
            Subtotal = invoice.TotalGoldValue + invoice.TotalMakingCharge,
            GrandTotal = invoice.IsExchange
                ? invoice.ExchangeCashDifference
                : invoice.TotalAmount,
            Items = invoice.Lines
                .OrderBy(l => l.Id)
                .Select((l, idx) => new InvoicePrintItem
                {
                    Number = idx + 1,
                    ItemName = string.IsNullOrWhiteSpace(l.Description)
                        ? $"عيار {l.KaratValue} ({(l.LineDirection == GoldInvoiceLineDirection.In ? "وارد" : "صادر")})"
                        : l.Description,
                    Quantity = l.WeightGrams,
                    UnitPrice = l.PricePerGram,
                    TotalPrice = l.LineTotal
                })
                .ToList()
        };

        _exportService.PrintInvoice(model);
        return Task.CompletedTask;
    }

    public Task PrintItemLabelAsync(GoldItem item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
}
