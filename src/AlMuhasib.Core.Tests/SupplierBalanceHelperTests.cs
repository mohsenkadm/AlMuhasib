using AlMuhasib.Core;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class SupplierBalanceHelperTests
{
    [Fact]
    public void ComputeOutstandingPayables_SubtractsUnappliedPayments()
    {
        var balance = SupplierBalanceHelper.ComputeOutstandingPayables(
            creditInvoiceRemaining: 1000,
            unappliedPayments: 350);

        Assert.Equal(650, balance);
    }

    [Fact]
    public void ComputeOutstandingPayables_NeverNegative()
    {
        var balance = SupplierBalanceHelper.ComputeOutstandingPayables(200, 500);
        Assert.Equal(0, balance);
    }

    [Fact]
    public void AllocateToPurchaseInvoices_AppliesFifo()
    {
        var invoices = new List<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)>
        {
            (1, new DateTime(2026, 1, 1), 800, 0, 800),
            (2, new DateTime(2026, 2, 1), 400, 0, 400),
        };

        var updates = SupplierBalanceHelper.AllocateToPurchaseInvoices(invoices, 1000);

        Assert.Equal(2, updates.Count);
        Assert.Equal(1, updates[0].Id);
        Assert.Equal(0, updates[0].RemainingAmount);
        Assert.True(updates[0].IsCreditPaid);
        Assert.Equal(2, updates[1].Id);
        Assert.Equal(200, updates[1].PaidAmount);
        Assert.Equal(200, updates[1].RemainingAmount);
        Assert.False(updates[1].IsCreditPaid);
    }

    [Fact]
    public void ApplyUnappliedPaymentsToAgingRows_ReducesFifoPerSupplier()
    {
        var rows = new[]
        {
            new AgingStub { SupplierId = 1, Remaining = 500 },
            new AgingStub { SupplierId = 1, Remaining = 300 },
            new AgingStub { SupplierId = 2, Remaining = 200 },
        };

        var payments = new[]
        {
            (SupplierKey: 1, Amount: 600m),
            (SupplierKey: 2, Amount: 50m),
        };

        var result = SupplierBalanceHelper.ApplyUnappliedPaymentsToAgingRows(
            rows,
            payments,
            r => r.SupplierId,
            r => r.Remaining,
            (r, rem) => new AgingStub { SupplierId = r.SupplierId, Remaining = rem });

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].SupplierId);
        Assert.Equal(200, result[0].Remaining); // 500+300=800 − 600 → 200 on second invoice
        Assert.Equal(2, result[1].SupplierId);
        Assert.Equal(150, result[1].Remaining);
    }

    [Fact]
    public void MarkPaymentApplied_IsIdempotent()
    {
        var once = SupplierBalanceHelper.MarkPaymentApplied("دفع مورد");
        var twice = SupplierBalanceHelper.MarkPaymentApplied(once);
        Assert.Equal(once, twice);
        Assert.True(SupplierBalanceHelper.IsPaymentApplied(twice));
    }

    private sealed class AgingStub
    {
        public int SupplierId { get; init; }
        public decimal Remaining { get; init; }
    }
}
