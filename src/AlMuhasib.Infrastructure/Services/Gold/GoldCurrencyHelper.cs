using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Infrastructure.Services.Gold;

internal static class GoldCurrencyHelper
{
    public static decimal ConvertAmount(
        decimal amount,
        GoldCurrency from,
        GoldCurrency to,
        decimal fxRate)
    {
        if (from == to)
            return amount;

        var fx = fxRate <= 0 ? 1m : fxRate;
        return from == GoldCurrency.USD
            ? amount * fx
            : amount / fx;
    }

    public static void ApplyDualTotals(GoldInvoice invoice)
    {
        var fx = invoice.FxRate <= 0 ? 1m : invoice.FxRate;
        if (invoice.PricingCurrency == GoldCurrency.USD)
        {
            invoice.TotalAmountUsd = invoice.TotalAmount;
            invoice.TotalAmountIqd = Round(invoice.TotalAmount * fx);
        }
        else
        {
            invoice.TotalAmountIqd = invoice.TotalAmount;
            invoice.TotalAmountUsd = Round(invoice.TotalAmount / fx);
        }
    }

    public static GoldInvoiceStatus ResolveStatus(decimal totalAmount, decimal paidAmount, GoldPaymentMethod method)
    {
        if (paidAmount <= 0)
            return method == GoldPaymentMethod.Credit ? GoldInvoiceStatus.Open : GoldInvoiceStatus.Completed;

        if (paidAmount + 0.0001m >= totalAmount)
            return GoldInvoiceStatus.Completed;

        return GoldInvoiceStatus.PartiallyPaid;
    }

    public static void ApplyStockDelta(
        GoldStockBalance balance,
        decimal gramsDelta,
        decimal? costPerGram)
    {
        if (gramsDelta > 0 && costPerGram.HasValue)
        {
            var incoming = gramsDelta;
            var newTotal = balance.GramsOnHand + incoming;
            if (newTotal > 0)
            {
                balance.AverageCostPerGram = Round(
                    ((balance.GramsOnHand * balance.AverageCostPerGram) + (incoming * costPerGram.Value)) / newTotal,
                    6);
            }

            balance.GramsOnHand = Round(newTotal, 4);
            return;
        }

        balance.GramsOnHand = Round(balance.GramsOnHand + gramsDelta, 4);
        if (balance.GramsOnHand < 0)
            balance.GramsOnHand = 0;
    }

    public static GoldInvoiceListItem ToListItem(GoldInvoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        InvoiceDate = invoice.InvoiceDate,
        InvoiceType = invoice.InvoiceType,
        PaymentMethod = invoice.PaymentMethod,
        Status = invoice.Status,
        CustomerName = invoice.Customer?.Name,
        PricingCurrency = invoice.PricingCurrency,
        PaymentCurrency = invoice.PaymentCurrency,
        TotalWeightGrams = invoice.TotalWeightGrams,
        TotalAmount = invoice.TotalAmount,
        TotalAmountIqd = invoice.TotalAmountIqd,
        TotalAmountUsd = invoice.TotalAmountUsd,
        PaidAmount = invoice.PaidAmount,
        RemainingAmount = invoice.RemainingAmount,
        Notes = invoice.Notes
    };

    public static decimal Round(decimal value, int decimals = 4) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
