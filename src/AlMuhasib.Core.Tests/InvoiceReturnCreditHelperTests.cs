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

    [Fact]
    public void ResolveReturnSettlement_Cash_UsesLeftoverOnly_NotFullNet()
    {
        // مرتجع نقدي 1000 مع تطبيق 600 على آجل → الصندوق +400 فقط
        var (cash, paid, remaining, paidOff) =
            InvoiceReturnCreditHelper.ResolveReturnSettlement(PaymentMethod.Cash, 1000, 600, 1000);

        Assert.Equal(400, cash);
        Assert.Equal(1000, paid);
        Assert.Equal(0, remaining);
        Assert.True(paidOff);
    }

    [Fact]
    public void ResolveReturnSettlement_Credit_DoesNotMoveCashEqualToAppliedAp()
    {
        // مرتجع آجل 1000 بدون دفعة نقدية، يُطبَّق كاملاً على آجل → لا حركة صندوق
        var (cash, paid, remaining, paidOff) =
            InvoiceReturnCreditHelper.ResolveReturnSettlement(PaymentMethod.Credit, 1000, 1000, 0);

        Assert.Equal(0, cash);
        Assert.Equal(0, paid);
        Assert.Equal(0, remaining);
        Assert.True(paidOff);
    }

    [Fact]
    public void ResolveReturnSettlement_Credit_LeftoverBecomesCreditNote_CashClamped()
    {
        // مرتجع آجل 1000، تطبيق 600، طلب نقد 200 → نقد 200 ورصيد دائن 200
        var (cash, paid, remaining, paidOff) =
            InvoiceReturnCreditHelper.ResolveReturnSettlement(PaymentMethod.Credit, 1000, 600, 200);

        Assert.Equal(200, cash);
        Assert.Equal(200, paid);
        Assert.Equal(200, remaining);
        Assert.False(paidOff);
    }

    [Fact]
    public void ResolveReturnSettlement_Credit_IgnoresCashWhenFullyAppliedToAp()
    {
        // لا يُسمح باستلام نقد مع تخفيض كامل للذمم بنفس المبلغ
        var (cash, paid, remaining, _) =
            InvoiceReturnCreditHelper.ResolveReturnSettlement(PaymentMethod.Credit, 1000, 1000, 200);

        Assert.Equal(0, cash);
        Assert.Equal(0, paid);
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void ResolveStoredReturnCashMovement_CashUsesAllocationsLeftover()
    {
        var notes = InvoiceReturnCreditHelper.MarkApplied(null, [(1, 600m)]);
        var cash = InvoiceReturnCreditHelper.ResolveStoredReturnCashMovement(
            PaymentMethod.Cash, 1000, 1000, notes);
        Assert.Equal(400, cash);
    }

    [Fact]
    public void UserScenario_CreditPurchase_ThenCashReturn_ThenCreditReturn_ReducesApNotIncreases()
    {
        // شراء آجل 390_000 → مرتجع نقدي 13_000 → مرتجع آجل 13_000
        // الصحيح: 390 → 377 → 364  |  الخطأ الذي ظهر للمستخدم: 390 ثم 403
        const decimal purchase = 390_000m;
        const decimal ret = 13_000m;

        var open = new List<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)>
        {
            (1, new DateTime(2026, 3, 1), purchase, 0, purchase),
        };

        var cashAlloc = InvoiceReturnCreditHelper.AllocateReturnToCreditInvoices(open, ret, preferredInvoiceId: 1);
        Assert.Single(cashAlloc);
        Assert.Equal(ret, cashAlloc[0].Applied);
        Assert.Equal(purchase - ret, cashAlloc[0].RemainingAmount);

        var cashSettle = InvoiceReturnCreditHelper.ResolveReturnSettlement(
            PaymentMethod.Cash, ret, cashAlloc[0].Applied, requestedCashPortion: ret);
        Assert.Equal(0, cashSettle.CashMovement); // لا نقد من المورد طالما خُفضت الذمة
        Assert.Equal(0, cashSettle.RemainingAmount);

        open[0] = (1, open[0].Date, purchase, cashAlloc[0].PaidAmount, cashAlloc[0].RemainingAmount);

        var creditAlloc = InvoiceReturnCreditHelper.AllocateReturnToCreditInvoices(open, ret, preferredInvoiceId: 1);
        Assert.Single(creditAlloc);
        Assert.Equal(purchase - ret - ret, creditAlloc[0].RemainingAmount);

        var creditSettle = InvoiceReturnCreditHelper.ResolveReturnSettlement(
            PaymentMethod.Credit, ret, creditAlloc[0].Applied, requestedCashPortion: 0);
        Assert.Equal(0, creditSettle.CashMovement);
        Assert.Equal(0, creditSettle.RemainingAmount);

        var outstanding = SupplierBalanceHelper.ComputeOutstandingPayables(
            creditAlloc[0].RemainingAmount, unappliedPayments: 0);
        Assert.Equal(364_000m, outstanding);
        Assert.NotEqual(403_000m, outstanding);
    }

    [Fact]
    public void CustomerScenario_CreditSale_ThenCashReturn_ThenCreditReturn_ReducesAr()
    {
        const decimal sale = 390_000m;
        const decimal ret = 13_000m;

        var open = new List<(int Id, DateTime Date, decimal NetAmount, decimal PaidAmount, decimal RemainingAmount)>
        {
            (1, new DateTime(2026, 3, 1), sale, 0, sale),
        };

        var first = InvoiceReturnCreditHelper.AllocateReturnToCreditInvoices(open, ret);
        open[0] = (1, open[0].Date, sale, first[0].PaidAmount, first[0].RemainingAmount);
        var second = InvoiceReturnCreditHelper.AllocateReturnToCreditInvoices(open, ret);

        var balance = CustomerBalanceHelper.ComputeOutstandingBalance(
            second[0].RemainingAmount,
            unpaidInstallmentRemaining: 0,
            unappliedDebtReceipts: 0,
            receiptAdvances: 0);
        Assert.Equal(364_000m, balance);
    }
}
