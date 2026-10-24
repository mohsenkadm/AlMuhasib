using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Services.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsOutstandingStatementPrintTests
{
    [Fact]
    public async Task Outstanding_claim_print_matches_company_debt_form()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("شركة سيدرال");
        var statements = new CustomsStatementService(h.Factory, h.Permissions);

        var invRequest = CustomsTestHarness.InvoiceRequest(
            customer.Id, AccountingCurrency.USD, 64_800m, fxRate: 1500m);
        invRequest.ServiceType = CustomsServiceType.Transport;
        invRequest.TransportKind = CustomsTransportKind.External;
        invRequest.Description = "حساب نقل";
        invRequest.Vehicles =
        [
            new CustomsInvoiceVehicleDraft { SyrianPlate = "1", VehiclePrice = 9_300m },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "2", VehiclePrice = 9_300m },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "3", VehiclePrice = 9_300m },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "4", VehiclePrice = 9_300m },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "5", VehiclePrice = 9_300m },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "6", VehiclePrice = 9_300m },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "7", VehiclePrice = 9_000m }
        ];
        var inv = await h.Invoices.CreateAsync(invRequest);
        await h.Invoices.ApproveAsync(inv.Id, "admin");
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 50_000m, AccountingCurrency.USD, fxRate: 1500m));

        var claim = await statements.GetOutstandingClaimAsync(customer.Id);
        Assert.Single(claim.Invoices);
        Assert.Equal(64_800m, claim.Invoices[0].TotalAmount);
        Assert.Equal(50_000m, claim.Invoices[0].PaidAmount);
        Assert.Equal(14_800m, claim.Invoices[0].RemainingAmount);
        Assert.Equal(7, claim.Invoices[0].VehicleCount);
        Assert.NotEmpty(claim.Invoices[0].ReceiptNumbers);

        var model = h.Print.BuildOutstandingBalanceStatement(claim);
        Assert.True(model.IsCustomsOutstandingStatement);
        Assert.NotNull(model.CustomsOutstandingStatement);
        Assert.Contains("مطالبة بدين متبقي", model.CustomsOutstandingStatement!.Title);
        Assert.Contains("المحترمين", model.CustomsOutstandingStatement.CustomerName);
        Assert.Contains(inv.InvoiceNumber, model.CustomsOutstandingStatement.InvoiceReferenceText);
        Assert.Equal("دين متبقي مستحق الدفع", model.CustomsOutstandingStatement.AccountStatusText);
        Assert.Single(model.CustomsOutstandingStatement.Rows);
        Assert.Equal("$64,800.00", model.CustomsOutstandingStatement.Rows[0].InvoiceTotalDisplay);
        Assert.Equal("$50,000.00", model.CustomsOutstandingStatement.Rows[0].PaidAmountDisplay);
        Assert.Equal("$14,800.00", model.CustomsOutstandingStatement.Rows[0].RemainingAmountDisplay);
        Assert.Contains("دولار أمريكي", model.CustomsOutstandingStatement.AmountInWords);
        Assert.Contains("USD", model.CustomsOutstandingStatement.BalanceDueDisplay);
        Assert.Contains("وصل", model.CustomsOutstandingStatement.PaidColumnHeader);

        h.Print.PrintCustomerStatement(model);
        Assert.Equal(1, h.Export.PrintStatementCalls);
        Assert.True(h.Export.LastStatement!.IsCustomsOutstandingStatement);

        var pdf = h.Print.ExportCustomerStatementPdf(model);
        Assert.False(string.IsNullOrWhiteSpace(pdf));
        Assert.Equal(1, h.Export.ExportStatementPdfCalls);
    }

    [Fact]
    public async Task Outstanding_claim_empty_when_fully_paid()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var statements = new CustomsStatementService(h.Factory, h.Permissions);

        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 100_000m, AccountingCurrency.IQD));

        var claim = await statements.GetOutstandingClaimAsync(customer.Id);
        Assert.Empty(claim.Invoices);
    }
}
