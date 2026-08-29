using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Controls;

public static class GoldInvoiceDetailMapper
{
    public static GoldInvoiceDetailDisplayModel FromInvoice(GoldInvoice invoice)
    {
        var partyLabel = invoice.InvoiceType == GoldInvoiceType.Purchase ? "المورد" : "الزبون";
        var partyName = invoice.InvoiceType == GoldInvoiceType.Purchase
            ? invoice.Supplier?.Name ?? "—"
            : invoice.Customer?.Name ?? "—";

        var currencySuffix = invoice.PaymentCurrency == GoldCurrency.USD ? " $" : " د.ع";
        var hasCredit = invoice.RemainingAmount > 0 || invoice.PaymentMethod == GoldPaymentMethod.Credit;

        var lines = invoice.Lines
            .OrderBy(l => l.Id)
            .Select((line, index) => new GoldInvoiceDetailLineRow
            {
                Number = index + 1,
                Description = string.IsNullOrWhiteSpace(line.Description) ? "—" : line.Description,
                KaratText = $"{line.KaratValue}K",
                WeightText = $"{line.WeightGrams:N3} غ",
                GoldValueText = FormatMoney(line.GoldValue, invoice.PaymentCurrency),
                MakingChargeText = FormatMoney(line.MakingCharge, invoice.PaymentCurrency),
                LineTotalText = FormatMoney(line.LineTotal, invoice.PaymentCurrency)
            })
            .ToList();

        var payments = invoice.Payments
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.Id)
            .Select((payment, index) => new GoldInvoiceDetailPaymentRow
            {
                Number = index + 1,
                DateText = payment.PaymentDate.ToString("yyyy/MM/dd"),
                AmountText = FormatMoney(payment.Amount, payment.Currency),
                CurrencyText = payment.Currency == GoldCurrency.USD ? "USD" : "IQD",
                Notes = payment.Notes ?? string.Empty
            })
            .ToList();

        return new GoldInvoiceDetailDisplayModel
        {
            Title = GetInvoiceTitle(invoice.InvoiceType),
            InvoiceNumber = invoice.InvoiceNumber,
            DateText = invoice.InvoiceDate.ToString("yyyy/MM/dd"),
            InvoiceTypeText = GetInvoiceTypeLabel(invoice.InvoiceType),
            PartyLabel = partyLabel,
            PartyName = partyName,
            WarehouseName = invoice.Warehouse?.Name ?? "—",
            PaymentMethod = GetPaymentMethodLabel(invoice.PaymentMethod),
            StatusText = GetStatusLabel(invoice.Status),
            Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? null : invoice.Notes,
            GoldValueText = FormatMoney(invoice.TotalGoldValue, invoice.PaymentCurrency),
            MakingChargeText = FormatMoney(invoice.TotalMakingCharge, invoice.PaymentCurrency),
            DiscountText = FormatMoney(invoice.DiscountAmount, invoice.PaymentCurrency),
            WeightText = $"{invoice.TotalWeightGrams:N3} غ",
            GrandTotalText = FormatMoney(invoice.TotalAmount, invoice.PaymentCurrency) + currencySuffix,
            PaidAmountText = hasCredit ? FormatMoney(invoice.PaidAmount, invoice.PaymentCurrency) + currencySuffix : null,
            RemainingAmountText = hasCredit ? FormatMoney(invoice.RemainingAmount, invoice.PaymentCurrency) + currencySuffix : null,
            HasCreditInfo = hasCredit,
            HasNotes = !string.IsNullOrWhiteSpace(invoice.Notes),
            HasPayments = payments.Count > 0,
            Lines = new ObservableCollection<GoldInvoiceDetailLineRow>(lines),
            Payments = new ObservableCollection<GoldInvoiceDetailPaymentRow>(payments)
        };
    }

    private static string GetInvoiceTitle(GoldInvoiceType type) => type switch
    {
        GoldInvoiceType.Purchase => "تفاصيل فاتورة شراء",
        GoldInvoiceType.Exchange => "تفاصيل فاتورة تبديل",
        GoldInvoiceType.SaleReturn => "تفاصيل مرتجع بيع",
        _ => "تفاصيل فاتورة بيع"
    };

    private static string GetInvoiceTypeLabel(GoldInvoiceType type) => type switch
    {
        GoldInvoiceType.Purchase => "شراء",
        GoldInvoiceType.Exchange => "تبديل",
        GoldInvoiceType.SaleReturn => "مرتجع بيع",
        _ => "بيع"
    };

    private static string GetPaymentMethodLabel(GoldPaymentMethod method) => method switch
    {
        GoldPaymentMethod.Credit => "آجل",
        _ => "نقد"
    };

    private static string GetStatusLabel(GoldInvoiceStatus status) => status switch
    {
        GoldInvoiceStatus.Open => "مفتوح",
        GoldInvoiceStatus.PartiallyPaid => "مدفوع جزئياً",
        GoldInvoiceStatus.Cancelled => "ملغى",
        _ => "مكتمل"
    };

    private static string FormatMoney(decimal amount, GoldCurrency currency) =>
        currency == GoldCurrency.USD ? amount.ToString("N2") : amount.ToString("N0");
}
