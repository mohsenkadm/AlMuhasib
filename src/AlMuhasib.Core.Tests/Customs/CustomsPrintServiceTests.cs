using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsPrintServiceTests
{
    [Fact]
    public async Task BuildCustomerInvoice_omits_expenses_and_internal_notes()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var expenseTypeId = await h.SeedExpenseTypeAsync();

        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id,
                AccountingCurrency.IQD,
                200_000m,
                expenseTypeId: expenseTypeId,
                expenseAmount: 25_000m));
        inv.InternalNotes = "ربح داخلي سري";
        inv.CustomerNotes = "ملاحظات للعميل";

        var model = h.Print.BuildCustomerInvoice(inv, customer.Phone);
        Assert.Equal(200_000m, model.GrandTotal);
        Assert.Single(model.Items);
        Assert.Equal("ملاحظات للعميل", model.Notes);
        Assert.DoesNotContain(model.Items, i => i.ItemName.Contains("مصروف", StringComparison.Ordinal));
        Assert.DoesNotContain(model.Items, i => (i.ItemName ?? "").Contains("ربح", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuildReceipt_and_export_with_permissions()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");
        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 40_000m, AccountingCurrency.IQD));

        var voucher = h.Print.BuildReceipt(receipt);
        Assert.Equal(40_000m, voucher.Amount);
        Assert.Equal(customer.Name, voucher.PartyName);

        var path = h.Print.ExportReceiptPdf(receipt);
        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.Equal(1, h.Export.ExportVoucherPdfCalls);

        h.Print.PrintCustomerInvoice(inv, customer.Phone);
        Assert.Equal(1, h.Export.PrintInvoiceCalls);

        var pdf = h.Print.ExportCustomerInvoicePdf(inv, customer.Phone);
        Assert.False(string.IsNullOrWhiteSpace(pdf));
        Assert.Equal(1, h.Export.ExportInvoicePdfCalls);
    }

    [Fact]
    public void Print_and_export_denied_without_permission()
    {
        var h = CustomsTestHarness.CreateUser(u =>
            u.Grant(CustomsScreenNames.Invoices, view: true));

        var detail = new CustomsInvoiceDetail
        {
            InvoiceNumber = "CI-1",
            CustomerName = "x",
            Currency = AccountingCurrency.IQD,
            TotalAmount = 1,
            ServiceType = CustomsServiceType.Clearance,
            Vehicles = [new CustomsInvoiceVehicleDraft { VehiclePrice = 1 }]
        };

        Assert.Throws<UnauthorizedAccessException>(() => h.Print.PrintCustomerInvoice(detail));
        Assert.Throws<UnauthorizedAccessException>(() => h.Print.ExportCustomerInvoicePdf(detail));
    }

    [Fact]
    public void H04_report_print_export_denied_without_permission()
    {
        var h = CustomsTestHarness.CreateUser(u =>
            u.Grant(CustomsScreenNames.Reports, view: true));

        var model = new StatementPrintModel
        {
            Title = "تقرير",
            PartyName = "x",
            Columns = ["أ"],
            Rows = [new object[] { "1" }]
        };

        Assert.Throws<UnauthorizedAccessException>(() =>
            h.Print.PrintReportTable(CustomsScreenNames.Reports, "تقرير", model.Columns, model.Rows));
        Assert.Throws<UnauthorizedAccessException>(() =>
            h.Print.ExportReportPdf(CustomsScreenNames.Reports, model));
        Assert.Throws<UnauthorizedAccessException>(() =>
            h.Print.ExportReportExcel(
                CustomsScreenNames.Reports,
                Path.Combine(Path.GetTempPath(), $"customs-deny-{Guid.NewGuid():N}.xlsx"),
                "Sheet",
                model.Columns,
                model.Rows));

        Assert.Throws<UnauthorizedAccessException>(() =>
            h.Print.PrintReportTable(CustomsScreenNames.ProfitReport, "ربح", model.Columns, model.Rows));
    }

    [Fact]
    public void H04_report_print_export_allowed_with_permission()
    {
        var h = CustomsTestHarness.CreateUser(u =>
        {
            u.Grant(CustomsScreenNames.Reports, view: true, print: true, export: true);
            u.Grant(CustomsScreenNames.ProfitReport, view: true, print: true, export: true);
        });

        var cols = new[] { "أ" };
        IList<object[]> rows = [new object[] { "1" }];
        var model = new StatementPrintModel
        {
            Title = "تقرير",
            PartyName = "x",
            Columns = cols,
            Rows = rows
        };

        h.Print.PrintReportTable(CustomsScreenNames.Reports, "تقرير", cols, rows);
        Assert.Equal(1, h.Export.PrintTableCalls);

        var pdf = h.Print.ExportReportPdf(CustomsScreenNames.Reports, model);
        Assert.False(string.IsNullOrWhiteSpace(pdf));
        Assert.Equal(1, h.Export.ExportStatementPdfCalls);

        var excelPath = Path.Combine(Path.GetTempPath(), $"customs-ok-{Guid.NewGuid():N}.xlsx");
        h.Print.ExportReportExcel(CustomsScreenNames.Reports, excelPath, "Sheet", cols, rows);
        Assert.Equal(1, h.Export.ExportExcelCalls);

        h.Print.PrintReportTable(CustomsScreenNames.ProfitReport, "ربح", cols, rows);
        Assert.Equal(2, h.Export.PrintTableCalls);
    }
}
