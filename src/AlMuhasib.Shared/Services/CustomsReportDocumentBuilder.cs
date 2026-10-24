using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// Shared Customs staff/list report form matching company print language.
/// Branding header from print settings is prepended at the top.
/// </summary>
public static class CustomsReportDocumentBuilder
{
    private static readonly SolidColorBrush Navy = Freeze(Color.FromRgb(0x15, 0x25, 0x4C));
    private static readonly SolidColorBrush InfoBarBg = Freeze(Color.FromRgb(0xEE, 0xF1, 0xF5));
    private static readonly SolidColorBrush GridBrush = Freeze(Color.FromRgb(0x90, 0xA4, 0xAE));
    private static readonly SolidColorBrush SummaryBg = Freeze(Color.FromRgb(0xFF, 0xF8, 0xE1));
    private static readonly SolidColorBrush SummaryBorder = Freeze(Color.FromRgb(0xF9, 0xA8, 0x25));
    private static readonly SolidColorBrush AltRow = Freeze(Color.FromRgb(0xFA, 0xFB, 0xFC));
    private static readonly SolidColorBrush Ink = Freeze(Color.FromRgb(0x21, 0x21, 0x21));
    private static readonly SolidColorBrush Muted = Freeze(Color.FromRgb(0x54, 0x6E, 0x7A));

    private static readonly string[] ArabicMonths =
    [
        "", "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران",
        "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"
    ];

    public static Size ResolvePageSize(StatementPrintModel m)
    {
        var landscape = m.CustomsReport?.UseLandscape != false;
        return InvoicePaperSizes.GetPageSize(landscape ? InvoicePaperSizes.A4Landscape : InvoicePaperSizes.A4);
    }

    public static FlowDocument Build(StatementPrintModel m)
    {
        var data = m.CustomsReport ?? CustomsReportPrintFactory.FromStatement(m);
        var pageSize = ResolvePageSize(m);
        var pagePadding = new Thickness(18, 12, 18, 12);
        var contentWidth = pageSize.Width - pagePadding.Left - pagePadding.Right;

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 10,
            Foreground = Ink,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = pageSize.Width,
            PageHeight = pageSize.Height,
            PagePadding = pagePadding,
            ColumnWidth = contentWidth
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc, branding: null, maxHeaderImageHeight: 80);

        doc.Blocks.Add(BuildTitleBar(data, contentWidth));
        doc.Blocks.Add(BuildInfoBar(data, contentWidth));
        doc.Blocks.Add(BuildTable(data, contentWidth));
        if (data.SummaryLines.Count > 0)
            doc.Blocks.Add(BuildSummary(data, contentWidth));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"معاينة الطباعة — {data.Title} — الصفحة 1 من 1");

        return doc;
    }

    private static Block BuildTitleBar(CustomsReportPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 2, 0, 8) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = Navy };
        var p = new Paragraph { Margin = new Thickness(12, 10, 12, 10), TextAlignment = TextAlignment.Center };
        p.Inlines.Add(new Run(data.Title) { Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14 });
        if (!string.IsNullOrWhiteSpace(data.Subtitle))
        {
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run(data.Subtitle) { Foreground = Brushes.White, FontSize = 10 });
        }
        row.Cells.Add(new TableCell(p));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildInfoBar(CustomsReportPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 8) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.34) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.33) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.33) });
        var row = new TableRow { Background = InfoBarBg };
        row.Cells.Add(InfoCell("رقم المستند", data.DocumentNumber));
        row.Cells.Add(InfoCell("تاريخ الطباعة", FormatArabicDate(data.PrintedAt)));
        row.Cells.Add(InfoCell(
            string.IsNullOrWhiteSpace(data.PeriodLabel) ? "الجهة" : "الفترة",
            string.IsNullOrWhiteSpace(data.PeriodLabel)
                ? (string.IsNullOrWhiteSpace(data.PartyName) ? "التخليص الكمركي" : data.PartyName)
                : data.PeriodLabel));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static TableCell InfoCell(string label, string value)
    {
        var p = new Paragraph { Margin = new Thickness(8, 7, 8, 7) };
        p.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.Bold, Foreground = Muted, FontSize = 9 });
        p.Inlines.Add(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value) { FontWeight = FontWeights.SemiBold, FontSize = 10 });
        return new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.6)
        };
    }

    private static Block BuildTable(CustomsReportPrintData data, double contentWidth)
    {
        var cols = data.Columns.Length == 0 ? ["بيان"] : data.Columns;
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 8) };
        var colW = contentWidth / cols.Length;
        foreach (var _ in cols)
            table.Columns.Add(new TableColumn { Width = new GridLength(colW) });

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = Navy };
        foreach (var h in cols)
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h)
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 8.5
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2, 6, 2, 6)
            })
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0.35)
            });
        }
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        var body = new TableRowGroup();
        if (data.Rows.Count == 0)
        {
            var empty = new TableRow();
            for (var i = 0; i < cols.Length; i++)
                empty.Cells.Add(TextCell(i == 0 ? "لا توجد بيانات" : string.Empty, center: true));
            body.Rows.Add(empty);
        }
        else
        {
            for (var r = 0; r < data.Rows.Count; r++)
            {
                var rowData = data.Rows[r];
                var row = new TableRow();
                if (r % 2 == 1) row.Background = AltRow;
                for (var c = 0; c < cols.Length; c++)
                {
                    var value = c < rowData.Length ? FormatCell(rowData[c]) : string.Empty;
                    row.Cells.Add(TextCell(value, center: c != 0 && c != 1));
                }
                body.Rows.Add(row);
            }
        }

        table.RowGroups.Add(body);
        return table;
    }

    private static Block BuildSummary(CustomsReportPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 4) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = SummaryBg };
        var p = new Paragraph { Margin = new Thickness(12, 10, 12, 10) };
        p.Inlines.Add(new Run("ملخص التقرير:") { FontWeight = FontWeights.Bold, FontSize = 11 });
        foreach (var line in data.SummaryLines)
        {
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run(line) { FontSize = 10 });
        }
        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = SummaryBorder,
            BorderThickness = new Thickness(1.2)
        });
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static TableCell TextCell(string text, bool center = false)
    {
        var p = new Paragraph(new Run(text ?? string.Empty) { FontSize = 8.5 })
        {
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Right,
            Margin = new Thickness(3, 4, 3, 4)
        };
        return new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.45)
        };
    }

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

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}

/// <summary>Builds <see cref="CustomsReportPrintData"/> from generic statement fields.</summary>
public static class CustomsReportPrintFactory
{
    public static CustomsReportPrintData FromStatement(StatementPrintModel m, AlMuhasib.Core.Enums.Customs.CustomsPrintDocumentKind? kind = null)
    {
        var period = BuildPeriod(m.FromDate, m.ToDate);
        var useLandscape = (m.Columns?.Length ?? 0) >= 5;
        return new CustomsReportPrintData
        {
            Title = string.IsNullOrWhiteSpace(m.Title) ? "تقرير التخليص الكمركي" : m.Title,
            Subtitle = string.IsNullOrWhiteSpace(m.PartyName) ? "نظام التخليص الكمركي" : m.PartyName,
            DocumentNumber = $"CR-{DateTime.Now:yyyyMMdd-HHmm}",
            PrintedAt = DateTime.Now,
            PartyName = m.PartyName ?? string.Empty,
            PeriodLabel = period,
            Columns = m.Columns ?? [],
            Rows = m.Rows ?? [],
            SummaryLines = m.SummaryLines?.ToList() ?? [],
            Kind = kind ?? m.CustomsReport?.Kind,
            UseLandscape = useLandscape
        };
    }

    public static CustomsReportPrintData FromTable(
        string title,
        string[] columns,
        IList<object[]> rows,
        IList<string>? summaryLines = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? partyName = null,
        AlMuhasib.Core.Enums.Customs.CustomsPrintDocumentKind? kind = null)
    {
        return FromStatement(new StatementPrintModel
        {
            Title = title,
            PartyName = partyName ?? "نظام التخليص الكمركي",
            FromDate = fromDate,
            ToDate = toDate,
            Columns = columns,
            Rows = rows,
            SummaryLines = summaryLines
        }, kind);
    }

    private static string BuildPeriod(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return "جميع الفترات";
        if (from is not null && to is not null)
            return $"{from:yyyy/MM/dd} — {to:yyyy/MM/dd}";
        if (from is not null) return $"من {from:yyyy/MM/dd}";
        return $"حتى {to:yyyy/MM/dd}";
    }
}
