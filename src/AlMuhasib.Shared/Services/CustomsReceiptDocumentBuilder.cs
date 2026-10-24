using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// Portrait Customs customer receipt (وصل قبض مالي) matching the company paper form.
/// Print branding (إعدادات الطابعة) is prepended at the top.
/// </summary>
public static class CustomsReceiptDocumentBuilder
{
    private static readonly SolidColorBrush Navy = Freeze(Color.FromRgb(0x15, 0x25, 0x4C));
    private static readonly SolidColorBrush InfoBarBg = Freeze(Color.FromRgb(0xEE, 0xF1, 0xF5));
    private static readonly SolidColorBrush GridBrush = Freeze(Color.FromRgb(0x90, 0xA4, 0xAE));
    private static readonly SolidColorBrush YellowBox = Freeze(Color.FromRgb(0xFF, 0xF8, 0xE1));
    private static readonly SolidColorBrush GreenBar = Freeze(Color.FromRgb(0x1B, 0x5E, 0x20));
    private static readonly SolidColorBrush PaidGreen = Freeze(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly SolidColorBrush RemainRed = Freeze(Color.FromRgb(0xC6, 0x28, 0x28));
    private static readonly SolidColorBrush NoteBg = Freeze(Color.FromRgb(0xF5, 0xF5, 0xF5));
    private static readonly SolidColorBrush Ink = Freeze(Color.FromRgb(0x21, 0x21, 0x21));
    private static readonly SolidColorBrush Muted = Freeze(Color.FromRgb(0x54, 0x6E, 0x7A));

    private static readonly string[] ArabicMonths =
    [
        "", "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران",
        "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"
    ];

    public static Size ResolvePageSize(VoucherPrintModel m) =>
        InvoicePaperSizes.GetPageSize(InvoicePaperSizes.A4);

    public static FlowDocument Build(VoucherPrintModel m)
    {
        var data = m.CustomsReceipt ?? new CustomsReceiptPrintData
        {
            Title = m.Title,
            ReceiptNumber = m.VoucherNumber,
            ReceiptDate = m.Date,
            CustomerName = m.PartyName ?? string.Empty,
            Amount = m.Amount,
            CurrencyLabel = m.CurrencyLabel,
            PaymentMethodLabel = m.VoucherTypeLabel,
            NoteText = m.Notes ?? string.Empty
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
        doc.Blocks.Add(BuildDetailsBox(data, contentWidth));
        doc.Blocks.Add(BuildAllocationsTable(data, contentWidth));
        doc.Blocks.Add(BuildTotalBar(data, contentWidth));
        if (!string.IsNullOrWhiteSpace(data.NoteText))
            doc.Blocks.Add(BuildNoteBar(data, contentWidth));
        doc.Blocks.Add(BuildSignatures(data, contentWidth));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"معاينة الطباعة — وصل قبض مالي — الصفحة 1 من 1");

        return doc;
    }

    private static Block BuildTitleBar(CustomsReceiptPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.55) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.45) });

        var row = new TableRow { Background = InfoBarBg };
        var title = new Paragraph(new Run(data.Title)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 15
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(10, 12, 10, 12)
        };

        var meta = new Paragraph { Margin = new Thickness(10, 10, 10, 10), TextAlignment = TextAlignment.Left };
        meta.Inlines.Add(new Run("رقم الوصل: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        meta.Inlines.Add(new Run(data.ReceiptNumber) { FontWeight = FontWeights.SemiBold });
        meta.Inlines.Add(new LineBreak());
        meta.Inlines.Add(new Run("التاريخ: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        meta.Inlines.Add(new Run(FormatArabicDate(data.ReceiptDate)) { FontWeight = FontWeights.SemiBold });

        row.Cells.Add(Cell(title));
        row.Cells.Add(Cell(meta));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildDetailsBox(CustomsReceiptPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = InfoBarBg };
        var p = new Paragraph { Margin = new Thickness(14, 12, 14, 12) };

        AddLine(p, "استلمنا من السادة", data.CustomerName);
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run("مبلغ وقدره: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        p.Inlines.Add(new Run(data.AmountInWords)
        {
            FontWeight = FontWeights.Bold,
            Background = YellowBox,
            FontSize = 12
        });
        p.Inlines.Add(new LineBreak());
        AddLine(p, "طريقة الدفع", data.PaymentMethodLabel);
        p.Inlines.Add(new LineBreak());
        AddLine(p, "وذلك عن", data.PurposeText);

        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.8),
            Padding = new Thickness(0)
        });
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildAllocationsTable(CustomsReceiptPrintData data, double contentWidth)
    {
        string[] headers =
        [
            "تفاصيل الحساب",
            "المبلغ الإجمالي للفاتورة",
            "المبلغ المقبوض (الدفعة الحالية)",
            "المبلغ المتبقي بذمة الزبون"
        ];
        double[] weights = [1.6, 1.0, 1.15, 1.15];

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
                FontSize = 10
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4, 8, 4, 8)
            })
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0.4)
            });
        }
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        var body = new TableRowGroup();
        foreach (var r in data.Allocations)
        {
            var row = new TableRow();
            row.Cells.Add(TextCell(r.AccountDetails));
            row.Cells.Add(TextCell(r.InvoiceTotalDisplay, center: true, bold: true));
            row.Cells.Add(TextCell(r.PaidAmountDisplay, center: true, bold: true, foreground: PaidGreen));
            row.Cells.Add(TextCell(r.RemainingAmountDisplay, center: true, bold: true, foreground: RemainRed));
            body.Rows.Add(row);
        }

        table.RowGroups.Add(body);
        return table;
    }

    private static Block BuildTotalBar(CustomsReceiptPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.62) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.38) });
        var row = new TableRow { Background = GreenBar };

        var label = new Paragraph(new Run("إجمالي المبلغ المستلم بموجب هذا الوصل:")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = Brushes.White
        })
        {
            Margin = new Thickness(12, 12, 12, 12)
        };
        var amount = new Paragraph(new Run(data.TotalReceivedDisplay)
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

    private static Block BuildNoteBar(CustomsReceiptPrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 14) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = NoteBg };
        var p = new Paragraph { Margin = new Thickness(12, 10, 12, 10) };
        p.Inlines.Add(new Run("ملاحظة: ") { FontWeight = FontWeights.Bold, Foreground = RemainRed });
        p.Inlines.Add(new Run(data.NoteText) { FontSize = 10.5 });
        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.7)
        });
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildSignatures(CustomsReceiptPrintData data, double contentWidth)
    {
        var branding = PrintBrandingProvider.Current;
        var receiver = string.IsNullOrWhiteSpace(branding.CompanyName)
            ? $"{data.ReceiverSignatureLabel}:"
            : $"{data.ReceiverSignatureLabel} ({branding.CompanyName}):";

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 18, 0, 4) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        var row = new TableRow();
        row.Cells.Add(SignatureCell(receiver));
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

    private static TableCell Cell(Block content) =>
        new(content)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.7),
            Padding = new Thickness(0)
        };

    private static TableCell TextCell(
        string text,
        bool center = false,
        bool bold = false,
        Brush? foreground = null)
    {
        var p = new Paragraph(new Run(text ?? string.Empty)
        {
            FontSize = 10.5,
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
