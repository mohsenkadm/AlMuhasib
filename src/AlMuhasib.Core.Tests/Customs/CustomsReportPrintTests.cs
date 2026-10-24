using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsReportPrintTests
{
    [Fact]
    public void Report_print_uses_customs_branded_form()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var cols = new[] { "البند", "الكمية", "المبلغ", "الحالة", "ملاحظات" };
        IList<object[]> rows =
        [
            new object[] { "تخليص", "3", "1,500,000", "مفتوح", "—" },
            new object[] { "نقل", "2", "800,000", "مسدد", "—" }
        ];
        IList<string> summary = ["الإجمالي: 2,300,000 د.ع", "عدد الصفوف: 2"];

        h.Print.PrintReportTable(
            CustomsScreenNames.Reports,
            "التقرير اليومي — التخليص",
            cols,
            rows,
            summary,
            CustomsPrintDocumentKind.DailyReport,
            fromDate: new DateTime(2026, 10, 1),
            toDate: new DateTime(2026, 10, 25),
            partyName: "الكل");

        Assert.Equal(1, h.Export.PrintStatementCalls);
        var model = h.Export.LastStatement;
        Assert.NotNull(model);
        Assert.True(model!.IsCustomsReport);
        Assert.NotNull(model.CustomsReport);
        Assert.Equal(CustomsPrintDocumentKind.DailyReport, model.CustomsReport!.Kind);
        Assert.Equal("التقرير اليومي — التخليص", model.CustomsReport.Title);
        Assert.Equal("التقرير اليومي", model.CustomsReport.Subtitle);
        Assert.Equal("2026/10/01 — 2026/10/25", model.CustomsReport.PeriodLabel);
        Assert.Equal(2, model.CustomsReport.Rows.Count);
        Assert.Equal(2, model.CustomsReport.SummaryLines.Count);
        Assert.True(model.CustomsReport.UseLandscape);
        Assert.StartsWith("CR-", model.CustomsReport.DocumentNumber);
    }

    [Theory]
    [InlineData(CustomsPrintDocumentKind.DebtsReport)]
    [InlineData(CustomsPrintDocumentKind.InvoiceReport)]
    [InlineData(CustomsPrintDocumentKind.CustomerReport)]
    [InlineData(CustomsPrintDocumentKind.ProfitReport)]
    [InlineData(CustomsPrintDocumentKind.MonthlyReport)]
    [InlineData(CustomsPrintDocumentKind.VehicleReport)]
    [InlineData(CustomsPrintDocumentKind.CustomersList)]
    [InlineData(CustomsPrintDocumentKind.InvoicesList)]
    [InlineData(CustomsPrintDocumentKind.ReceiptsList)]
    public void Report_export_pdf_marks_customs_form(CustomsPrintDocumentKind kind)
    {
        var h = CustomsTestHarness.CreateAdmin();
        var model = new StatementPrintModel
        {
            Title = CustomsPrintDocumentKindDisplay.ToArabic(kind),
            PartyName = "نظام التخليص الكمركي",
            FromDate = DateTime.Today.AddDays(-7),
            ToDate = DateTime.Today,
            Columns = ["أ", "ب", "ج", "د", "هـ"],
            Rows = [new object[] { "1", "2", "3", "4", "5" }],
            SummaryLines = ["ملخص"]
        };

        var screen = kind == CustomsPrintDocumentKind.ProfitReport
            ? CustomsScreenNames.ProfitReport
            : CustomsScreenNames.Reports;

        var path = h.Print.ExportReportPdf(screen, model, kind);
        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.Equal(1, h.Export.ExportStatementPdfCalls);
        Assert.True(h.Export.LastStatement!.IsCustomsReport);
        Assert.Equal(kind, h.Export.LastStatement.CustomsReport!.Kind);
        Assert.Equal(CustomsPrintDocumentKindDisplay.ToArabic(kind), h.Export.LastStatement.CustomsReport.Subtitle);
    }

    [Fact]
    public void List_print_paths_use_dedicated_document_kinds()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var cols = new[] { "الاسم", "الهاتف", "الرصيد", "الحالة", "ملاحظات" };
        IList<object[]> rows = [new object[] { "عميل", "07", "0", "نشط", "" }];

        h.Print.PrintReportTable(
            CustomsScreenNames.Customers,
            "قائمة عملاء التخليص الكمركي",
            cols,
            rows,
            kind: CustomsPrintDocumentKind.CustomersList);
        Assert.Equal(CustomsPrintDocumentKind.CustomersList, h.Export.LastStatement!.CustomsReport!.Kind);

        h.Print.PrintReportTable(
            CustomsScreenNames.Invoices,
            "فواتير التخليص الكمركي",
            cols,
            rows,
            kind: CustomsPrintDocumentKind.InvoicesList,
            fromDate: DateTime.Today.AddMonths(-1),
            toDate: DateTime.Today);
        Assert.Equal(CustomsPrintDocumentKind.InvoicesList, h.Export.LastStatement!.CustomsReport!.Kind);

        h.Print.PrintReportTable(
            CustomsScreenNames.Receipts,
            "وصولات القبض — التخليص الكمركي",
            cols,
            rows,
            kind: CustomsPrintDocumentKind.ReceiptsList);
        Assert.Equal(CustomsPrintDocumentKind.ReceiptsList, h.Export.LastStatement!.CustomsReport!.Kind);
        Assert.Equal(3, h.Export.PrintStatementCalls);
    }
}
