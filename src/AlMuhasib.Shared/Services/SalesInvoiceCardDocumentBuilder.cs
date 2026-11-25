using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// طباعة فاتورة المبيعات بشكل البطاقات: شارة الرقم، بطاقتا العميل والفاتورة،
/// جدول بترويسة داكنة، ملاحظة، صناديق المبالغ، ثم التوقيع المتقطع.
/// النص أسود والحدود داكنة حتى تبقى الفاتورة واضحة عند الطباعة العادية بدون ألوان.
/// </summary>
public static class SalesInvoiceCardDocumentBuilder
{
    private static readonly FontFamily Font = new("Segoe UI, Tahoma, Arial");
    private static readonly SolidColorBrush Navy = Brush(0x0E, 0x4D, 0x8B);
    private static readonly SolidColorBrush NavyText = Brush(0x0B, 0x2C, 0x4A);
    private static readonly SolidColorBrush Ink = Brush(0x1A, 0x1A, 0x1A);
    private static readonly SolidColorBrush CardFill = Brush(0xF7, 0xFB, 0xFE);
    private static readonly SolidColorBrush HeaderFill = Brush(0xE6, 0xEE, 0xF5);
    private static readonly SolidColorBrush AltRow = Brush(0xF4, 0xF7, 0xFB);
    private static readonly SolidColorBrush Line = Brush(0x8A, 0x9B, 0xAD);
    private static readonly SolidColorBrush HeaderLine = Brush(0x3D, 0x6F, 0x9A);
    private static readonly SolidColorBrush NoteFill = Brush(0xFF, 0xF8, 0xE6);
    private static readonly SolidColorBrush NoteBorder = Brush(0x8A, 0x6A, 0x12);
    private static readonly SolidColorBrush TotalFill = Brush(0xE7, 0xF1, 0xFA);
    private static readonly SolidColorBrush RemainBorder = Brush(0x8E, 0x24, 0x24);
    private static readonly SolidColorBrush RemainText = Brush(0x6D, 0x1B, 0x1B);

    private const double LineWidth = 1.15;

    public static FlowDocument Build(InvoicePrintModel m)
    {
        var pageSize = InvoicePaperSizes.GetPageSize(m.PaperSize);
        var scale = pageSize.Width / InvoicePaperSizes.ReferenceWidth;
        var compact = m.Items.Count > 18;
        var baseFont = (compact ? 12.5 : 13.5) * scale;
        var pagePadding = Scale(compact ? new Thickness(28, 16, 28, 16) : new Thickness(36, 20, 36, 20), scale);
        var contentWidth = Math.Max(200, pageSize.Width - pagePadding.Left - pagePadding.Right - 4);
        var currency = string.IsNullOrWhiteSpace(m.CurrencyLabel) ? "د.ع" : m.CurrencyLabel;
        var branding = PrintBrandingProvider.Current;

        var doc = new FlowDocument
        {
            FontFamily = Font,
            FontSize = baseFont,
            Foreground = Ink,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = pageSize.Width,
            PageHeight = pageSize.Height,
            PagePadding = pagePadding,
            ColumnWidth = contentWidth
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);
        doc.Blocks.Add(Ui(BuildMasthead(m, branding, contentWidth, scale, compact), Scale(new Thickness(0, 2, 0, 12), scale)));
        doc.Blocks.Add(Ui(BuildInfoCards(m, contentWidth, scale, baseFont), Scale(new Thickness(0, 0, 0, 12), scale)));

        doc.Blocks.Add(new Paragraph(new Run(m.HideAmounts ? "تفاصيل المواد" : "بنود الفاتورة"))
        {
            FontFamily = Font,
            FontSize = (baseFont + 2) * (compact ? 0.98 : 1),
            FontWeight = FontWeights.Bold,
            Foreground = NavyText,
            Margin = Scale(new Thickness(0, 2, 0, 6), scale)
        });

        doc.Blocks.Add(BuildItemsTable(m, contentWidth, scale, baseFont, compact, currency));

        if (m.Schedule is { Count: > 0 })
        {
            doc.Blocks.Add(new Paragraph(new Run("جدول الأقساط"))
            {
                FontFamily = Font,
                FontSize = (baseFont + 2) * (compact ? 0.98 : 1),
                FontWeight = FontWeights.Bold,
                Foreground = NavyText,
                Margin = Scale(new Thickness(0, 12, 0, 6), scale)
            });
            doc.Blocks.Add(BuildScheduleTable(m, contentWidth, scale, baseFont));
            doc.Blocks.Add(BuildScheduleSummary(m, scale, baseFont, currency));
        }

        if (!string.IsNullOrWhiteSpace(m.Notes))
            doc.Blocks.Add(Ui(BuildNote(m.Notes, contentWidth, scale, baseFont), Scale(new Thickness(0, 10, 0, 0), scale)));

        if (!m.HideAmounts)
            doc.Blocks.Add(Ui(BuildTotals(m, contentWidth, scale, baseFont, currency), Scale(new Thickness(0, 10, 0, 0), scale)));

        doc.Blocks.Add(Ui(BuildSignatures(contentWidth, scale, baseFont), Scale(new Thickness(0, compact ? 12 : 18, 0, 0), scale)));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

        return doc;
    }

    private static UIElement BuildMasthead(
        InvoicePrintModel m,
        AlMuhasib.Core.Models.PrintBrandingSnapshot branding,
        double contentWidth,
        double scale,
        bool compact)
    {
        var showCompany = !branding.HasHeaderContent && !string.IsNullOrWhiteSpace(branding.CompanyName);
        var headline = showCompany ? branding.CompanyName : m.Title;
        var pill = showCompany
            ? $"{m.Title}   ·   {m.Date:yyyy/MM/dd}"
            : m.Date.ToString("yyyy/MM/dd");
        if (!showCompany && m.CreditDueDate.HasValue)
            pill += $"    الاستحقاق {m.CreditDueDate:yyyy/MM/dd}";

        var root = new StackPanel { Width = contentWidth, FlowDirection = FlowDirection.RightToLeft };
        var row = new Grid { FlowDirection = FlowDirection.RightToLeft };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150 * scale) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150 * scale) });

        var badge = new Border
        {
            Background = Brushes.White,
            BorderBrush = Navy,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(14 * scale),
            Padding = Scale(new Thickness(8, 8, 8, 8), scale),
            MinHeight = 68 * scale,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Children =
                {
                    Caption("رقم الفاتورة", 11 * scale, NavyText),
                    Caption(string.IsNullOrWhiteSpace(m.InvoiceNumber) ? "—" : m.InvoiceNumber, compact ? 14 * scale : 16 * scale, NavyText, FontWeights.Bold)
                }
            }
        };
        Grid.SetColumn(badge, 0);
        row.Children.Add(badge);

        var center = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = Scale(new Thickness(10, 0, 10, 0), scale)
        };
        center.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(headline) ? m.Title : headline,
            FontFamily = Font,
            FontSize = (compact ? 18 : 22) * scale,
            FontWeight = FontWeights.Bold,
            Foreground = NavyText,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });
        center.Children.Add(new Border
        {
            Background = TotalFill,
            BorderBrush = Navy,
            BorderThickness = new Thickness(1.4),
            CornerRadius = new CornerRadius(16 * scale),
            Padding = Scale(new Thickness(14, 5, 14, 5), scale),
            Margin = Scale(new Thickness(0, 8, 0, 0), scale),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = pill,
                FontFamily = Font,
                FontSize = 12.5 * scale,
                FontWeight = FontWeights.SemiBold,
                Foreground = NavyText,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            }
        });
        Grid.SetColumn(center, 1);
        row.Children.Add(center);

        root.Children.Add(row);
        root.Children.Add(new Border
        {
            Height = 2.4 * scale,
            Background = Navy,
            Margin = Scale(new Thickness(0, 10, 0, 0), scale)
        });
        return root;
    }

    private static UIElement BuildInfoCards(InvoicePrintModel m, double contentWidth, double scale, double baseFont)
    {
        var customer = new List<(string Label, string Value)>();
        Add(customer, "الاسم", m.PartyName);
        Add(customer, "الهاتف", m.PartyPhone);
        Add(customer, "العنوان", m.PartyAddress);
        Add(customer, "البريد", m.PartyEmail);
        Add(customer, "رقم العميل", m.FileNumber);
        if (customer.Count == 0)
            customer.Add(("الاسم", "—"));

        var invoice = new List<(string Label, string Value)>();
        Add(invoice, "رقم الفاتورة", m.InvoiceNumber);
        Add(invoice, "التاريخ", m.Date.ToString("yyyy/MM/dd"));
        if (m.CreditDueDate.HasValue)
            Add(invoice, "تاريخ الاستحقاق", m.CreditDueDate.Value.ToString("yyyy/MM/dd"));
        Add(invoice, "طريقة الدفع", m.PaymentMethod);
        if (m.NumberOfInstallments is > 0)
            Add(invoice, "عدد الأقساط", m.NumberOfInstallments.Value.ToString());
        if (m.InstallmentAmount is > 0)
            Add(invoice, "مبلغ القسط", FormatNumber(m.InstallmentAmount.Value));
        Add(invoice, "المخزن", m.WarehouseName);
        Add(invoice, "المندوب", m.SalesRepresentativeName);
        Add(invoice, "هاتف المندوب", m.SalesRepresentativePhone);
        Add(invoice, "السائق", m.DriverName);
        if (invoice.Count == 0)
            invoice.Add(("رقم الفاتورة", "—"));

        var gap = 10 * scale;
        var grid = new Grid
        {
            Width = contentWidth,
            FlowDirection = FlowDirection.RightToLeft
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var partyTitle = string.IsNullOrWhiteSpace(m.PartyLabel) ? "بيانات العميل" : $"بيانات {m.PartyLabel}";
        var right = InfoCard(partyTitle, customer, scale, baseFont);
        var left = InfoCard("بيانات الفاتورة", invoice, scale, baseFont);
        Grid.SetColumn(right, 0);
        Grid.SetColumn(left, 2);
        grid.Children.Add(right);
        grid.Children.Add(left);
        return grid;
    }

    private static Border InfoCard(string title, IReadOnlyList<(string Label, string Value)> rows, double scale, double baseFont)
    {
        // LeftToRight هنا حتى تبقى HorizontalAlignment.Right هي الجهة اليمنى فعلاً
        // ولا تنعكس مع اتجاه الصفحة.
        var stack = new StackPanel { FlowDirection = FlowDirection.LeftToRight };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontFamily = Font,
            FontSize = baseFont + 1.5,
            FontWeight = FontWeights.Bold,
            Foreground = NavyText,
            TextAlignment = TextAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Scale(new Thickness(0, 0, 0, 6), scale)
        });
        stack.Children.Add(new Border
        {
            Height = 1.6 * scale,
            Background = Navy,
            Margin = Scale(new Thickness(28, 0, 28, 8), scale)
        });

        foreach (var (label, value) in rows)
        {
            var line = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = Scale(new Thickness(0, 2, 0, 2), scale)
            };
            line.Children.Add(new TextBlock
            {
                Text = value,
                FontFamily = Font,
                FontSize = baseFont,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ink,
                FlowDirection = FlowDirection.RightToLeft,
                TextAlignment = TextAlignment.Right,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 280 * scale,
                VerticalAlignment = VerticalAlignment.Center
            });
            line.Children.Add(new TextBlock
            {
                Text = $"{label}:",
                FontFamily = Font,
                FontSize = baseFont,
                FontWeight = FontWeights.SemiBold,
                Foreground = NavyText,
                FlowDirection = FlowDirection.RightToLeft,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = Scale(new Thickness(8, 0, 0, 0), scale)
            });
            stack.Children.Add(line);
        }

        return new Border
        {
            CornerRadius = new CornerRadius(12 * scale),
            BorderBrush = Navy,
            BorderThickness = new Thickness(1.4),
            Background = CardFill,
            Padding = Scale(new Thickness(12, 10, 12, 10), scale),
            Child = stack,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private static Table BuildItemsTable(
        InvoicePrintModel m,
        double contentWidth,
        double scale,
        double baseFont,
        bool compact,
        string currency)
    {
        var layout = InvoicePrintLayoutHelper.Resolve(m, compact);
        var titles = new List<string> { "ت" };
        titles.AddRange(InvoicePrintLayoutHelper.BuildColumnTitles(layout, currency));
        // الوصف في القالب القديم يتصدر الرقم؛ هنا الرقم عمود مستقل.
        if (titles.Count > 1)
            titles[1] = "الوصف";

        var numeric = InvoicePrintLayoutHelper.BuildNumericWidths(layout).Select(w => w * scale).ToList();
        var numberWidth = 36 * scale;
        var fixedWidth = numberWidth + numeric.Sum();
        var minDescription = 120 * scale;
        if (fixedWidth > contentWidth - minDescription && fixedWidth > 0)
        {
            var factor = (contentWidth - minDescription) / fixedWidth;
            numberWidth *= factor;
            numeric = numeric.Select(w => w * factor).ToList();
            fixedWidth = numberWidth + numeric.Sum();
        }

        var padding = Scale(compact ? new Thickness(4, 3, 4, 3) : new Thickness(6, 5, 6, 5), scale);
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0),
            BorderBrush = Navy,
            BorderThickness = new Thickness(LineWidth),
            FlowDirection = FlowDirection.RightToLeft
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(numberWidth) });
        table.Columns.Add(new TableColumn { Width = new GridLength(Math.Max(80, contentWidth - fixedWidth)) });
        foreach (var width in numeric)
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });

        var header = new TableRowGroup();
        var headerRow = new TableRow();
        for (var i = 0; i < titles.Count; i++)
        {
            headerRow.Cells.Add(Cell(
                titles[i],
                padding,
                baseFont,
                bold: true,
                align: TextAlignment.Center,
                background: HeaderFill,
                foreground: NavyText,
                border: HeaderLine,
                isLast: i == titles.Count - 1));
        }
        header.Rows.Add(headerRow);
        table.RowGroups.Add(header);

        var body = new TableRowGroup();
        var index = 0;
        foreach (var item in m.Items)
        {
            var row = new TableRow();
            var background = index % 2 == 1 ? AltRow : Brushes.White;
            var last = layout.HideAmounts;
            row.Cells.Add(Cell(item.Number.ToString(), padding, baseFont, align: TextAlignment.Center, background: background, isLast: false));
            row.Cells.Add(Cell(
                InvoicePrintLayoutHelper.FormatItemName(item, m, layout.HideAmounts && layout.ShowWarehouse, includeNumber: false),
                padding,
                baseFont,
                background: background,
                isLast: false));
            row.Cells.Add(Cell(FormatNumber(item.Quantity), padding, baseFont, align: TextAlignment.Center, background: background, isLast: last));

            if (!layout.HideAmounts)
            {
                if (layout.ShowWarehouse)
                {
                    row.Cells.Add(Cell(string.IsNullOrWhiteSpace(item.WarehouseName) ? "—" : item.WarehouseName, padding, baseFont, align: TextAlignment.Center, background: background));
                }

                row.Cells.Add(Cell(FormatNumber(item.UnitPrice), padding, baseFont, align: TextAlignment.Center, background: background));
                if (layout.ShowLineDiscount)
                {
                    row.Cells.Add(Cell(
                        InvoicePrintLayoutHelper.FormatDiscountPercent(item.DiscountPercent),
                        padding,
                        baseFont,
                        align: TextAlignment.Center,
                        background: background));
                }

                row.Cells.Add(Cell(FormatNumber(item.TotalPrice), padding, baseFont, bold: true, align: TextAlignment.Center, background: background, isLast: true));
            }

            body.Rows.Add(row);
            index++;
        }

        if (m.Items.Count == 0)
        {
            var empty = new TableRow();
            empty.Cells.Add(Cell("لا توجد بنود", padding, baseFont, align: TextAlignment.Center, columnSpan: titles.Count, isLast: true));
            body.Rows.Add(empty);
        }

        table.RowGroups.Add(body);
        return table;
    }

    private static Table BuildScheduleTable(InvoicePrintModel m, double contentWidth, double scale, double baseFont)
    {
        var titles = new[]
        {
            "ت", "تاريخ الاستحقاق", "مبلغ القسط", "مبلغ التأمين", "مبلغ الخصم",
            "المسدد", "الباقي", "تاريخ التسديد", "الحالة", "التأخير"
        };
        var weights = new[] { 26d, 78d, 62d, 58d, 58d, 58d, 58d, 72d, 54d, 46d };
        var weightSum = weights.Sum();
        var font = Math.Max(8.5, baseFont * 0.78);
        var padding = Scale(new Thickness(3, 3, 3, 3), scale);

        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0),
            BorderBrush = Navy,
            BorderThickness = new Thickness(LineWidth),
            FlowDirection = FlowDirection.RightToLeft
        };
        foreach (var weight in weights)
            table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * weight / weightSum) });

        var header = new TableRowGroup();
        var headerRow = new TableRow();
        for (var i = 0; i < titles.Length; i++)
        {
            headerRow.Cells.Add(Cell(
                titles[i],
                padding,
                font,
                bold: true,
                align: TextAlignment.Center,
                background: HeaderFill,
                foreground: NavyText,
                border: HeaderLine,
                isLast: i == titles.Length - 1));
        }
        header.Rows.Add(headerRow);
        table.RowGroups.Add(header);

        var body = new TableRowGroup();
        var index = 0;
        foreach (var row in m.Schedule!)
        {
            var background = row.StatusText switch
            {
                "مسدد" => Brush(0xE8, 0xF5, 0xE9),
                "متأخر" => Brush(0xFF, 0xEB, 0xEE),
                _ => index % 2 == 1 ? AltRow : Brushes.White
            };
            var values = new[]
            {
                row.Number.ToString(),
                row.DueDate.ToString("yyyy/MM/dd"),
                FormatNumber(row.Amount),
                "0",
                "0",
                FormatNumber(row.PaidAmount),
                FormatNumber(row.RemainingAmount),
                row.PaymentDate?.ToString("yyyy/MM/dd") ?? "",
                row.StatusText,
                row.DelayDays?.ToString() ?? ""
            };
            var data = new TableRow();
            for (var i = 0; i < values.Length; i++)
            {
                data.Cells.Add(Cell(
                    values[i],
                    padding,
                    font,
                    align: TextAlignment.Center,
                    background: background,
                    isLast: i == values.Length - 1));
            }
            body.Rows.Add(data);
            index++;
        }
        table.RowGroups.Add(body);
        return table;
    }

    private static Paragraph BuildScheduleSummary(InvoicePrintModel m, double scale, double baseFont, string currency)
    {
        decimal totalPaid = 0;
        decimal totalRemaining = 0;
        var paidCount = 0;
        foreach (var row in m.Schedule!)
        {
            totalPaid += row.PaidAmount;
            totalRemaining += row.RemainingAmount;
            if (row.StatusText is "مسدد" or "مسدد جزئياً" && row.PaidAmount > 0)
                paidCount++;
        }

        var paidPct = m.GrandTotal > 0 ? totalPaid * 100m / m.GrandTotal : 0;
        return new Paragraph
        {
            FontFamily = Font,
            FontSize = baseFont * 0.92,
            Foreground = NavyText,
            Margin = Scale(new Thickness(0, 6, 0, 0), scale),
            Inlines =
            {
                new Run($"إجمالي المسدد: {Money(totalPaid, currency)}") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"إجمالي المتبقي: {Money(totalRemaining, currency)}") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"أقساط مسددة: {paidCount} من {m.Schedule.Count}") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"نسبة التحصيل: {paidPct:N1}%")
            }
        };
    }

    private static UIElement BuildNote(string notes, double contentWidth, double scale, double baseFont)
    {
        return new Border
        {
            Width = contentWidth,
            Background = NoteFill,
            BorderBrush = NoteBorder,
            BorderThickness = new Thickness(1.3),
            CornerRadius = new CornerRadius(10 * scale),
            Padding = Scale(new Thickness(12, 8, 12, 8), scale),
            Child = new TextBlock
            {
                FontFamily = Font,
                FontSize = baseFont,
                Foreground = Ink,
                TextWrapping = TextWrapping.Wrap,
                FlowDirection = FlowDirection.RightToLeft,
                Text = "ملاحظة: " + notes
            }
        };
    }

    private static UIElement BuildTotals(InvoicePrintModel m, double contentWidth, double scale, double baseFont, string currency)
    {
        var extras = new List<(string Label, string Value)>();
        void Line(string label, decimal amount, bool show = true)
        {
            if (show)
                extras.Add((label, Money(amount, currency)));
        }

        Line("المجموع الفرعي", m.Subtotal, m.Subtotal != m.GrandTotal || m.DiscountAmount != 0 || m.TransportFeeAmount != 0 || m.TaxAmount != 0);
        if (m.DiscountAmount != 0)
        {
            Line("الخصم", m.DiscountAmount);
            Line("المبلغ بعد الخصم", m.Subtotal - m.DiscountAmount);
        }
        if (m.TransportFeeAmount != 0)
            Line("أجور النقل", m.TransportFeeAmount);
        if (m.PurchaseExpenseAmount != 0)
            Line("مصاريف المشتريات", m.PurchaseExpenseAmount);
        if (m.TaxRate != 0 || m.TaxAmount != 0)
            Line(m.TaxRate != 0 ? $"الضريبة {m.TaxRate:0.##}%" : "الضريبة", m.TaxAmount);
        if (m.CompanyFeeAmount is { } fee && fee != 0)
            Line("نسبة الشركة", fee);
        if (m.RoundingAmount != 0)
            Line("التقريب", m.RoundingAmount);
        if (m.PaidAmount != 0 && m.RemainingAmount != 0)
            Line("المدفوع", m.PaidAmount);

        var balance = m.GetCustomerOutstandingBalanceDisplay();
        if (!string.IsNullOrWhiteSpace(balance))
            extras.Add(("رصيد العميل (الباقي عليه)", balance));

        var stack = new StackPanel { Width = contentWidth, FlowDirection = FlowDirection.RightToLeft };
        if (extras.Count > 0)
        {
            var card = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            foreach (var (label, value) in extras)
                card.Children.Add(AmountLine(label, value, baseFont, scale, emphasize: false));

            stack.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(12 * scale),
                BorderBrush = Navy,
                BorderThickness = new Thickness(1.3),
                Background = Brushes.White,
                Padding = Scale(new Thickness(8, 6, 8, 6), scale),
                Margin = Scale(new Thickness(0, 0, 0, 8), scale),
                Child = card
            });
        }

        var showRemaining = m.RemainingAmount != 0 || (m.PaidAmount != 0 && m.PaidAmount != m.GrandTotal);
        var secondLabel = m.RemainingAmount != 0 ? "المتبقي" : "المدفوع";
        var secondValue = m.RemainingAmount != 0 ? Money(m.RemainingAmount, currency) : Money(m.PaidAmount, currency);
        var secondIsRemain = m.RemainingAmount != 0;

        var pills = new Grid { FlowDirection = FlowDirection.RightToLeft };
        if (showRemaining)
        {
            pills.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pills.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10 * scale) });
            pills.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var total = AmountPill("الإجمالي المستحق", Money(m.GrandTotal, currency), scale, baseFont, filled: true, remain: false);
            var other = AmountPill(secondLabel, secondValue, scale, baseFont, filled: false, remain: secondIsRemain);
            Grid.SetColumn(total, 0);
            Grid.SetColumn(other, 2);
            pills.Children.Add(total);
            pills.Children.Add(other);
        }
        else
        {
            pills.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pills.Children.Add(AmountPill("الإجمالي المستحق", Money(m.GrandTotal, currency), scale, baseFont, filled: true, remain: false));
        }

        stack.Children.Add(pills);
        return stack;
    }

    private static Grid AmountLine(string label, string value, double baseFont, double scale, bool emphasize)
    {
        var line = new Grid
        {
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Scale(new Thickness(4, 3, 4, 3), scale)
        };
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var labelBlock = new TextBlock
        {
            Text = label,
            FontFamily = Font,
            FontSize = baseFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = NavyText,
            VerticalAlignment = VerticalAlignment.Center
        };
        var valueBlock = new TextBlock
        {
            Text = value,
            FontFamily = Font,
            FontSize = emphasize ? baseFont + 1 : baseFont,
            FontWeight = FontWeights.Bold,
            Foreground = Ink,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(valueBlock, 1);
        line.Children.Add(labelBlock);
        line.Children.Add(valueBlock);
        return line;
    }

    private static Border AmountPill(string label, string value, double scale, double baseFont, bool filled, bool remain)
    {
        var text = new TextBlock
        {
            FontFamily = Font,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FlowDirection = FlowDirection.RightToLeft,
            Foreground = remain ? RemainText : NavyText
        };
        text.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.SemiBold, FontSize = baseFont });
        text.Inlines.Add(new Run(value) { FontWeight = FontWeights.Bold, FontSize = baseFont + 2 });

        return new Border
        {
            CornerRadius = new CornerRadius(12 * scale),
            Background = filled ? TotalFill : Brushes.White,
            BorderBrush = remain ? RemainBorder : Navy,
            BorderThickness = new Thickness(filled ? 1.3 : 1.6),
            Padding = Scale(new Thickness(12, 10, 12, 10), scale),
            Child = text
        };
    }

    private static UIElement BuildSignatures(double contentWidth, double scale, double baseFont)
    {
        var grid = new Grid
        {
            Width = contentWidth,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(0)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12 * scale) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var receiver = SignatureBox("توقيع المستلم", scale, baseFont);
        var seller = SignatureBox("توقيع البائع", scale, baseFont);
        Grid.SetColumn(receiver, 0);
        Grid.SetColumn(seller, 2);
        grid.Children.Add(receiver);
        grid.Children.Add(seller);
        return grid;
    }

    private static Grid SignatureBox(string label, double scale, double baseFont)
    {
        var box = new Grid { Height = 96 * scale, FlowDirection = FlowDirection.RightToLeft };
        box.Children.Add(new Rectangle
        {
            Stroke = Navy,
            StrokeThickness = 1.35,
            StrokeDashArray = new DoubleCollection([5, 3.2]),
            RadiusX = 12 * scale,
            RadiusY = 12 * scale,
            Fill = Brushes.Transparent
        });
        box.Children.Add(new TextBlock
        {
            Text = label,
            FontFamily = Font,
            FontSize = baseFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = NavyText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = Scale(new Thickness(0, 10, 0, 0), scale)
        });
        return box;
    }

    private static TableCell Cell(
        string text,
        Thickness padding,
        double fontSize,
        bool bold = false,
        TextAlignment? align = null,
        Brush? background = null,
        Brush? foreground = null,
        Brush? border = null,
        int columnSpan = 1,
        bool isLast = false)
    {
        var paragraph = TextParagraph(text, fontSize, foreground ?? Ink, bold ? FontWeights.Bold : FontWeights.Normal, align);
        return new TableCell(paragraph)
        {
            Padding = padding,
            BorderBrush = border ?? Line,
            BorderThickness = isLast
                ? new Thickness(LineWidth, 0, 0, LineWidth)
                : new Thickness(0, 0, LineWidth, LineWidth),
            ColumnSpan = columnSpan,
            Background = background
        };
    }

    private static Paragraph TextParagraph(string text, double fontSize, Brush foreground, FontWeight weight, TextAlignment? align)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0),
            FontFamily = Font,
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = foreground,
            FlowDirection = FlowDirection.RightToLeft
        };
        if (align.HasValue)
            paragraph.TextAlignment = align.Value;

        var lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run(lines[i]));
        }

        return paragraph;
    }

    private static TextBlock Caption(string text, double size, Brush foreground, FontWeight? weight = null) => new()
    {
        Text = text,
        FontFamily = Font,
        FontSize = size,
        FontWeight = weight ?? FontWeights.SemiBold,
        Foreground = foreground,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    private static BlockUIContainer Ui(UIElement child, Thickness margin) => new(child) { Margin = margin };

    private static void Add(List<(string Label, string Value)> rows, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value != "—")
            rows.Add((label, value.Trim()));
    }

    private static string Money(decimal value, string currency) => $"{FormatNumber(value)} {currency}";

    private static string FormatNumber(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("N0") : value.ToString("N2");

    private static Thickness Scale(Thickness thickness, double scale) =>
        new(thickness.Left * scale, thickness.Top * scale, thickness.Right * scale, thickness.Bottom * scale);

    private static SolidColorBrush Brush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
