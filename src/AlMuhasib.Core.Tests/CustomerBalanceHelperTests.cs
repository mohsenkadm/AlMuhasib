using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class CustomerBalanceHelperTests
{
    [Fact]
    public void ComputeOutstandingBalance_SumsCreditAndInstallments_MinusAdjustments()
    {
        var balance = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditInvoiceRemaining: 1000,
            unpaidInstallmentRemaining: 500,
            unappliedDebtReceipts: 200,
            receiptAdvances: 100);

        Assert.Equal(1200, balance);
    }

    [Fact]
    public void AllocateToCreditInvoices_AppliesFifo()
    {
        var invoices = new List<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)>
        {
            (1, new DateTime(2026, 1, 1), 1000, 0, 1000),
            (2, new DateTime(2026, 2, 1), 500, 0, 500),
        };

        var updates = CustomerBalanceHelper.AllocateToCreditInvoices(invoices, 1200);

        Assert.Equal(2, updates.Count);
        Assert.Equal(1, updates[0].Id);
        Assert.Equal(1000, updates[0].PaidAmount);
        Assert.Equal(0, updates[0].RemainingAmount);
        Assert.True(updates[0].IsCreditPaid);
        Assert.Equal(2, updates[1].Id);
        Assert.Equal(200, updates[1].PaidAmount);
        Assert.Equal(300, updates[1].RemainingAmount);
        Assert.False(updates[1].IsCreditPaid);
    }

    [Fact]
    public void BuildCustomerStatementLedger_UsesPaidAmountNotDoubleCountingAppliedDebtReceipt()
    {
        var invoices = new[]
        {
            new CustomerBalanceInvoiceRow
            {
                Id = 1,
                Date = new DateTime(2026, 1, 10),
                InvoiceNumber = "S-1",
                InvoiceType = InvoiceType.Sale,
                PaymentMethod = PaymentMethod.Credit,
                NetAmount = 1000,
                PaidAmount = 400,
                RemainingAmount = 600
            }
        };

        var vouchers = new[]
        {
            new CustomerBalanceVoucherRow
            {
                Id = 9,
                Date = new DateTime(2026, 1, 15),
                VoucherNumber = "DRC1",
                VoucherType = VoucherType.DebtReceipt,
                Amount = 400,
                Notes = CustomerBalanceHelper.DebtReceiptAppliedMarker
            }
        };

        var (rows, balance) = CustomerBalanceHelper.BuildCustomerStatementLedger(
            invoices, vouchers, Array.Empty<CustomerBalanceInstallmentPaymentRow>(), 0);

        Assert.Equal(600, balance);
        Assert.Contains(rows, r => r.Debit == 1000);
        Assert.Contains(rows, r => r.Credit == 400 && r.Description.Contains("تسديد"));
        Assert.DoesNotContain(rows, r => r.Description.Contains("سند تسديد دين"));
    }

    [Fact]
    public void MarkDebtReceiptApplied_IsIdempotent()
    {
        var once = CustomerBalanceHelper.MarkDebtReceiptApplied("test");
        var twice = CustomerBalanceHelper.MarkDebtReceiptApplied(once);
        Assert.Equal(once, twice);
        Assert.True(CustomerBalanceHelper.IsDebtReceiptApplied(twice));
    }
}
