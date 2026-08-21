using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// قالب A4 لفواتير البيع/الشراء/المرتجع: عنوان، تاريخ الفاتورة، بيانات الجهة ورقم الفاتورة/المندوب،
/// ثم جدول البنود مع صفوف المجاميع وطريقة الدفع أسفل الجدول.
/// العملة تُكتب في رأس الأعمدة وليس بجانب كل رقم لتفادي تشابك الأرقام مع النص العربي.
/// </summary>
public static class ModernInvoiceDocumentBuilder
{
    public const double PageWidth = 793.7;   // A4 @ 96 DPI
    public const double PageHeight = 1122.5;

    private const double LineWidth = 1.0;

    private static readonly SolidColorBrush Ink = Freeze(Color.FromRgb(0x1F, 0x24, 0x28));
    private static readonly SolidColorBrush Muted = Freeze(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly SolidColorBrush HeaderInk = Freeze(Color.FromRgb(0x4B, 0x55, 0x63));
    private static readonly SolidColorBrush Grid = Freeze(Color.FromRgb(0xB0, 0xB7, 0xBE));
    private static readonly SolidColorBrush HeadBg = Freeze(Color.FromRgb(0xF2, 0xF4, 0xF6));
    private static readonly SolidColorBrush TotalBg = Freeze(Color.FromRgb(0xFA, 0xFA, 0xFA));

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static FlowDocument Build(InvoicePrintModel m)
    {
        var hideAmounts = m.HideAmounts;
        // قالب "Compact" في الإعدادات — أو عدد بنود كبير — يضغط الخطوط والحشوات بنفس التصميم.
        var compact = InvoiceA4TemplateTheme.Resolve(m.A4TemplateId).ForceCompactMetrics || m.Items.Count > 18;
        // حجم الخط الأساسي 14 لسهولة القراءة؛ القالب المضغوط يقلّل قليلاً فقط.
        var baseFont = compact ? 13.0 : 14.0;
        var cellPadding = compact ? new Thickness(6, 4, 6, 4) : new Thickness(8, 5, 8, 5);
        var currency = string.IsNullOrWhiteSpace(m.CurrencyLabel) ? "د.ع" : m.CurrencyLabel;
        var branding = PrintBrandingProvider.Current;
        var pagePadding = compact ? new Thickness(34, 20, 34, 20) : new Thickness(44, 24, 44, 24);
        // أعمدة الجداول تُحسب على عرض المحتوى الفعلي: الأعمدة المرنة (Star) تخرج عن الصفحة
        // في FlowDocument فتختفي القيم وتتضخم الصفوف.
        var contentWidth = PageWidth - pagePadding.Left - pagePadding.Right;

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = baseFont,
            Foreground = Ink,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = PageWidth,
            PageHeight = PageHeight,
            PagePadding = pagePadding,
            ColumnWidth = contentWidth
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        // ── العنوان ──
        doc.Blocks.Add(new Paragraph(new Run(m.Title))
        {
            FontSize = compact ? 20 : 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, compact ? 4 : 8, 0, compact ? 6 : 10)
        });

        // سطر بيانات الشركة يظهر فقط عندما لا يوجد ترويسة مطبوعة، لتفادي تكرار نفس البيانات.
        var companyParts = branding.HasHeaderContent
            ? []
            : new[] { branding.CompanyName, branding.Address, branding.PhonePrimary }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();
        if (companyParts.Length > 0)
        {
            doc.Blocks.Add(new Paragraph(new Run(string.Join("   |   ", companyParts)))
            {
                FontSize = baseFont,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, compact ? 8 : 12)
            });
        }

        var dateLine = new Paragraph
        {
            FontSize = baseFont + 0.5,
            Margin = new Thickness(0, compact ? 2 : 4, 0, compact ? 8 : 12)
        };
        dateLine.Inlines.Add(new Run("تاريخ الفاتورة  ") { FontWeight = FontWeights.Bold, Foreground = HeaderInk });
        dateLine.Inlines.Add(new Run(m.Date.ToString("yyyy/MM/dd")) { FontWeight = FontWeights.SemiBold });
        if (m.CreditDueDate.HasValue)
        {
            dateLine.Inlines.Add(new Run("    تاريخ الاستحقاق  ") { FontWeight = FontWeights.Bold, Foreground = HeaderInk });
            dateLine.Inlines.Add(new Run(m.CreditDueDate.Value.ToString("yyyy/MM/dd")) { FontWeight = FontWeights.SemiBold });
        }
        doc.Blocks.Add(dateLine);

        // ── بطاقة العميل يميناً، ورقم الفاتورة مع المندوب يساراً ──
        var detailsGap = compact ? 8.0 : 12.0;
        var detailsWidth = (contentWidth - detailsGap) / 2;
        var detailsLayout = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0)
        };
        detailsLayout.Columns.Add(new TableColumn { Width = new GridLength(detailsWidth) });
        detailsLayout.Columns.Add(new TableColumn { Width = new GridLength(detailsWidth) });

        var customerRows = new List<(string Label, string Value)>();
        AddInfoRow(customerRows, "الاسم", m.PartyName);
        AddInfoRow(customerRows, "الهاتف", m.PartyPhone);
        AddInfoRow(customerRows, "العنوان", m.PartyAddress);
        AddInfoRow(customerRows, "رقم الملف", m.FileNumber);
        if (customerRows.Count == 0)
            customerRows.Add(("الاسم", "—"));

        var invoiceRows = new List<(string Label, string Value)>();
        AddInfoRow(invoiceRows, "رقم الفاتورة", m.InvoiceNumber);
        AddInfoRow(invoiceRows, "الاسم", m.SalesRepresentativeName);
        AddInfoRow(invoiceRows, "الهاتف", m.SalesRepresentativePhone);
        AddInfoRow(invoiceRows, "السائق", m.DriverName);
        if (invoiceRows.Count == 0)
            invoiceRows.Add(("رقم الفاتورة", string.IsNullOrWhiteSpace(m.InvoiceNumber) ? "—" : m.InvoiceNumber));

        var detailsGroup = new TableRowGroup();
        var detailsRow = new TableRow();
        detailsRow.Cells.Add(DetailsCard(
            customerRows,
            cellPadding,
            baseFont,
            detailsWidth - (detailsGap / 2),
            new Thickness(detailsGap / 2, 0, 0, 0)));
        detailsRow.Cells.Add(DetailsCard(
            invoiceRows,
            cellPadding,
            baseFont,
            detailsWidth - (detailsGap / 2),
            new Thickness(0, 0, detailsGap / 2, 0)));

        detailsGroup.Rows.Add(detailsRow);
        detailsLayout.RowGroups.Add(detailsGroup);
        doc.Blocks.Add(detailsLayout);

        // ── جدول البنود والمجاميع ──
        AddHeading(doc, hideAmounts ? "تفاصيل المواد" : "المبالغ الإجمالية", baseFont, compact);

        var itemsTable = NewGridTable(new Thickness(0, compact ? 2 : 4, 0, 0));
        var columnTitles = hideAmounts
            ? new[] { "الوصف", "الكمية" }
            : new[] { "الوصف", "الكمية", $"سعر الوحدة ({currency})", $"الإجمالي ({currency})" };

        var numericWidths = hideAmounts
            ? new[] { 110.0 }
            : new[] { compact ? 58.0 : 66.0, compact ? 105.0 : 118.0, compact ? 115.0 : 130.0 };
        itemsTable.Columns.Add(new TableColumn { Width = new GridLength(contentWidth - numericWidths.Sum()) });
        foreach (var width in numericWidths)
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(width) });

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow();
        for (var i = 0; i < columnTitles.Length; i++)
        {
            headerRow.Cells.Add(GridCell(
                columnTitles[i],
                cellPadding,
                baseFont,
                bold: true,
                align: TextAlignment.Center,
                background: HeadBg,
                foreground: HeaderInk,
                isLastColumn: i == columnTitles.Length - 1));
        }
        headerGroup.Rows.Add(headerRow);
        itemsTable.RowGroups.Add(headerGroup);

        var bodyGroup = new TableRowGroup();
        foreach (var item in m.Items)
        {
            var row = new TableRow();

            var nameCell = new TableCell
            {
                Padding = cellPadding,
                BorderBrush = Grid,
                BorderThickness = InnerBorder(isLastColumn: false)
            };
            nameCell.Blocks.Add(new Paragraph(new Run($"{item.Number}. {item.ItemName}"))
            {
                Margin = new Thickness(0),
                FontSize = baseFont
            });
            if (m.PharmacyUsageReceipt && !string.IsNullOrWhiteSpace(item.UsageInstructions))
            {
                nameCell.Blocks.Add(new Paragraph(new Run($"طريقة الاستخدام: {item.UsageInstructions}"))
                {
                    Margin = new Thickness(0, 1, 0, 0),
                    FontSize = baseFont - 1.5,
                    Foreground = Muted
                });
            }
            row.Cells.Add(nameCell);

            row.Cells.Add(GridCell(
                FormatNumber(item.Quantity),
                cellPadding,
                baseFont,
                align: TextAlignment.Center,
                isLastColumn: hideAmounts));

            if (!hideAmounts)
            {
                row.Cells.Add(GridCell(FormatNumber(item.UnitPrice), cellPadding, baseFont, align: TextAlignment.Center));
                row.Cells.Add(GridCell(FormatNumber(item.TotalPrice), cellPadding, baseFont, align: TextAlignment.Center, isLastColumn: true));
            }

            bodyGroup.Rows.Add(row);
        }
        itemsTable.RowGroups.Add(bodyGroup);

        doc.Blocks.Add(itemsTable);

        if (!hideAmounts)
        {
            var amountEntries = new List<(string Label, string Value, bool Emphasize)>();
            if (!string.IsNullOrWhiteSpace(m.PaymentMethod))
                amountEntries.Add(("طريقة الدفع", m.PaymentMethod, false));
            amountEntries.Add(("المجموع الفرعي", FormatNumber(m.Subtotal), false));
            if (m.DiscountAmount != 0)
            {
                amountEntries.Add(("الخصم", FormatNumber(m.DiscountAmount), false));
                amountEntries.Add(("المبلغ بعد الخصم", FormatNumber(m.Subtotal - m.DiscountAmount), false));
            }
            if (m.TransportFeeAmount != 0)
                amountEntries.Add(("أجور النقل", FormatNumber(m.TransportFeeAmount), false));
            if (m.TaxRate != 0 || m.TaxAmount != 0)
                amountEntries.Add((m.TaxRate != 0 ? $"الضريبة {m.TaxRate:0.##}%" : "الضريبة", FormatNumber(m.TaxAmount), false));
            if (m.CompanyFeeAmount is { } fee && fee != 0)
                amountEntries.Add(("نسبة الشركة", FormatNumber(fee), false));
            if (m.RoundingAmount != 0)
                amountEntries.Add(("التقريب", FormatNumber(m.RoundingAmount), false));
            amountEntries.Add(("الإجمالي المستحق", FormatNumber(m.GrandTotal), true));
            if (m.PaidAmount != 0 || m.RemainingAmount != 0)
            {
                amountEntries.Add(("المدفوع", FormatNumber(m.PaidAmount), false));
                amountEntries.Add(("المتبقي", FormatNumber(m.RemainingAmount), false));
            }

            AddCompactTotals(doc, amountEntries, contentWidth, compact, baseFont);
        }

        if (!string.IsNullOrWhiteSpace(m.Notes))
        {
            var notes = new Paragraph { Margin = new Thickness(0, compact ? 6 : 10, 0, 0), FontSize = baseFont };
            notes.Inlines.Add(new Run("ملاحظات: ") { FontWeight = FontWeights.Bold });
            notes.Inlines.Add(new Run(m.Notes));
            doc.Blocks.Add(notes);
        }

        // ── التواقيع ──
        var signatureTable = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, compact ? 12 : 28, 0, 0)
        };
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(contentWidth / 2) });
        signatureTable.Columns.Add(new TableColumn { Width = new GridLength(contentWidth / 2) });
        var signatureGroup = new TableRowGroup();
        var signatureRow = new TableRow();
        foreach (var label in new[] { "توقيع المستلم: ________________", "توقيع البائع: ________________" })
        {
            signatureRow.Cells.Add(new TableCell(new Paragraph(new Run(label))
            {
                TextAlignment = TextAlignment.Center,
                Foreground = Muted,
                FontSize = baseFont,
                Margin = new Thickness(0)
            })
            { Padding = new Thickness(0, 6, 0, 6) });
        }
        signatureGroup.Rows.Add(signatureRow);
        signatureTable.RowGroups.Add(signatureGroup);
        doc.Blocks.Add(signatureTable);

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

        return doc;
    }

    private static Table NewGridTable(Thickness margin) => new()
    {
        CellSpacing = 0,
        Margin = margin,
        BorderBrush = Grid,
        BorderThickness = new Thickness(LineWidth)
    };

    private static TableCell DetailsCard(
        IReadOnlyList<(string Label, string Value)> rows,
        Thickness padding,
        double fontSize,
        double cardWidth,
        Thickness outerPadding)
    {
        const double labelWidth = 105;
        var card = NewGridTable(new Thickness(0));
        card.Columns.Add(new TableColumn { Width = new GridLength(labelWidth) });
        card.Columns.Add(new TableColumn { Width = new GridLength(Math.Max(40, cardWidth - labelWidth)) });

        var group = new TableRowGroup();
        foreach (var (label, value) in rows)
        {
            var row = new TableRow();
            row.Cells.Add(GridCell(
                string.IsNullOrWhiteSpace(label) ? " " : $"{label}:",
                padding,
                fontSize,
                bold: !string.IsNullOrWhiteSpace(label),
                background: HeadBg));
            row.Cells.Add(GridCell(
                string.IsNullOrWhiteSpace(value) ? " " : value,
                padding,
                fontSize,
                isLastColumn: true));
            group.Rows.Add(row);
        }

        card.RowGroups.Add(group);
        var cell = new TableCell { Padding = outerPadding };
        cell.Blocks.Add(card);
        return cell;
    }

    private static void AddInfoRow(List<(string Label, string Value)> rows, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            rows.Add((label, value));
    }

    private static void AddCompactTotals(
        FlowDocument doc,
        IReadOnlyList<(string Label, string Value, bool Emphasize)> entries,
        double contentWidth,
        bool compact,
        double baseFont)
    {
        var fontSize = Math.Max(8.5, baseFont - 1.5);
        var padding = compact ? new Thickness(4, 2, 4, 2) : new Thickness(5, 3, 5, 3);
        var totalsWidth = Math.Min(contentWidth * 0.58, compact ? 360.0 : 400.0);
        var labelWidth = totalsWidth * 0.28;
        var valueWidth = totalsWidth * 0.22;

        var totalsTable = NewGridTable(new Thickness(0));
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(labelWidth) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(valueWidth) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(labelWidth) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(valueWidth) });

        var group = new TableRowGroup();
        for (var i = 0; i < entries.Count; i += 2)
        {
            var row = new TableRow();
            AddAmountPair(row, entries[i], padding, fontSize, isLastColumn: false);
            if (i + 1 < entries.Count)
            {
                AddAmountPair(row, entries[i + 1], padding, fontSize, isLastColumn: true);
            }
            else
            {
                row.Cells.Add(GridCell(" ", padding, fontSize, background: TotalBg));
                row.Cells.Add(GridCell(" ", padding, fontSize, background: TotalBg, isLastColumn: true));
            }

            group.Rows.Add(row);
        }

        totalsTable.RowGroups.Add(group);

        var wrapper = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, compact ? 6 : 8, 0, 0)
        };
        wrapper.Columns.Add(new TableColumn { Width = new GridLength(contentWidth - totalsWidth) });
        wrapper.Columns.Add(new TableColumn { Width = new GridLength(totalsWidth) });
        var wrapGroup = new TableRowGroup();
        var wrapRow = new TableRow();
        wrapRow.Cells.Add(new TableCell { Padding = new Thickness(0) });
        var totalsCell = new TableCell { Padding = new Thickness(0) };
        totalsCell.Blocks.Add(totalsTable);
        wrapRow.Cells.Add(totalsCell);
        wrapGroup.Rows.Add(wrapRow);
        wrapper.RowGroups.Add(wrapGroup);
        doc.Blocks.Add(wrapper);
    }

    private static void AddAmountPair(
        TableRow row,
        (string Label, string Value, bool Emphasize) entry,
        Thickness padding,
        double fontSize,
        bool isLastColumn)
    {
        var background = entry.Emphasize ? HeadBg : TotalBg;
        var size = entry.Emphasize ? fontSize + 0.5 : fontSize;
        row.Cells.Add(GridCell(entry.Label, padding, size, bold: true, background: background));
        row.Cells.Add(GridCell(
            entry.Value,
            padding,
            size,
            bold: true,
            align: TextAlignment.Center,
            background: background,
            isLastColumn: isLastColumn));
    }

    /// <summary>
    /// في الاتجاه من اليمين لليسار العمود الأخير يقع على يسار الصفحة،
    /// لذلك يحتاج حدّاً أيسر وإلا يختفي الإطار الأيسر للجدول.
    /// </summary>
    private static Thickness InnerBorder(bool isLastColumn) =>
        isLastColumn
            ? new Thickness(LineWidth, 0, 0, LineWidth)
            : new Thickness(0, 0, LineWidth, LineWidth);

    /// <param name="align">null = بداية السطر حسب اتجاه المستند (يمين في العربية).</param>
    private static TableCell GridCell(
        string text,
        Thickness padding,
        double fontSize,
        bool bold = false,
        TextAlignment? align = null,
        Brush? background = null,
        Brush? foreground = null,
        int columnSpan = 1,
        bool isLastColumn = false)
    {
        var paragraph = new Paragraph(new Run(text))
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            FontSize = fontSize,
            Foreground = foreground ?? Ink,
            Margin = new Thickness(0)
        };
        if (align.HasValue)
            paragraph.TextAlignment = align.Value;

        var cell = new TableCell(paragraph)
        {
            Padding = padding,
            BorderBrush = Grid,
            BorderThickness = InnerBorder(isLastColumn),
            ColumnSpan = columnSpan
        };
        if (background is not null)
            cell.Background = background;
        return cell;
    }

    private static void AddHeading(FlowDocument doc, string text, double baseFont, bool compact) =>
        doc.Blocks.Add(new Paragraph(new Run(text))
        {
            FontSize = baseFont + 1.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, compact ? 8 : 14, 0, compact ? 3 : 5)
        });

    private static void AddBullet(FlowDocument doc, string label, string value, double baseFont)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(14, 1, 14, 1),
            FontSize = baseFont,
            LineHeight = baseFont + 6
        };
        paragraph.Inlines.Add(new Run($"• {label}: ") { FontWeight = FontWeights.Bold });
        paragraph.Inlines.Add(new Run(value));
        doc.Blocks.Add(paragraph);
    }

    private static string FormatNumber(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("N0") : value.ToString("N2");
}
