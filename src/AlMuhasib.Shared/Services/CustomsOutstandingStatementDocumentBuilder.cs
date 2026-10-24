using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// Portrait Customs outstanding-balance claim (مطالبة بدين متبقي).
/// Print branding (إعدادات الطابعة) is prepended at the top.
/// </summary>
public static class CustomsOutstandingStatementDocumentBuilder
{
    private static readonly SolidColorBrush Navy = Freeze(Color.FromRgb(0x15, 0x25, 0x4C));
    private static readonly SolidColorBrush InfoBarBg = Freeze(Color.FromRgb(0xEE, 0xF1, 0xF5));
    private static readonly SolidColorBrush GridBrush = Freeze(Color.FromRgb(0x90, 0xA4, 0xAE));
    private static readonly SolidColorBrush SettlementBg = Freeze(Color.FromRgb(0xFF, 0xEB, 0xEE));
    private static readonly SolidColorBrush SettlementBorder = Freeze(Color.FromRgb(0xE5, 0x73, 0x73));
    private static readonly SolidColorBrush RedBar = Freeze(Color.FromRgb(0xB7, 0x1C, 0x1C));
    private static readonly SolidColorBrush TitleRed = Freeze(Color.FromRgb(0xC6, 0x28, 0x28));
    private static readonly SolidColorBrush PaidGreen = Freeze(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly SolidColorBrush RemainRed = Freeze(Color.FromRgb(0xC6, 0x28, 0x28));
    private static readonly SolidColorBrush Ink = Freeze(Color.FromRgb(0x21, 0x21, 0x21));
    private static readonly SolidColorBrush Muted = Freeze(Color.FromRgb(0x54, 0x6E, 0x7A));

    private static readonly string[] ArabicMonths =
    [
        "", "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران",
        "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"
    ];

    public static Size ResolvePageSize(StatementPrintModel m) =>
        InvoicePaperSizes.GetPageSize(InvoicePaperSizes.A4);

    public static FlowDocument Build(StatementPrintModel m)
    {
        var data = m.CustomsOutstandingStatement ?? new CustomsOutstandingStatementPrintData
        {
            Title = m.Title,
            CustomerName = m.PartyName,
            DocumentDate = DateTime.Today
        };

        var pageSize = ResolvePageSize(m);
        var pagePadding = new Thickness(28, 16, 28, 16);
        var contentWidth = pageSize.Width - pagePadding.Left - pagePadding.Right;

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 11,
            Foreground = Ink,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = pageSize.Width,
            PageHeight = pageSize.Height,
            PagePadding = pagePadding,
            ColumnWidth = contentWidth
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc, branding: null, maxHeaderImageHeight: 92);

        doc.Blocks.Add(BuildTitleBar(data, contentWidth));
        doc.Blocks.Add(BuildInfoBox(data, contentWidth));
        doc.Blocks.Add(BuildTable(data, contentWidth));
        doc.Blocks.Add(BuildSettlementBox(data, contentWidth));
        doc.Blocks.Add(BuildDueBar(data, contentWidth));
        doc.Blocks.Add(BuildSignatures(data, contentWidth));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: "معاينة الطباعة — مطالبة دين متبقي — الصفحة 1 من 1");

        return doc;
    }

    private static Block BuildTitleBar(CustomsOutstandingStatementPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.58) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.42) });

        var row = new TableRow { Background = InfoBarBg };
        var title = new Paragraph(new Run(data.Title)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = TitleRed
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 12, 8, 12)
        };

        var meta = new Paragraph { Margin = new Thickness(10, 10, 10, 10), TextAlignment = TextAlignment.Left };
        meta.Inlines.Add(new Run("رقم المستند: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        meta.Inlines.Add(new Run(data.DocumentNumber) { FontWeight = FontWeights.SemiBold });
        meta.Inlines.Add(new LineBreak());
        meta.Inlines.Add(new Run("التاريخ: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        meta.Inlines.Add(new Run(FormatArabicDate(data.DocumentDate)) { FontWeight = FontWeights.SemiBold });

        row.Cells.Add(Bordered(title));
        row.Cells.Add(Bordered(meta));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildInfoBox(CustomsOutstandingStatementPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = InfoBarBg };
        var p = new Paragraph { Margin = new Thickness(14, 12, 14, 12) };
        AddLine(p, "المستدين / المطلوب", data.CustomerName);
        p.Inlines.Add(new LineBreak());
        AddLine(p, "مرجع الفاتورة الأصلية", data.InvoiceReferenceText);
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run("حالة الحساب: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        p.Inlines.Add(new Run(data.AccountStatusText) { FontWeight = FontWeights.Bold, Foreground = RemainRed });
        row.Cells.Add(Bordered(p));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildTable(CustomsOutstandingStatementPrintData data, double contentWidth)
    {
        string[] headers =
        [
            "ت",
            "البيان / مرجع الحساب",
            "المبلغ الكلي للفاتورة",
            data.PaidColumnHeader,
            "المبلغ المتبقي (الدين)"
        ];
        double[] weights = [0.35, 1.7, 1.0, 1.15, 1.05];

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };
        var sum = weights.Sum();
        foreach (var w in weights)
            table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * (w / sum)) });

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = Navy };
        foreach (var h in headers)
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h)
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 9
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(3, 8, 3, 8)
            })
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0.4)
            });
        }
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        var body = new TableRowGroup();
        if (data.Rows.Count == 0)
        {
            var empty = new TableRow();
            empty.Cells.Add(TextCell("—", center: true));
            empty.Cells.Add(TextCell("لا توجد ديون متبقية"));
            empty.Cells.Add(TextCell("—", center: true));
            empty.Cells.Add(TextCell("—", center: true));
            empty.Cells.Add(TextCell("—", center: true));
            body.Rows.Add(empty);
        }
        else
        {
            foreach (var r in data.Rows)
            {
                var row = new TableRow();
                row.Cells.Add(TextCell(r.Number.ToString(CultureInfo.InvariantCulture), center: true));
                row.Cells.Add(TextCell(r.Description));
                row.Cells.Add(TextCell(r.InvoiceTotalDisplay, center: true, bold: true));
                row.Cells.Add(TextCell(r.PaidAmountDisplay, center: true, bold: true, foreground: PaidGreen));
                row.Cells.Add(TextCell(r.RemainingAmountDisplay, center: true, bold: true, foreground: RemainRed));
                body.Rows.Add(row);
            }
        }

        table.RowGroups.Add(body);
        return table;
    }

    private static Block BuildSettlementBox(CustomsOutstandingStatementPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = SettlementBg };
        var p = new Paragraph { Margin = new Thickness(14, 12, 14, 12) };
        p.Inlines.Add(new Run("تسوية وتفصيل الدين المتبقي")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = RemainRed
        });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run($"{data.BaseInvoiceTotalLabel}: {data.BaseInvoiceTotalDisplay}") { FontSize = 11 });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run($"{data.ReceiptsTotalLabel}: - {data.ReceiptsTotalDisplay}")
        {
            FontSize = 11,
            Foreground = PaidGreen,
            FontWeight = FontWeights.SemiBold
        });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run($"{data.RemainingLabel}: {data.RemainingDisplay}")
        {
            FontSize = 11,
            Foreground = RemainRed,
            FontWeight = FontWeights.Bold
        });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run(data.AmountInWords) { FontWeight = FontWeights.SemiBold, FontSize = 11 });

        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = SettlementBorder,
            BorderThickness = new Thickness(1.4)
        });
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildDueBar(CustomsOutstandingStatementPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 2, 0, 14) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.62) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.38) });
        var row = new TableRow { Background = RedBar };
        var label = new Paragraph(new Run("إجمالي المبلغ المستحق للدفع (Balance Due):")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = Brushes.White
        })
        {
            Margin = new Thickness(12, 12, 12, 12)
        };
        var amount = new Paragraph(new Run(data.BalanceDueDisplay)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            Foreground = Brushes.White
        })
        {
            Margin = new Thickness(12, 10, 12, 10),
            TextAlignment = TextAlignment.Left
        };
        row.Cells.Add(new TableCell(label));
        row.Cells.Add(new TableCell(amount));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildSignatures(CustomsOutstandingStatementPrintData data, double contentWidth)
    {
        var branding = PrintBrandingProvider.Current;
        var organizer = string.IsNullOrWhiteSpace(branding.CompanyName)
            ? $"{data.OrganizerSignatureLabel}:"
            : $"{data.OrganizerSignatureLabel} ({branding.CompanyName}):";

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 16, 0, 4) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        var row = new TableRow();
        row.Cells.Add(SignatureCell(organizer));
        row.Cells.Add(SignatureCell(data.PayerSignatureLabel + ":"));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static TableCell SignatureCell(string title)
    {
        var p = new Paragraph { Margin = new Thickness(8, 8, 8, 28), TextAlignment = TextAlignment.Center };
        p.Inlines.Add(new Run(title) { FontWeight = FontWeights.SemiBold, FontSize = 11 });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run("________________") { Foreground = Muted, FontSize = 12 });
        return new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.7),
            Padding = new Thickness(4)
        };
    }

    private static void AddLine(Paragraph p, string label, string value)
    {
        p.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        p.Inlines.Add(new Run(value) { FontWeight = FontWeights.SemiBold });
    }

    private static TableCell Bordered(Block content) =>
        new(content)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.7)
        };

    private static TableCell TextCell(
        string text,
        bool center = false,
        bool bold = false,
        Brush? foreground = null)
    {
        var p = new Paragraph(new Run(text ?? string.Empty)
        {
            FontSize = 10,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = foreground ?? Ink
        })
        {
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Right,
            Margin = new Thickness(4, 8, 4, 8)
        };
        return new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.5)
        };
    }

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12 ? ArabicMonths[date.Month] : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year}";
    }

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
