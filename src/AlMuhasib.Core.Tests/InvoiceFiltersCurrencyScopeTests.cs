using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class InvoiceFiltersCurrencyScopeTests
{
    [Fact]
    public void ForProfitAndSalesTotals_ScopeUsd_FiltersUsdOnly()
    {
        var invoices = new List<Invoice>
        {
            new() { Id = 1, InvoiceType = InvoiceType.Sale, Currency = AccountingCurrency.IQD, NetAmount = 100 },
            new() { Id = 2, InvoiceType = InvoiceType.Sale, Currency = AccountingCurrency.USD, NetAmount = 10 },
        }.AsQueryable();
        var plans = new List<InstallmentPlan>().AsQueryable();

        var usd = InvoiceFilters.ForProfitAndSalesTotals(invoices, plans, ReportCurrencyScope.Usd).ToList();
        Assert.Single(usd);
        Assert.Equal(2, usd[0].Id);

        var all = InvoiceFilters.ForProfitAndSalesTotals(invoices, plans, ReportCurrencyScope.All).ToList();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void ForProfitAndSalesTotals_ExcludesOpeningCreditBalances()
    {
        var invoices = new List<Invoice>
        {
            new()
            {
                Id = 1,
                InvoiceType = InvoiceType.Sale,
                Currency = AccountingCurrency.IQD,
                NetAmount = 100,
                Notes = OpeningCreditBalanceMarkers.NotesPrefix
            },
            new() { Id = 2, InvoiceType = InvoiceType.Sale, Currency = AccountingCurrency.IQD, NetAmount = 50 },
        }.AsQueryable();
        var plans = new List<InstallmentPlan>().AsQueryable();

        var rows = InvoiceFilters.ForProfitAndSalesTotals(invoices, plans, ReportCurrencyScope.Iqd).ToList();
        Assert.Single(rows);
        Assert.Equal(2, rows[0].Id);
    }
}
