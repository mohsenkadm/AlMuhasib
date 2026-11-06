using System.Globalization;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Utilities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

/// <summary>مولّدات PDF للسندات وحركات المستثمرين وكشوف الحساب.</summary>
internal static class DocumentPdfGenerator
{
    private static bool _licenseSet;

    private static readonly string[] ArabicMonths =
    [
        "", "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران",
        "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"
    ];

    private static void EnsureLicense()
    {
        if (_licenseSet) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseSet = true;
    }

    public static byte[] GenerateVoucher(VoucherPrintModel m)
    {
        EnsureLicense();
        var branding = PrintBrandingProvider.Current;
        var currency = string.IsNullOrWhiteSpace(m.CurrencyLabel) ? "د.ع" : m.CurrencyLabel.Trim();
        var isUsd = currency.Contains('$')
                    || currency.Contains("دولار", StringComparison.Ordinal);
        var amountDisplay = FormatMoney(m.Amount, currency, isUsd);
        var amountInWords = BuildAmountInWords(m.Amount, amountDisplay, isUsd);
        var title = string.IsNullOrWhiteSpace(m.Title) ? m.VoucherTypeLabel : m.Title;
        if (string.IsNullOrWhiteSpace(title))
            title = "سند";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor("#212121"));
                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    AppendBrandingHeader(col, branding);

                    col.Item().PaddingTop(8).Background("#EEF1F5").Border(0.7f).BorderColor("#90A4AE").Padding(10).Row(row =>
                    {
                        row.RelativeItem(1.2f).AlignCenter().AlignMiddle()
                            .Text(title).Bold().FontSize(14);
                        row.RelativeItem().Column(meta =>
                        {
                            meta.Item().Text(t =>
                            {
                                t.Span("رقم السند: ").SemiBold().FontColor("#546E7A");
                                t.Span(m.VoucherNumber).SemiBold();
                            });
                            meta.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("التاريخ: ").SemiBold().FontColor("#546E7A");
                                t.Span(FormatArabicDate(m.Date)).SemiBold();
                            });
                        });
                    });

                    col.Item().PaddingTop(10).Background("#EEF1F5").Border(0.8f).BorderColor("#90A4AE").Padding(12).Column(box =>
                    {
                        if (!string.IsNullOrWhiteSpace(m.PartyName))
                        {
                            var partyLabel = string.IsNullOrWhiteSpace(m.PartyLabel) ? "الطرف" : m.PartyLabel!;
                            box.Item().Text(t =>
                            {
                                t.Span($"{partyLabel}: ").SemiBold().FontColor("#546E7A");
                                t.Span(m.PartyName).SemiBold().FontSize(11);
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(m.PartyPhone))
                        {
                            box.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("الهاتف: ").SemiBold().FontColor("#546E7A");
                                t.Span(m.PartyPhone).SemiBold();
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(m.VoucherTypeLabel))
                        {
                            box.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("نوع السند: ").SemiBold().FontColor("#546E7A");
                                t.Span(m.VoucherTypeLabel).SemiBold();
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(m.CashBoxName))
                        {
                            box.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("القاصة: ").SemiBold().FontColor("#546E7A");
                                t.Span(m.CashBoxName).SemiBold();
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(m.BankAccountName))
                        {
                            box.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("المصرف: ").SemiBold().FontColor("#546E7A");
                                t.Span(m.BankAccountName).SemiBold();
                            });
                        }

                        box.Item().PaddingTop(10).Row(r =>
                        {
                            r.AutoItem().AlignMiddle().Text("مبلغ وقدره: ").SemiBold().FontColor("#546E7A");
                            r.RelativeItem().Background("#FFF8E1").Border(1).BorderColor("#F9A825").Padding(6)
                                .Text(amountInWords).Bold().FontSize(11);
                        });

                        if (m.BankFees > 0)
                        {
                            var feesDisplay = FormatMoney(m.BankFees, currency, isUsd);
                            var netDisplay = FormatMoney(m.Amount - m.BankFees, currency, isUsd);
                            box.Item().PaddingTop(8).Text(t =>
                            {
                                t.Span("عمولة المصرف: ").SemiBold().FontColor("#546E7A");
                                t.Span(feesDisplay).SemiBold();
                            });
                            box.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("صافي المبلغ: ").SemiBold().FontColor("#546E7A");
                                t.Span(netDisplay).Bold().FontColor("#2E7D32");
                            });
                        }
                    });

                    col.Item().PaddingTop(12).Background("#1B5E20").Padding(12).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle()
                            .Text("إجمالي مبلغ السند:")
                            .Bold().FontSize(11).FontColor(Colors.White);
                        row.ConstantItem(160).AlignMiddle().AlignLeft()
                            .Text(amountDisplay)
                            .Bold().FontSize(15).FontColor(Colors.White);
                    });

                    if (!string.IsNullOrWhiteSpace(m.Notes))
                    {
                        col.Item().PaddingTop(10).Background("#F5F5F5").Border(0.7f).BorderColor("#90A4AE").Padding(10).Text(t =>
                        {
                            t.Span("ملاحظات: ").Bold().FontColor("#C62828");
                            t.Span(m.Notes);
                        });
                    }

                    var receiver = string.IsNullOrWhiteSpace(branding.CompanyName)
                        ? "المستلم:"
                        : $"المستلم ({branding.CompanyName}):";

                    col.Item().PaddingTop(22).Row(row =>
                    {
                        row.RelativeItem().Border(0.7f).BorderColor("#90A4AE").Padding(10).AlignCenter().Column(c =>
                        {
                            c.Item().Text(receiver).SemiBold();
                            c.Item().PaddingTop(20).Text("________________").FontColor("#546E7A");
                        });
                        row.ConstantItem(12);
                        row.RelativeItem().Border(0.7f).BorderColor("#90A4AE").Padding(10).AlignCenter().Column(c =>
                        {
                            c.Item().Text("المحاسب:").SemiBold();
                            c.Item().PaddingTop(20).Text("________________").FontColor("#546E7A");
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static string FormatMoney(decimal amount, string currencyLabel, bool isUsd) =>
        isUsd
            ? $"{amount:N2} {currencyLabel}"
            : $"{amount:N0} {currencyLabel}";

    private static string BuildAmountInWords(decimal amount, string amountDisplay, bool isUsd)
    {
        var wordsCurrency = isUsd ? "دولار أمريكي" : "دينار عراقي";
        var fraction = isUsd ? "سنت" : "فلس";
        var rawWords = ArabicAmountToWords.Convert(amount, wordsCurrency, fraction);
        var coreWords = rawWords
            .Replace(" فقط لا غير", string.Empty, StringComparison.Ordinal)
            .Trim();
        return $"{amountDisplay} ({coreWords} لا غير)";
    }

    private static void AppendBrandingHeader(ColumnDescriptor col, Core.Models.PrintBrandingSnapshot branding)
    {
        if (branding.ShowHeaderImage && branding.HeaderImageData is { Length: > 0 })
        {
            col.Item().MaxHeight(88).AlignCenter().Image(branding.HeaderImageData).FitWidth();
            return;
        }

        if (!branding.ShowHeaderText || string.IsNullOrWhiteSpace(branding.CompanyName))
            return;

        col.Item().Background("#15254C").Padding(10).Row(row =>
        {
            row.RelativeItem().AlignRight().Column(c =>
            {
                c.Item().Text(branding.CompanyName).FontSize(14).Bold().FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.Details))
                    c.Item().Text(branding.Details).FontSize(8).FontColor("#BBDEFB");
            });
            row.RelativeItem().AlignLeft().Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(branding.PhonePrimary))
                    c.Item().Text(branding.PhonePrimary).FontSize(8).FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.PhoneSecondary))
                    c.Item().Text(branding.PhoneSecondary).FontSize(8).FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.Address))
                    c.Item().Text(branding.Address).FontSize(8).FontColor("#BBDEFB");
            });
        });
    }

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12
            ? ArabicMonths[date.Month]
            : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year}";
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
