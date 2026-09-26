using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class InvoiceReturnCreditHelperTests
{
    [Fact]
    public void AllocateReturnToCreditInvoices_PrefersRelatedThenFifo()
    {
        var invoices = new List<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)>
        {
            (1, new DateTime(2026, 1, 1), 10_000_000, 0, 10_000_000),
            (2, new DateTime(2026, 2, 1), 19_000_000, 0, 19_000_000),
        };

        var updates = InvoiceReturnCreditHelper.AllocateReturnToCreditInvoices(
            invoices, 19_000_000, preferredInvoiceId: 2);

        Assert.Single(updates);
        Assert.Equal(2, updates[0].Id);
        Assert.Equal(0, updates[0].RemainingAmount);
        Assert.True(updates[0].IsCreditPaid);
        Assert.Equal(19_000_000, updates[0].Applied);
    }

    [Fact]
    public void AllocateReturnToCreditInvoices_PartialLeavesRemainderUnapplied()
    {
        var invoices = new List<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)>
        {
            (1, new DateTime(2026, 1, 1), 5_000_000, 0, 5_000_000),
        };

        var updates = InvoiceReturnCreditHelper.AllocateReturnToCreditInvoices(invoices, 8_000_000);
        Assert.Single(updates);
        Assert.Equal(0, updates[0].RemainingAmount);
        Assert.Equal(5_000_000, updates[0].Applied);
    }

    [Fact]
    public void MarkAndParseAllocations_RoundTrip()
    {
        var notes = InvoiceReturnCreditHelper.MarkApplied("مرتجع", [(42, 1500.5m)]);
        Assert.True(InvoiceReturnCreditHelper.IsReturnApplied(notes));
        var parsed = InvoiceReturnCreditHelper.ParseAllocations(notes);
        Assert.Single(parsed);
        Assert.Equal(42, parsed[0].InvoiceId);
        Assert.Equal(1500.5m, parsed[0].Amount);
    }

    [Theory]
    [InlineData(InvoiceType.SaleReturn, InvoiceType.Sale)]
    [InlineData(InvoiceType.PurchaseReturn, InvoiceType.Purchase)]
    public void GetOriginalInvoiceType_MapsReturns(InvoiceType ret, InvoiceType expected)
        => Assert.Equal(expected, InvoiceReturnCreditHelper.GetOriginalInvoiceType(ret));
}
