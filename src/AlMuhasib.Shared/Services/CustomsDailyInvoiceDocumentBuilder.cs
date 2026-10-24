using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// Landscape daily Customs invoice/statement form matching the official company layout.
/// Print branding (إعدادات الطابعة) is prepended at the top via <see cref="PrintBrandingFlowDocumentHelper"/>.
/// </summary>
public static class CustomsDailyInvoiceDocumentBuilder
{
    private static readonly SolidColorBrush TableHeaderBg = Freeze(Color.FromRgb(0x15, 0x25, 0x4C));
    private static readonly SolidColorBrush InfoBarBg = Freeze(Color.FromRgb(0xEE, 0xF1, 0xF5));
    private static readonly SolidColorBrush GridBrush = Freeze(Color.FromRgb(0x90, 0xA4, 0xAE));
    private static readonly SolidColorBrush CardBorder = Freeze(Color.FromRgb(0x64, 0xB5, 0xF6));
    private static readonly SolidColorBrush PaidBarBg = Freeze(Color.FromRgb(0xFF, 0xF8, 0xE1));
    private static readonly SolidColorBrush PaidGreen = Freeze(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly SolidColorBrush RemainRed = Freeze(Color.FromRgb(0xC6, 0x28, 0x28));
    private static readonly SolidColorBrush TotalOrange = Freeze(Color.FromRgb(0xEF, 0x6C, 0x00));
    private static readonly SolidColorBrush Ink = Freeze(Color.FromRgb(0x21, 0x21, 0x21));
    private static readonly SolidColorBrush Muted = Freeze(Color.FromRgb(0x54, 0x6E, 0x7A));

    private static readonly string[] ColumnHeaders =
    [
        "ت",
        "اسم السائق",
        "رقم العجلة",
        "تاريخ التفريغ",
        "رقم الإرسالية وعديدها",
        "سيل",
        "إشعاع",
        "رسم",
        "فحص سيطرة نوعية",
        "نقل بري مانيڤست",
        "أتعاب التخليص",
        "المصاريف",
        "ملاحظات"
    ];

    // Relative widths (RTL visual order = array order with FlowDirection RTL)
    private static readonly double[] ColumnWeights =
        [0.35, 1.35, 0.95, 0.95, 1.15, 0.55, 0.55, 0.55, 0.95, 0.95, 0.9, 0.75, 0.85];

    public static Size ResolvePageSize(InvoicePrintModel m) =>
        InvoicePaperSizes.GetPageSize(
            string.IsNullOrWhiteSpace(m.PaperSize) ? InvoicePaperSizes.A4Landscape : m.PaperSize);

    public static FlowDocument Build(InvoicePrintModel m)
    {
        var data = m.CustomsDaily ?? new CustomsDailyInvoicePrintData
        {
            CustomerName = m.PartyName,
            InvoiceNumber = m.InvoiceNumber,
            InvoiceDate = m.Date,
            CurrencyLabel = m.CurrencyLabel,
            InvoiceTotal = m.GrandTotal,
            PaidAmount = m.PaidAmount,
            RemainingAmount = m.RemainingAmount
        };

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

        // هيدر إعدادات الطابعة يبقى في أعلى الكشف
        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc, branding: null, maxHeaderImageHeight: 92);

        doc.Blocks.Add(BuildInfoBar(data, contentWidth));
        doc.Blocks.Add(BuildMainTable(data, contentWidth));
        doc.Blocks.Add(BuildSummaryCards(data, contentWidth));
        doc.Blocks.Add(BuildPaidRemainingBar(data, contentWidth));
        doc.Blocks.Add(BuildSignatures(contentWidth));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"معاينة الطباعة — فاتورة الوجهة — صفحة 1 من 1");

        return doc;
    }

    private static Block BuildInfoBar(CustomsDailyInvoicePrintData data, double contentWidth)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 4, 0, 8)
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.48) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.28) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.24) });

        var row = new TableRow { Background = InfoBarBg };
        row.Cells.Add(InfoCell("العميل", data.CustomerName));
        row.Cells.Add(InfoCell("رقم الفاتورة", data.InvoiceNumber));
        row.Cells.Add(InfoCell("تاريخ الفاتورة", data.InvoiceDate.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)));

        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        return table;
    }

    private static TableCell InfoCell(string label, string value)
    {
        var p = new Paragraph { Margin = new Thickness(10, 8, 10, 8) };
        p.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.Bold, Foreground = Muted, FontSize = 10 });
        p.Inlines.Add(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value)
        {
            FontWeight = FontWeights.SemiBold,
            FontSize = 11
        });
        return new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.6),
            Padding = new Thickness(0)
        };
    }

    private static Block BuildMainTable(CustomsDailyInvoicePrintData data, double contentWidth)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var weightSum = ColumnWeights.Sum();
        foreach (var w in ColumnWeights)
            table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * (w / weightSum)) });

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = TableHeaderBg };
        foreach (var title in ColumnHeaders)
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(title)
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 8.5
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2, 5, 2, 5)
            })
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0.4),
                Padding = new Thickness(1)
            });
        }
        headerGroup.Rows.Add(headerRow);

        // Currency / unit sub-header
        var currency = string.IsNullOrWhiteSpace(data.CurrencyLabel) ? "د.ع" : data.CurrencyLabel;
        var unitRow = new TableRow { Background = Freeze(Color.FromRgb(0xE3, 0xF2, 0xFD)) };
        for (var i = 0; i < ColumnHeaders.Length; i++)
        {
            var text = i switch
            {
                0 or 1 or 2 or 3 or 4 or 12 => i == 4 ? "العملة / الوحدة:" : string.Empty,
                _ => currency
            };
            unitRow.Cells.Add(TextCell(text, bold: true, fontSize: 8, center: true));
        }
        headerGroup.Rows.Add(unitRow);
        table.RowGroups.Add(headerGroup);

        var body = new TableRowGroup();
        var minRows = Math.Max(data.MinRows, data.Rows.Count);
        for (var i = 0; i < minRows; i++)
        {
            var rowData = i < data.Rows.Count ? data.Rows[i] : new CustomsDailyInvoicePrintRow { Number = i + 1 };
            var row = new TableRow();
            if (i % 2 == 1)
                row.Background = Freeze(Color.FromRgb(0xFA, 0xFB, 0xFC));

            row.Cells.Add(TextCell(rowData.Number.ToString(CultureInfo.InvariantCulture), center: true, fontSize: 9));
            row.Cells.Add(TextCell(rowData.DriverName, fontSize: 9));
            row.Cells.Add(TextCell(rowData.WheelNumber, center: true, fontSize: 9));
            row.Cells.Add(TextCell(rowData.UnloadingDate, center: true, fontSize: 9));
            row.Cells.Add(TextCell(rowData.Consignment, center: true, fontSize: 9));
            row.Cells.Add(TextCell(FormatAmount(rowData.Seal, currency), center: true, fontSize: 9));
            row.Cells.Add(TextCell(FormatAmount(rowData.Radiation, currency), center: true, fontSize: 9));
            row.Cells.Add(TextCell(FormatAmount(rowData.Duty, currency), center: true, fontSize: 9));
            row.Cells.Add(TextCell(FormatAmount(rowData.QualityControl, currency), center: true, fontSize: 9));
            row.Cells.Add(TextCell(FormatAmount(rowData.LandTransport, currency), center: true, fontSize: 9));
            row.Cells.Add(TextCell(FormatAmount(rowData.ClearanceFees, currency), center: true, fontSize: 9));
            row.Cells.Add(TextCell(FormatAmount(rowData.Expenses, currency), center: true, fontSize: 9));
            row.Cells.Add(TextCell(rowData.Notes, fontSize: 8.5));
            body.Rows.Add(row);
        }

        table.RowGroups.Add(body);
        return table;
    }

    private static Block BuildSummaryCards(CustomsDailyInvoicePrintData data, double contentWidth)
    {
        var currency = string.IsNullOrWhiteSpace(data.CurrencyLabel) ? "د.ع" : data.CurrencyLabel;
        var cards = new (string Title, decimal Amount, bool Highlight)[]
        {
            ("إجمالي الرسوم", data.TotalDuties, false),
            ("إجمالي الفحوصات", data.TotalInspections, false),
            ("إجمالي النقل البري", data.TotalLandTransport, false),
            ("أتعاب التخليص", data.TotalClearanceFees, false),
            ("إجمالي المصاريف", data.TotalExpenses, false),
            ("إجمالي الفاتورة", data.InvoiceTotal, true)
        };

        var table = new Table
        {
            CellSpacing = 6,
            Margin = new Thickness(0, 2, 0, 8)
        };
        var colW = contentWidth / cards.Length;
        foreach (var _ in cards)
            table.Columns.Add(new TableColumn { Width = new GridLength(colW) });

        var row = new TableRow();
        foreach (var card in cards)
        {
            var stack = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(4, 6, 4, 6) };
            stack.Inlines.Add(new Run(card.Title)
            {
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = Muted
            });
            stack.Inlines.Add(new LineBreak());
            stack.Inlines.Add(new Run($"{currency} {card.Amount:N0}")
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = card.Highlight ? TotalOrange : Ink
            });

            row.Cells.Add(new TableCell(stack)
            {
                BorderBrush = CardBorder,
                BorderThickness = new Thickness(1.2),
                Padding = new Thickness(2),
                Background = Brushes.White
            });
        }

        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        return table;
    }

    private static Block BuildPaidRemainingBar(CustomsDailyInvoicePrintData data, double contentWidth)
    {
        var currency = string.IsNullOrWhiteSpace(data.CurrencyLabel) ? "د.ع" : data.CurrencyLabel;
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 0, 0, 10)
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });

        var paid = new Paragraph { Margin = new Thickness(12, 8, 12, 8) };
        paid.Inlines.Add(new Run("المسدد (المدفوع): ") { FontWeight = FontWeights.SemiBold, Foreground = Muted });
        paid.Inlines.Add(new Run($"{currency} {data.PaidAmount:N0}")
        {
            FontWeight = FontWeights.Bold,
            Foreground = PaidGreen,
            FontSize = 12
        });

        var remain = new Paragraph { Margin = new Thickness(12, 8, 12, 8), TextAlignment = TextAlignment.Left };
        remain.Inlines.Add(new Run("المتبقي بذمة العميل: ") { FontWeight = FontWeights.SemiBold, Foreground = Muted });
        remain.Inlines.Add(new Run($"{currency} {data.RemainingAmount:N0}")
        {
            FontWeight = FontWeights.Bold,
            Foreground = RemainRed,
            FontSize = 12
        });

        var row = new TableRow { Background = PaidBarBg };
        row.Cells.Add(new TableCell(paid) { BorderBrush = Freeze(Color.FromRgb(0xFF, 0xE0, 0x82)), BorderThickness = new Thickness(1) });
        row.Cells.Add(new TableCell(remain) { BorderBrush = Freeze(Color.FromRgb(0xFF, 0xE0, 0x82)), BorderThickness = new Thickness(1) });

        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        return table;
    }

    private static Block BuildSignatures(double contentWidth)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 6, 0, 0)
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth / 3) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth / 3) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth / 3) });

        var row = new TableRow();
        row.Cells.Add(SignatureCell("منظم الفاتورة / المحاسب"));
        row.Cells.Add(StampCell());
        row.Cells.Add(SignatureCell("توقيع واستلام العميل"));

        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        return table;
    }

    private static TableCell SignatureCell(string title)
    {
        var p = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 18, 8, 8)
        };
        p.Inlines.Add(new Run(title) { FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Muted });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run("____________________") { FontSize = 11, Foreground = GridBrush });
        return new TableCell(p);
    }

    private static TableCell StampCell()
    {
        var p = new Paragraph
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 8, 8, 8)
        };
        p.Inlines.Add(new Run("ختم الشركة") { FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = CardBorder });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run("◯") { FontSize = 28, Foreground = CardBorder });
        return new TableCell(p);
    }

    private static TableCell TextCell(string text, bool bold = false, double fontSize = 10, bool center = false)
    {
        var display = string.IsNullOrWhiteSpace(text) ? "—" : text;
        return new TableCell(new Paragraph(new Run(display)
        {
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
        })
        {
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Right,
            Margin = new Thickness(3, 4, 3, 4)
        })
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(1)
        };
    }

    private static string FormatAmount(decimal? amount, string currency)
    {
        if (amount is null || amount == 0)
            return "—";
        return $"{currency} {amount.Value:N0}";
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
