using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Controls;

internal static class InvoiceDetailMapper
{
    public static InvoiceDetailDisplayModel FromInvoice(
        Invoice invoice,
        string? paymentMethodOverride = null,
        decimal? companyFeeOverride = null)
    {
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
                    AmountText = $"{inst.Amount:N0} د.ع",
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
                InvoiceType.Installment => "فاتورة أقساط",
                _ => "تفاصيل الفاتورة"
            },
            InvoiceNumber = invoice.InvoiceNumber,
            DateText = invoice.Date.ToString("yyyy/MM/dd"),
            CreditDueDateText = invoice.CreditDueDate?.ToString("yyyy/MM/dd"),
            PartyLabel = invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn ? "المورد" : "العميل",
            PartyName = invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
                ? invoice.Supplier?.Name ?? "—"
                : invoice.Customer?.Name ?? "—",
            WarehouseName = invoice.Warehouse?.Name ?? "—",
            PaymentMethod = payment,
            Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? null : invoice.Notes.Trim(),
            HasNotes = !string.IsNullOrWhiteSpace(invoice.Notes),
            SubtotalText = $"{invoice.TotalAmount:N0} د.ع",
            DiscountText = invoice.DiscountAmount > 0 ? $"{invoice.DiscountAmount:N0} د.ع" : "—",
            RoundingText = invoice.RoundingAmount != 0 ? $"{invoice.RoundingAmount:N0} د.ع" : "—",
            GrandTotalText = $"{invoice.NetAmount:N0} د.ع",
            CompanyFeeText = companyFee > 0 ? $"{companyFee:N0} د.ع" : null,
            HasCreditInfo = invoice.PaymentMethod == PaymentMethod.Credit,
            PaidAmountText = invoice.PaymentMethod == PaymentMethod.Credit ? $"{invoice.PaidAmount:N0} د.ع" : null,
            RemainingAmountText = invoice.PaymentMethod == PaymentMethod.Credit ? $"{invoice.RemainingAmount:N0} د.ع" : null,
            CreditStatusText = invoice.PaymentMethod == PaymentMethod.Credit
                ? invoice.IsCreditPaid ? "مسددة بالكامل" : "غير مسددة"
                : null,
            HasInstallments = plan is not null,
            InstallmentSummaryText = plan is null
                ? null
                : $"{plan.NumberOfInstallments} قسط × {plan.InstallmentAmount:N0} د.ع",
            Items = new(invoice.Items.OrderBy(i => i.Id).Select((item, index) => new InvoiceDetailItemRow
            {
                Number = index + 1,
                ItemName = item.ItemName,
                QuantityText = item.Quantity.ToString("N0"),
                UnitPriceText = $"{item.UnitPrice:N0}",
                TotalPriceText = $"{item.TotalPrice:N0}"
            })),
            Installments = installmentRows
        };
    }

    private static string PaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Credit => "آجل",
        PaymentMethod.Installment => "أقساط",
        _ => method.ToString()
    };
}
