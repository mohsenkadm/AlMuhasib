using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// قالب A4 لفواتير البيع/الشراء/المرتجع: عنوان مركزي، جدول بيانات الفاتورة،
/// بيانات الجهة والمندوب، ثم جدول البنود مع صفوف المجاميع داخل نفس الجدول.
/// العملة تُكتب في رأس الأعمدة وليس بجانب كل رقم لتفادي تشابك الأرقام مع النص العربي.
/// </summary>
public static class ModernInvoiceDocumentBuilder
{
    public const double PageWidth = 793.7;   // A4 @ 96 DPI
    public const double PageHeight = 1122.5;

    private const double LineWidth = 0.8;

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
        var baseFont = compact ? 9.5 : 10.5;
        var cellPadding = compact ? new Thickness(6, 3, 6, 3) : new Thickness(8, 5, 8, 5);
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
            FontSize = compact ? 18 : 21,
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
                Margin = new Thickness(0, 0, 0, compact ? 10 : 16)
            });
        }

        // ── جدول بيانات الفاتورة ──
        var metaRows = new List<(string Label, string Value)>
        {
            ("رقم الفاتورة", m.InvoiceNumber),
            ("تاريخ الفاتورة", m.Date.ToString("yyyy/MM/dd"))
        };
        if (m.CreditDueDate.HasValue)
            metaRows.Add(("تاريخ الاستحقاق", m.CreditDueDate.Value.ToString("yyyy/MM/dd")));
        if (!hideAmounts && !string.IsNullOrWhiteSpace(m.PaymentMethod))
            metaRows.Add(("طريقة الدفع", m.PaymentMethod));
        if (!string.IsNullOrWhiteSpace(m.WarehouseName))
            metaRows.Add(("المخزن", m.WarehouseName));
        if (!string.IsNullOrWhiteSpace(m.FileNumber))
            metaRows.Add(("رقم الملف", m.FileNumber!));
        if (!string.IsNullOrWhiteSpace(m.DriverName))
            metaRows.Add(("السائق", m.DriverName!));

        // صندوق بيانات مضغوط بعرض جزء من الصفحة (كما في القوالب العالمية) لا يمتد بالكامل.
        var metaLabelWidth = compact ? 150.0 : 170.0;
        var metaValueWidth = Math.Min(contentWidth - metaLabelWidth, compact ? 230.0 : 265.0);
        var metaTable = NewGridTable(new Thickness(0));
        metaTable.Columns.Add(new TableColumn { Width = new GridLength(metaLabelWidth) });
        metaTable.Columns.Add(new TableColumn { Width = new GridLength(metaValueWidth) });
        var metaGroup = new TableRowGroup();
        foreach (var (label, value) in metaRows)
        {
            var row = new TableRow();
            row.Cells.Add(GridCell($"{label}:", cellPadding, baseFont, bold: true, background: HeadBg));
            row.Cells.Add(GridCell(string.IsNullOrWhiteSpace(value) ? "—" : value, cellPadding, baseFont, isLastColumn: true));
            metaGroup.Rows.Add(row);
        }
        metaTable.RowGroups.Add(metaGroup);
        doc.Blocks.Add(metaTable);

        // ── بطاقة العميل يميناً، وبطاقة المندوب يساراً فقط عند وجود مندوب فعلي ──
        var hasSalesRepresentative =
            !string.IsNullOrWhiteSpace(m.SalesRepresentativeName)
            || !string.IsNullOrWhiteSpace(m.SalesRepresentativePhone)
            || !string.IsNullOrWhiteSpace(m.SalesRepresentativeEmail);

        var detailsGap = compact ? 8.0 : 12.0;
        var detailsWidth = hasSalesRepresentative
            ? (contentWidth - detailsGap) / 2
            : Math.Min(contentWidth, compact ? 340.0 : 390.0);
        var detailsLayout = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, compact ? 8 : 14, 0, 0)
        };
        detailsLayout.Columns.Add(new TableColumn { Width = new GridLength(detailsWidth) });
        if (hasSalesRepresentative)
            detailsLayout.Columns.Add(new TableColumn { Width = new GridLength(detailsWidth) });

        var customerRows = new List<(string Label, string Value)>
        {
            ("الاسم", string.IsNullOrWhiteSpace(m.PartyName) ? "—" : m.PartyName),
            ("الهاتف", string.IsNullOrWhiteSpace(m.PartyPhone) ? "—" : m.PartyPhone!),
            ("العنوان", string.IsNullOrWhiteSpace(m.PartyAddress) ? "—" : m.PartyAddress!),
            ("البريد الإلكتروني", string.IsNullOrWhiteSpace(m.PartyEmail) ? "—" : m.PartyEmail!)
        };

        var detailsGroup = new TableRowGroup();
        var detailsRow = new TableRow();
        detailsRow.Cells.Add(DetailsCard(
            $"بيانات {m.PartyLabel}",
            customerRows,
            cellPadding,
            baseFont,
            hasSalesRepresentative ? detailsWidth - (detailsGap / 2) : detailsWidth,
            hasSalesRepresentative ? new Thickness(0, 0, detailsGap / 2, 0) : new Thickness(0)));

        if (hasSalesRepresentative)
        {
            var representativeRows = new List<(string Label, string Value)>
            {
                ("الاسم", string.IsNullOrWhiteSpace(m.SalesRepresentativeName) ? "—" : m.SalesRepresentativeName!),
                ("الهاتف", string.IsNullOrWhiteSpace(m.SalesRepresentativePhone) ? "—" : m.SalesRepresentativePhone!),
                ("البريد الإلكتروني", string.IsNullOrWhiteSpace(m.SalesRepresentativeEmail) ? "—" : m.SalesRepresentativeEmail!),
                ("", "")
            };
            detailsRow.Cells.Add(DetailsCard(
                "مندوب المبيعات",
                representativeRows,
                cellPadding,
                baseFont,
                detailsWidth - (detailsGap / 2),
                new Thickness(detailsGap / 2, 0, 0, 0)));
        }

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
            // جدول مستقل بعمودين بنفس عرض عمود "الإجمالي" ليبقى الخط الرأسي متصلاً مع جدول البنود.
            var valueWidth = numericWidths[^1];
            var totalsTable = NewGridTable(new Thickness(0));
            totalsTable.Columns.Add(new TableColumn { Width = new GridLength(contentWidth - valueWidth) });
            totalsTable.Columns.Add(new TableColumn { Width = new GridLength(valueWidth) });
            var totalsGroup = new TableRowGroup();

            void AddTotalRow(string label, decimal value, bool emphasize = false)
            {
                var background = emphasize ? HeadBg : TotalBg;
                var fontSize = emphasize ? baseFont + 1 : baseFont;
                var row = new TableRow();
                row.Cells.Add(GridCell(label, cellPadding, fontSize, bold: true, background: background));
                row.Cells.Add(GridCell(
                    FormatNumber(value),
                    cellPadding,
                    fontSize,
                    bold: true,
                    align: TextAlignment.Center,
                    background: background,
                    isLastColumn: true));
                totalsGroup.Rows.Add(row);
            }

            AddTotalRow("المجموع الفرعي", m.Subtotal);
            if (m.DiscountAmount != 0)
            {
                AddTotalRow("الخصم", m.DiscountAmount);
                AddTotalRow("المبلغ بعد الخصم", m.Subtotal - m.DiscountAmount);
            }
            if (m.TransportFeeAmount != 0)
                AddTotalRow("أجور النقل", m.TransportFeeAmount);
            if (m.TaxRate != 0 || m.TaxAmount != 0)
                AddTotalRow(m.TaxRate != 0 ? $"الضريبة {m.TaxRate:0.##}%" : "الضريبة", m.TaxAmount);
            if (m.CompanyFeeAmount is { } fee && fee != 0)
                AddTotalRow("نسبة الشركة", fee);
            if (m.RoundingAmount != 0)
                AddTotalRow("التقريب", m.RoundingAmount);

            AddTotalRow("الإجمالي المستحق", m.GrandTotal, emphasize: true);

            if (m.PaidAmount != 0 || m.RemainingAmount != 0)
            {
                AddTotalRow("المدفوع", m.PaidAmount);
                AddTotalRow("المتبقي", m.RemainingAmount);
            }

            totalsTable.RowGroups.Add(totalsGroup);
            doc.Blocks.Add(totalsTable);
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
        string title,
        IReadOnlyList<(string Label, string Value)> rows,
        Thickness padding,
        double fontSize,
        double cardWidth,
        Thickness outerPadding)
    {
        const double labelWidth = 105;
        var card = NewGridTable(new Thickness(0));
        card.Columns.Add(new TableColumn { Width = new GridLength(labelWidth) });
        card.Columns.Add(new TableColumn { Width = new GridLength(cardWidth - labelWidth) });

        var group = new TableRowGroup();
        var titleRow = new TableRow();
        titleRow.Cells.Add(GridCell(
            title,
            new Thickness(8, 6, 8, 6),
            fontSize + 1,
            bold: true,
            align: TextAlignment.Center,
            background: HeadBg,
            foreground: HeaderInk,
            columnSpan: 2,
            isLastColumn: true));
        group.Rows.Add(titleRow);

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

    /// <summary>
    /// خط فاصل واحد بين الخلايا: أسفل دائماً، وجانب واحد لكل خلية عدا الأخيرة،
    /// حتى لا تتضاعف خطوط الشبكة (سبب عدم وضوح الجدول).
    /// </summary>
    private static Thickness InnerBorder(bool isLastColumn) =>
        isLastColumn ? new Thickness(0, 0, 0, LineWidth) : new Thickness(0, 0, LineWidth, LineWidth);

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
