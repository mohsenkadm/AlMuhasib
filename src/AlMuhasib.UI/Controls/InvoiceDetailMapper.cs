using System.Collections.ObjectModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Controls;

internal static class InvoiceDetailMapper
{
    public static InvoiceDetailDisplayModel FromInvoice(
        Invoice invoice,
        string? paymentMethodOverride = null,
        decimal? companyFeeOverride = null)
    {
        var currency = invoice.Currency;
        var payment = paymentMethodOverride ?? PaymentMethodLabel(invoice.PaymentMethod);
        var companyFee = companyFeeOverride ?? invoice.CompanyFeeAmount;
        var plan = invoice.InstallmentPlans.FirstOrDefault();
        var installmentRows = new ObservableCollection<InvoiceDetailInstallmentRow>();

        if (plan?.Installments is { Count: > 0 } installments)
        {
            var ordered = installments.OrderBy(i => i.DueDate).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                var inst = ordered[i];
                installmentRows.Add(new InvoiceDetailInstallmentRow
                {
                    Number = i + 1,
                    DueDateText = inst.DueDate.ToString("yyyy/MM/dd"),
                    AmountText = AccountingCurrencyHelper.Format(inst.Amount, currency),
                    StatusText = inst.Status switch
                    {
                        InstallmentStatus.Paid => "مسدد",
                        InstallmentStatus.PartiallyPaid => "جزئي",
                        InstallmentStatus.Overdue => "متأخر",
                        _ => "معلق"
                    }
                });
            }
        }

        return new InvoiceDetailDisplayModel
        {
            Title = invoice.InvoiceType switch
            {
                InvoiceType.Sale => "فاتورة مبيعات",
                InvoiceType.Purchase => "فاتورة مشتريات",
                InvoiceType.PurchaseReturn => "مرتجع مشتريات",
                InvoiceType.SaleReturn => "مرتجع مبيعات",
                InvoiceType.Installment => "فاتورة أقساط",
                InvoiceType.Damage => "فاتورة تلف",
                _ => "تفاصيل الفاتورة"
            },
            InvoiceNumber = invoice.InvoiceNumber,
            DateText = invoice.Date.ToString("yyyy/MM/dd"),
            CreditDueDateText = invoice.CreditDueDate?.ToString("yyyy/MM/dd"),
            PartyLabel = invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn ? "المورد" : "العميل",
            PartyName = invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
                ? invoice.Supplier?.Name ?? "—"
                : CustomerDisplayHelper.FormatDisplayName(
                    invoice.Customer?.Name ?? "—",
                    invoice.Customer?.FileNumber),
            WarehouseName = invoice.Warehouse?.Name ?? "—",
            PaymentMethod = payment,
            Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? null : invoice.Notes.Trim(),
            HasNotes = !string.IsNullOrWhiteSpace(invoice.Notes),
            SubtotalText = AccountingCurrencyHelper.Format(invoice.TotalAmount, currency),
            DiscountText = invoice.DiscountAmount > 0
                ? AccountingCurrencyHelper.Format(invoice.DiscountAmount, currency)
                : "—",
            RoundingText = invoice.RoundingAmount != 0
                ? AccountingCurrencyHelper.Format(invoice.RoundingAmount, currency)
                : "—",
            TransportFeeText = invoice.TransportFeeAmount > 0
                ? AccountingCurrencyHelper.Format(invoice.TransportFeeAmount, currency)
                : null,
            GrandTotalText = AccountingCurrencyHelper.Format(invoice.NetAmount, currency),
            CompanyFeeText = companyFee > 0
                ? AccountingCurrencyHelper.Format(companyFee, currency)
                : null,
            HasCreditInfo = invoice.PaymentMethod == PaymentMethod.Credit,
            PaidAmountText = invoice.PaymentMethod == PaymentMethod.Credit
                ? AccountingCurrencyHelper.Format(invoice.PaidAmount, currency)
                : null,
            RemainingAmountText = invoice.PaymentMethod == PaymentMethod.Credit
                ? AccountingCurrencyHelper.Format(invoice.RemainingAmount, currency)
                : null,
            CreditStatusText = invoice.PaymentMethod == PaymentMethod.Credit
                ? invoice.IsCreditPaid ? "مسددة بالكامل" : "غير مسددة"
                : null,
            HasInstallments = plan is not null,
            InstallmentSummaryText = plan is null
                ? null
                : $"{plan.NumberOfInstallments} قسط × {AccountingCurrencyHelper.Format(plan.InstallmentAmount, currency)}",
            Items = new(invoice.Items.OrderBy(i => i.Id).Select((item, index) => new InvoiceDetailItemRow
            {
                Number = index + 1,
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(
                    item.ItemName,
                    item.CustomFieldsJson),
                QuantityText = item.Quantity.ToString("N0"),
                UnitPriceText = FormatAmount(item.UnitPrice, currency),
                TotalPriceText = FormatAmount(item.TotalPrice, currency)
            })),
            Installments = installmentRows
        };
    }

    private static string FormatAmount(decimal amount, AccountingCurrency currency) =>
        amount.ToString(currency == AccountingCurrency.USD ? "N2" : "N0");

    private static string PaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Credit => "آجل",
        PaymentMethod.Installment => "أقساط",
        _ => method.ToString()
    };
}
