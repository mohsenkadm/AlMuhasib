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
    public void ComputeOutstandingBalances_KeepsCurrenciesSeparate()
    {
        var dual = CustomerBalanceHelper.ComputeOutstandingBalances(
            creditInvoiceRemainings: [(AccountingCurrency.IQD, 1000), (AccountingCurrency.USD, 50)],
            unpaidInstallmentRemainings: [(AccountingCurrency.IQD, 200)],
            unappliedDebtReceipts: [(AccountingCurrency.USD, 10)],
            receiptAdvances: [(AccountingCurrency.IQD, 100)]);

        Assert.Equal(1100, dual.Iqd);
        Assert.Equal(40, dual.Usd);
    }

    [Fact]
    public void AllocateToCreditInvoices_AppliesFifoSameCurrencyOnly()
    {
        var invoices = new List<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount, AccountingCurrency Currency)>
        {
            (1, new DateTime(2026, 1, 1), 1000, 0, 1000, AccountingCurrency.IQD),
            (2, new DateTime(2026, 1, 2), 50, 0, 50, AccountingCurrency.USD),
            (3, new DateTime(2026, 2, 1), 500, 0, 500, AccountingCurrency.IQD),
        };

        var updates = CustomerBalanceHelper.AllocateToCreditInvoices(invoices, 1200, AccountingCurrency.IQD);

        Assert.Equal(2, updates.Count);
        Assert.Equal(1, updates[0].Id);
        Assert.Equal(1000, updates[0].PaidAmount);
        Assert.Equal(0, updates[0].RemainingAmount);
        Assert.True(updates[0].IsCreditPaid);
        Assert.Equal(3, updates[1].Id);
        Assert.Equal(200, updates[1].PaidAmount);
        Assert.Equal(300, updates[1].RemainingAmount);
        Assert.False(updates[1].IsCreditPaid);
        Assert.DoesNotContain(updates, u => u.Id == 2);
    }

    [Fact]
    public void AllocateToCreditInvoices_LegacyOverloadAssumesIqd()
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
                RemainingAmount = 600,
                Currency = AccountingCurrency.IQD
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
                Notes = CustomerBalanceHelper.DebtReceiptAppliedMarker,
                Currency = AccountingCurrency.IQD
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
    public void BuildCustomerStatementLedger_DoesNotMixUsdIntoIqdLedger()
    {
        var invoices = new[]
        {
            new CustomerBalanceInvoiceRow
            {
                Id = 1,
                Date = new DateTime(2026, 1, 10),
                InvoiceNumber = "S-IQD",
                InvoiceType = InvoiceType.Sale,
                PaymentMethod = PaymentMethod.Credit,
                NetAmount = 1000,
                PaidAmount = 0,
                RemainingAmount = 1000,
                Currency = AccountingCurrency.IQD
            },
            new CustomerBalanceInvoiceRow
            {
                Id = 2,
                Date = new DateTime(2026, 1, 11),
                InvoiceNumber = "S-USD",
                InvoiceType = InvoiceType.Sale,
                PaymentMethod = PaymentMethod.Credit,
                NetAmount = 50,
                PaidAmount = 0,
                RemainingAmount = 50,
                Currency = AccountingCurrency.USD
            }
        };

        var (rows, balance) = CustomerBalanceHelper.BuildCustomerStatementLedger(
            invoices,
            Array.Empty<CustomerBalanceVoucherRow>(),
            Array.Empty<CustomerBalanceInstallmentPaymentRow>(),
            unpaidInstallmentRemaining: 0,
            currencyFilter: AccountingCurrency.IQD);

        Assert.Equal(1000, balance);
        Assert.Single(rows.Where(r => r.Debit > 0));
        Assert.DoesNotContain(rows, r => r.Description.Contains("S-USD"));
    }

    [Fact]
    public void BuildCustomerStatementLedger_DoesNotDoubleCountAppliedReceipt()
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
                PaidAmount = 300,
                RemainingAmount = 700
            }
        };

        var vouchers = new[]
        {
            new CustomerBalanceVoucherRow
            {
                Id = 11,
                Date = new DateTime(2026, 1, 20),
                VoucherNumber = "RCV1",
                VoucherType = VoucherType.Receipt,
                Amount = 300,
                Notes = CustomerBalanceHelper.DebtReceiptAppliedMarker
            },
            new CustomerBalanceVoucherRow
            {
                Id = 12,
                Date = new DateTime(2026, 1, 25),
                VoucherNumber = "RCV2",
                VoucherType = VoucherType.Receipt,
                Amount = 100,
                Notes = null
            }
        };

        var (rows, balance) = CustomerBalanceHelper.BuildCustomerStatementLedger(
            invoices, vouchers, Array.Empty<CustomerBalanceInstallmentPaymentRow>(), 0);

        // Remaining 700 − unapplied receipt 100 = 600 (applied receipt already in PaidAmount)
        Assert.Equal(600, balance);
        Assert.Contains(rows, r => r.Credit == 300 && r.Description.Contains("تسديد"));
        Assert.Contains(rows, r => r.Credit == 100 && r.Description.Contains("سند قبض RCV2"));
        Assert.DoesNotContain(rows, r => r.Description.Contains("سند قبض RCV1"));
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
