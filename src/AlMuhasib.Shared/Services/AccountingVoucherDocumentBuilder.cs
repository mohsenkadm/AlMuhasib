using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Utilities;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// معاينة طباعة سند قبض/صرف محاسبي بنفس أسلوب وصل الجمارك.
/// </summary>
public static class AccountingVoucherDocumentBuilder
{
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
        var currency = string.IsNullOrWhiteSpace(m.CurrencyLabel) ? "د.ع" : m.CurrencyLabel.Trim();
        var isUsd = currency.Contains('$')
                    || currency.Contains("دولار", StringComparison.Ordinal);
        var amountDisplay = FormatMoney(m.Amount, currency, isUsd);
        var amountInWords = BuildAmountInWords(m.Amount, amountDisplay, isUsd);
        var title = string.IsNullOrWhiteSpace(m.Title) ? m.VoucherTypeLabel : m.Title;
        if (string.IsNullOrWhiteSpace(title))
            title = "سند";

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

        doc.Blocks.Add(BuildTitleBar(title, m, contentWidth));
        doc.Blocks.Add(BuildDetailsBox(m, amountInWords, currency, isUsd, contentWidth));
        doc.Blocks.Add(BuildTotalBar(amountDisplay, contentWidth));
        if (!string.IsNullOrWhiteSpace(m.Notes))
            doc.Blocks.Add(BuildNoteBar(m.Notes!, contentWidth));
        doc.Blocks.Add(BuildSignatures(contentWidth));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"معاينة الطباعة — {title} — الصفحة 1 من 1");

        return doc;
    }

    private static Block BuildTitleBar(string title, VoucherPrintModel m, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.55) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.45) });

        var row = new TableRow { Background = InfoBarBg };
        var titlePara = new Paragraph(new Run(title)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 15
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(10, 12, 10, 12)
        };

        var meta = new Paragraph { Margin = new Thickness(10, 10, 10, 10), TextAlignment = TextAlignment.Left };
        meta.Inlines.Add(new Run("رقم السند: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        meta.Inlines.Add(new Run(m.VoucherNumber) { FontWeight = FontWeights.SemiBold });
        meta.Inlines.Add(new LineBreak());
        meta.Inlines.Add(new Run("التاريخ: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        meta.Inlines.Add(new Run(FormatArabicDate(m.Date)) { FontWeight = FontWeights.SemiBold });

        row.Cells.Add(Cell(titlePara));
        row.Cells.Add(Cell(meta));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildDetailsBox(
        VoucherPrintModel m,
        string amountInWords,
        string currency,
        bool isUsd,
        double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = InfoBarBg };
        var p = new Paragraph { Margin = new Thickness(14, 12, 14, 12) };

        if (!string.IsNullOrWhiteSpace(m.PartyName))
        {
            var partyLabel = string.IsNullOrWhiteSpace(m.PartyLabel) ? "الطرف" : m.PartyLabel!;
            AddLine(p, partyLabel, m.PartyName);
            p.Inlines.Add(new LineBreak());
        }

        if (!string.IsNullOrWhiteSpace(m.PartyPhone))
        {
            AddLine(p, "الهاتف", m.PartyPhone);
            p.Inlines.Add(new LineBreak());
        }

        if (!string.IsNullOrWhiteSpace(m.VoucherTypeLabel))
        {
            AddLine(p, "نوع السند", m.VoucherTypeLabel);
            p.Inlines.Add(new LineBreak());
        }

        if (!string.IsNullOrWhiteSpace(m.CashBoxName))
        {
            AddLine(p, "القاصة", m.CashBoxName);
            p.Inlines.Add(new LineBreak());
        }

        if (!string.IsNullOrWhiteSpace(m.BankAccountName))
        {
            AddLine(p, "المصرف", m.BankAccountName);
            p.Inlines.Add(new LineBreak());
        }

        p.Inlines.Add(new Run("مبلغ وقدره: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        p.Inlines.Add(new Run(amountInWords)
        {
            FontWeight = FontWeights.Bold,
            Background = YellowBox,
            FontSize = 12
        });

        if (m.BankFees > 0)
        {
            p.Inlines.Add(new LineBreak());
            AddLine(p, "عمولة المصرف", FormatMoney(m.BankFees, currency, isUsd));
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run("صافي المبلغ: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
            p.Inlines.Add(new Run(FormatMoney(m.Amount - m.BankFees, currency, isUsd))
            {
                FontWeight = FontWeights.Bold,
                Foreground = PaidGreen
            });
        }

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

    private static Block BuildTotalBar(string amountDisplay, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.62) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.38) });
        var row = new TableRow { Background = GreenBar };

        var label = new Paragraph(new Run("إجمالي مبلغ السند:")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = Brushes.White
        })
        {
            Margin = new Thickness(12, 12, 12, 12)
        };
        var amount = new Paragraph(new Run(amountDisplay)
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

    private static Block BuildNoteBar(string notes, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 14) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = NoteBg };
        var p = new Paragraph { Margin = new Thickness(12, 10, 12, 10) };
        p.Inlines.Add(new Run("ملاحظات: ") { FontWeight = FontWeights.Bold, Foreground = RemainRed });
        p.Inlines.Add(new Run(notes) { FontSize = 10.5 });
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

    private static Block BuildSignatures(double contentWidth)
    {
        var branding = PrintBrandingProvider.Current;
        var receiver = string.IsNullOrWhiteSpace(branding.CompanyName)
            ? "المستلم:"
            : $"المستلم ({branding.CompanyName}):";

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 18, 0, 4) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        var row = new TableRow();
        row.Cells.Add(SignatureCell(receiver));
        row.Cells.Add(SignatureCell("المحاسب:"));
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

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12
            ? ArabicMonths[date.Month]
            : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year}";
    }

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
