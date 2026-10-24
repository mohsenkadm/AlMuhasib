using System.Globalization;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// QuestPDF Customs staff/list report. Branding header from print settings at top.
/// </summary>
internal static class CustomsReportPdfGenerator
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

    public static byte[] Generate(StatementPrintModel m)
    {
        EnsureLicense();
        var branding = PrintBrandingProvider.Current;
        var data = m.CustomsReport ?? CustomsReportPrintFactory.FromStatement(m);
        var landscape = data.UseLandscape;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(14);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Arial").FontColor("#212121"));
                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    AppendBrandingHeader(col, branding);

                    col.Item().PaddingTop(6).Background("#15254C").Padding(10).Column(bar =>
                    {
                        bar.Item().AlignCenter().Text(data.Title).Bold().FontSize(13).FontColor(Colors.White);
                        if (!string.IsNullOrWhiteSpace(data.Subtitle))
                            bar.Item().AlignCenter().Text(data.Subtitle).FontSize(9).FontColor(Colors.White);
                    });

                    col.Item().PaddingTop(6).Background("#EEF1F5").Border(0.6f).BorderColor("#90A4AE").Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("رقم المستند: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.DocumentNumber).SemiBold();
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("تاريخ الطباعة: ").SemiBold().FontColor("#546E7A");
                            t.Span(FormatArabicDate(data.PrintedAt)).SemiBold();
                        });
                        row.RelativeItem().Text(t =>
                        {
                            var label = string.IsNullOrWhiteSpace(data.PeriodLabel) ? "الجهة: " : "الفترة: ";
                            var value = string.IsNullOrWhiteSpace(data.PeriodLabel)
                                ? (string.IsNullOrWhiteSpace(data.PartyName) ? "التخليص الكمركي" : data.PartyName)
                                : data.PeriodLabel;
                            t.Span(label).SemiBold().FontColor("#546E7A");
                            t.Span(value).SemiBold();
                        });
                    });

                    var cols = data.Columns.Length == 0 ? new[] { "بيان" } : data.Columns;
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            foreach (var _ in cols)
                                c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            foreach (var title in cols)
                                h.Cell().Element(HeaderCell).AlignCenter().Text(title).FontSize(7.5f).Bold().FontColor(Colors.White);
                        });

                        if (data.Rows.Count == 0)
                        {
                            for (var i = 0; i < cols.Length; i++)
                                table.Cell().Element(BodyCell).AlignCenter().Text(i == 0 ? "لا توجد بيانات" : string.Empty);
                        }
                        else
                        {
                            foreach (var row in data.Rows)
                            {
                                for (var i = 0; i < cols.Length; i++)
                                {
                                    var value = i < row.Length ? FormatCell(row[i]) : string.Empty;
                                    var cell = table.Cell().Element(BodyCell);
                                    if (i is 0 or 1) cell.Text(value);
                                    else cell.AlignCenter().Text(value);
                                }
                            }
                        }
                    });

                    if (data.SummaryLines.Count > 0)
                    {
                        col.Item().PaddingTop(8).Border(1.2f).BorderColor("#F9A825").Background("#FFF8E1").Padding(10).Column(box =>
                        {
                            box.Item().Text("ملخص التقرير:").Bold().FontSize(10);
                            foreach (var line in data.SummaryLines)
                                box.Item().PaddingTop(2).Text(line);
                        });
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void AppendBrandingHeader(ColumnDescriptor col, Core.Models.PrintBrandingSnapshot branding)
    {
        if (branding.ShowHeaderImage && branding.HeaderImageData is { Length: > 0 })
        {
            col.Item().MaxHeight(72).AlignCenter().Image(branding.HeaderImageData).FitWidth();
            return;
        }

        if (!branding.ShowHeaderText || string.IsNullOrWhiteSpace(branding.CompanyName))
            return;

        col.Item().Background("#15254C").Padding(8).Row(row =>
        {
            row.RelativeItem().AlignRight().Column(c =>
            {
                c.Item().Text(branding.CompanyName).FontSize(12).Bold().FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.Details))
                    c.Item().Text(branding.Details).FontSize(7.5f).FontColor("#BBDEFB");
            });
            row.RelativeItem().AlignLeft().Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(branding.PhonePrimary))
                    c.Item().Text(branding.PhonePrimary).FontSize(7.5f).FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.PhoneSecondary))
                    c.Item().Text(branding.PhoneSecondary).FontSize(7.5f).FontColor(Colors.White);
            });
        });
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.Background("#15254C").Border(0.35f).BorderColor(Colors.White).Padding(3);

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.45f).BorderColor("#90A4AE").Padding(3);

    private static string FormatCell(object? value) => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("N2", CultureInfo.InvariantCulture),
        double d => d.ToString("N2", CultureInfo.InvariantCulture),
        float f => f.ToString("N2", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12 ? ArabicMonths[date.Month] : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year} — {date:HH:mm}";
    }
}
