using AlMuhasib.Core.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

/// <summary>مولّدات PDF للسندات وحركات المستثمرين وكشوف الحساب.</summary>
internal static class DocumentPdfGenerator
{
    private static bool _licenseSet;

    private static void EnsureLicense()
    {
        if (_licenseSet) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseSet = true;
    }

    public static byte[] GenerateVoucher(VoucherPrintModel m)
    {
        EnsureLicense();
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Teal.Darken2).Padding(10)
                        .Text(m.Title).FontSize(18).Bold().FontColor(Colors.White).AlignCenter();
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text($"رقم السند: {m.VoucherNumber}");
                        row.RelativeItem().AlignRight().Text($"التاريخ: {m.Date:yyyy/MM/dd}");
                    });
                    col.Item().PaddingTop(4).Text($"النوع: {m.VoucherTypeLabel}");
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(m.PartyName))
                    {
                        var label = string.IsNullOrWhiteSpace(m.PartyLabel) ? "الطرف" : m.PartyLabel;
                        col.Item().Text($"{label}: {m.PartyName}").FontSize(12);
                    }

                    if (!string.IsNullOrWhiteSpace(m.PartyPhone))
                        col.Item().Text($"الهاتف: {m.PartyPhone}");

                    if (!string.IsNullOrWhiteSpace(m.CashBoxName))
                        col.Item().Text($"الصندوق: {m.CashBoxName}");

                    if (!string.IsNullOrWhiteSpace(m.BankAccountName))
                        col.Item().Text($"الحساب المصرفي: {m.BankAccountName}");

                    col.Item().PaddingTop(16).Background(Colors.Grey.Lighten4).Padding(12).Column(box =>
                    {
                        box.Item().Text($"المبلغ: {m.Amount:N0} د.ع").Bold().FontSize(16).AlignCenter();
                        if (m.BankFees > 0)
                            box.Item().PaddingTop(4).Text($"عمولة المصرف: {m.BankFees:N0} د.ع").AlignCenter();
                    });

                    if (!string.IsNullOrWhiteSpace(m.Notes))
                        col.Item().PaddingTop(12).Text($"ملاحظات: {m.Notes}");
                });

                page.Footer().AlignCenter().Text("مع التحية — المحاسب");
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateInvestorTransaction(InvestorTransactionPrintModel m)
    {
        EnsureLicense();
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Indigo.Darken2).Padding(10)
                        .Text(m.Title).FontSize(18).Bold().FontColor(Colors.White).AlignCenter();
                    col.Item().PaddingTop(8).Text($"المستثمر: {m.InvestorName}").FontSize(12);
                    if (!string.IsNullOrWhiteSpace(m.InvestorPhone))
                        col.Item().Text($"الهاتف: {m.InvestorPhone}");
                    col.Item().Text($"النوع: {m.TransactionTypeLabel}");
                    col.Item().Text($"التاريخ: {m.Date:yyyy/MM/dd HH:mm}");
                    if (!string.IsNullOrWhiteSpace(m.CashBoxName))
                        col.Item().Text($"الصندوق: {m.CashBoxName}");
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(box =>
                    {
                        box.Item().Text($"المبلغ: {m.Amount:N0} د.ع").Bold().FontSize(16).AlignCenter();
                        if (m.BalanceAfter.HasValue)
                            box.Item().PaddingTop(6)
                                .Text($"الرصيد بعد العملية: {m.BalanceAfter.Value:N0} د.ع")
                                .AlignCenter();
                    });

                    if (!string.IsNullOrWhiteSpace(m.Notes))
                        col.Item().PaddingTop(12).Text($"ملاحظات: {m.Notes}");
                });

                page.Footer().AlignCenter().Text("مع التحية — المحاسب");
            });
        }).GeneratePdf();
    }

    public static byte[] GenerateStatement(StatementPrintModel m)
    {
        EnsureLicense();
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Blue.Darken2).Padding(10)
                        .Text(m.Title).FontSize(16).Bold().FontColor(Colors.White).AlignCenter();
                    col.Item().PaddingTop(6).Text(m.PartyName).FontSize(12).Bold();
                    if (!string.IsNullOrWhiteSpace(m.PartyPhone))
                        col.Item().Text($"الهاتف: {m.PartyPhone}");
                    if (m.FromDate.HasValue || m.ToDate.HasValue)
                    {
                        var from = m.FromDate?.ToString("yyyy/MM/dd") ?? "—";
                        var to = m.ToDate?.ToString("yyyy/MM/dd") ?? "—";
                        col.Item().Text($"الفترة: {from} → {to}");
                    }
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    if (m.Columns.Length == 0)
                    {
                        col.Item().Text("لا توجد بيانات.");
                        return;
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            foreach (var _ in m.Columns)
                                c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            foreach (var header in m.Columns)
                                h.Cell().Element(HeaderCell).Text(header);
                        });

                        foreach (var row in m.Rows)
                        {
                            for (var i = 0; i < m.Columns.Length; i++)
                            {
                                var value = i < row.Length ? FormatCell(row[i]) : string.Empty;
                                table.Cell().Element(BodyCell).Text(value);
                            }
                        }
                    });

                    if (m.SummaryLines is { Count: > 0 })
                    {
                        col.Item().PaddingTop(10).Column(sum =>
                        {
                            foreach (var line in m.SummaryLines)
                                sum.Item().Text(line).Bold();
                        });
                    }
                });

                page.Footer().AlignCenter()
                    .Text($"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm} — المحاسب");
            });
        }).GeneratePdf();
    }

    private static string FormatCell(object? value) => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("N0"),
        double d => d.ToString("N0"),
        float f => f.ToString("N0"),
        DateTime dt => dt.ToString("yyyy/MM/dd"),
        _ => value.ToString() ?? string.Empty
    };

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold()).Padding(3).Background(Colors.Grey.Lighten3)
            .Border(0.5f).BorderColor(Colors.Grey.Medium);

    private static IContainer BodyCell(IContainer c) =>
        c.Padding(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
}
