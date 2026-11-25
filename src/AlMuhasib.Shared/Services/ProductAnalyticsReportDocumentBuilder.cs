using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// طباعة تقرير المنتجات بنفس بطاقات فاتورة المبيعات: حدود كحلية ونص داكن
/// حتى تبقى الورقة مقروءة بالطباعة العادية بدون ألوان.
/// </summary>
public static class ProductAnalyticsReportDocumentBuilder
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

    private const double LineWidth = 1.15;

    public static FlowDocument Build(ProductAnalyticsPrintModel model)
    {
        var pageSize = InvoicePaperSizes.GetPageSize(InvoicePaperSizes.A4Landscape);
        var scale = pageSize.Width / InvoicePaperSizes.ReferenceWidth;
        var baseFont = 10.6 * scale;
        var pagePadding = new Thickness(28 * scale, 16 * scale, 28 * scale, 16 * scale);
        var contentWidth = Math.Max(240, pageSize.Width - pagePadding.Left - pagePadding.Right - 4);

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
        doc.Blocks.Add(Ui(BuildMasthead(model, contentWidth, scale), new Thickness(0, 2, 0, 10)));
        doc.Blocks.Add(Ui(BuildInfoCards(model, contentWidth, scale, baseFont), new Thickness(0, 0, 0, 10)));
        doc.Blocks.Add(Ui(BuildTotalPills(model, contentWidth, scale, baseFont), new Thickness(0, 0, 0, 8)));

        doc.Blocks.Add(SectionTitle("التصنيفات", baseFont, scale));
        doc.Blocks.Add(BuildCategoryTable(model, contentWidth, baseFont));

        doc.Blocks.Add(SectionTitle("تحليلات تشغيلية", baseFont, scale));
        if (model.Result.Insights.Count == 0)
            doc.Blocks.Add(new Paragraph(new Run("لا توجد تحليلات ضمن هذه الفلاتر.")) { Foreground = Ink, FontFamily = Font });
        else
        {
            foreach (var insight in model.Result.Insights)
                doc.Blocks.Add(BuildInsightTable(insight, contentWidth, baseFont));
        }

        var notes = string.Join("\n", new[] { model.CurrencyNote, model.ProfitNote, model.StockAgeNote }
            .Where(line => !string.IsNullOrWhiteSpace(line)));
        if (notes.Length > 0)
            doc.Blocks.Add(Ui(BuildNote(notes, contentWidth, scale, baseFont), new Thickness(0, 10, 0, 0)));

        doc.Blocks.Add(Ui(BuildSignatures(contentWidth, scale, baseFont), new Thickness(0, 16, 0, 0)));
        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        return doc;
    }

    private static UIElement BuildMasthead(ProductAnalyticsPrintModel model, double contentWidth, double scale)
    {
        var root = new StackPanel { Width = contentWidth, FlowDirection = FlowDirection.RightToLeft };
        var row = new Grid { FlowDirection = FlowDirection.RightToLeft };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168 * scale) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168 * scale) });

        var badge = new Border
        {
            Background = Brushes.White,
            BorderBrush = Navy,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(14 * scale),
            Padding = new Thickness(8 * scale),
            MinHeight = 64 * scale,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Children =
                {
                    Caption("الفترة", 10 * scale, NavyText),
                    Caption($"{model.From:yyyy/MM/dd}", 12.5 * scale, NavyText, FontWeights.Bold),
                    Caption($"{model.To:yyyy/MM/dd}", 12.5 * scale, NavyText, FontWeights.Bold)
                }
            }
        };
        Grid.SetColumn(badge, 0);
        row.Children.Add(badge);

        var center = new StackPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10 * scale, 0, 10 * scale, 0)
        };
        center.Children.Add(new TextBlock
        {
            Text = "تحليل المنتجات",
            FontFamily = Font,
            FontSize = 22 * scale,
            FontWeight = FontWeights.Bold,
            Foreground = NavyText,
            TextAlignment = TextAlignment.Center
        });
        center.Children.Add(new Border
        {
            Background = TotalFill,
            BorderBrush = Navy,
            BorderThickness = new Thickness(1.4),
            CornerRadius = new CornerRadius(16 * scale),
            Padding = new Thickness(14 * scale, 5 * scale, 14 * scale, 5 * scale),
            Margin = new Thickness(0, 8 * scale, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock
            {
                Text = $"طُبع {DateTime.Now:yyyy/MM/dd HH:mm}",
                FontFamily = Font,
                FontSize = 12 * scale,
                FontWeight = FontWeights.SemiBold,
                Foreground = NavyText
            }
        });
        Grid.SetColumn(center, 1);
        row.Children.Add(center);

        root.Children.Add(row);
        root.Children.Add(new Border
        {
            Height = 2.4 * scale,
            Background = Navy,
            Margin = new Thickness(0, 10 * scale, 0, 0)
        });
        return root;
    }

    private static UIElement BuildInfoCards(ProductAnalyticsPrintModel model, double contentWidth, double scale, double baseFont)
    {
        var result = model.Result;
        var filters = new List<(string Label, string Value)>
        {
            ("من", model.From.ToString("yyyy/MM/dd")),
            ("إلى", model.To.ToString("yyyy/MM/dd")),
            ("المستودع", model.WarehouseName),
            ("التصنيف", model.CategoryName),
            ("المورد", model.SupplierName),
            ("النشاط", model.ActivityName)
        };
        if (!string.IsNullOrWhiteSpace(model.SearchText))
            filters.Add(("البحث", model.SearchText));

        var summary = new List<(string Label, string Value)>
        {
            ("المنتجات", result.ProductCount.ToString("N0")),
            ("التصنيفات", result.Categories.Count.ToString("N0")),
            ("الكمية المباعة", result.GrossQuantitySold.ToString("N0")),
            ("صافي المبيعات", Money(result.NetSales)),
            ("المشتريات", Money(result.PurchaseValue)),
            ("المرتجعات", Money(result.ReturnValue)),
            ("التكلفة", Money(result.Cogs)),
            ("قيمة المخزون", Money(result.StockValue))
        };

        var grid = new Grid { Width = contentWidth, FlowDirection = FlowDirection.RightToLeft };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10 * scale) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = InfoCard("نطاق التقرير", filters, baseFont, scale);
        var right = InfoCard("ملخص الأرقام", summary, baseFont, scale);
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 2);
        grid.Children.Add(left);
        grid.Children.Add(right);
        return grid;
    }

    private static Border InfoCard(string title, IReadOnlyList<(string Label, string Value)> rows, double baseFont, double scale)
    {
        var stack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontFamily = Font,
            FontSize = baseFont + 1.2,
            FontWeight = FontWeights.Bold,
            Foreground = NavyText,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        });
        stack.Children.Add(new Border
        {
            Height = 1.4 * scale,
            Background = Navy,
            Margin = new Thickness(20 * scale, 0, 20 * scale, 6)
        });
        foreach (var (label, value) in rows)
        {
            var line = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 1.5, 0, 1.5)
            };
            line.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                FontFamily = Font,
                FontSize = baseFont,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ink,
                FlowDirection = FlowDirection.RightToLeft,
                TextAlignment = TextAlignment.Right,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 250 * scale
            });
            line.Children.Add(new TextBlock
            {
                Text = $"{label}:",
                FontFamily = Font,
                FontSize = baseFont,
                FontWeight = FontWeights.SemiBold,
                Foreground = NavyText,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Thickness(8, 0, 0, 0)
            });
            stack.Children.Add(line);
        }

        return new Border
        {
            CornerRadius = new CornerRadius(12 * scale),
            BorderBrush = Navy,
            BorderThickness = new Thickness(1.4),
            Background = CardFill,
            Padding = new Thickness(12 * scale, 8 * scale, 12 * scale, 8 * scale),
            Child = stack
        };
    }

    private static UIElement BuildTotalPills(ProductAnalyticsPrintModel model, double contentWidth, double scale, double baseFont)
    {
        var grid = new Grid { Width = contentWidth, FlowDirection = FlowDirection.RightToLeft };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10 * scale) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var sales = AmountPill("صافي المبيعات", Money(model.Result.NetSales), scale, baseFont, filled: true);
        var profit = AmountPill($"مجمل الربح  ·  الهامش {model.Result.MarginPercent:N1}%", Money(model.Result.GrossProfit), scale, baseFont, filled: false);
        Grid.SetColumn(sales, 0);
        Grid.SetColumn(profit, 2);
        grid.Children.Add(sales);
        grid.Children.Add(profit);
        return grid;
    }

    private static Border AmountPill(string label, string value, double scale, double baseFont, bool filled)
    {
        var text = new TextBlock
        {
            FontFamily = Font,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FlowDirection = FlowDirection.RightToLeft,
            Foreground = NavyText
        };
        text.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.SemiBold, FontSize = baseFont });
        text.Inlines.Add(new Run(value) { FontWeight = FontWeights.Bold, FontSize = baseFont + 1.5 });
        return new Border
        {
            CornerRadius = new CornerRadius(12 * scale),
            Background = filled ? TotalFill : Brushes.White,
            BorderBrush = Navy,
            BorderThickness = new Thickness(filled ? 1.3 : 1.6),
            Padding = new Thickness(12 * scale, 8 * scale, 12 * scale, 8 * scale),
            Child = text
        };
    }

    private static Table BuildCategoryTable(ProductAnalyticsPrintModel model, double contentWidth, double font)
    {
        var titles = new[] { "التصنيف", "المنتجات", "الكمية", "المبيعات", "المشتريات", "المرتجعات", "الربح", "الهامش", "المخزون", "راكد" };
        var weights = new[] { 150d, 70d, 70d, 90d, 90d, 80d, 80d, 60d, 90d, 50d };
        var rows = model.Result.Categories.Select(c => new[]
        {
            c.Name,
            c.ProductCount.ToString("N0"),
            Qty(c.NetQuantity),
            Money(c.SalesAmount),
            Money(c.PurchaseValue),
            Money(c.ReturnValue),
            Money(c.GrossProfit),
            $"{c.MarginPercent:N1}%",
            Money(c.StockValue),
            c.StagnantCount.ToString("N0")
        }).ToList();
        if (rows.Count == 0)
            rows.Add(["لا توجد تصنيفات", "", "", "", "", "", "", "", "", ""]);
        return BuildTable(titles, weights, rows, contentWidth, font, nameColumn: 0);
    }

    private static Table BuildInsightTable(ProductAnalyticsInsightGroup insight, double contentWidth, double font)
    {
        var rows = insight.Lines.Select(line =>
        {
            var parts = line.Split(" — ", 2, StringSplitOptions.TrimEntries);
            return new[] { parts[0], parts.Length > 1 ? parts[1] : string.Empty };
        }).ToList();
        if (rows.Count == 0)
            rows.Add(["لا توجد بيانات كافية.", ""]);
        var table = BuildTable([insight.Title, "القيمة"], [3, 1], rows, contentWidth, font, nameColumn: 0);
        return table;
    }

    private static Table BuildTable(string[] titles, double[] weights, IReadOnlyList<string[]> rows, double contentWidth, double font, int nameColumn = 1)
    {
        var sum = weights.Sum();
        var padding = new Thickness(3, 2, 3, 2);
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = Navy,
            BorderThickness = new Thickness(LineWidth),
            FlowDirection = FlowDirection.RightToLeft
        };
        foreach (var weight in weights)
            table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * weight / sum) });

        var header = new TableRowGroup();
        var headerRow = new TableRow();
        for (var i = 0; i < titles.Length; i++)
        {
            headerRow.Cells.Add(Cell(titles[i], padding, font, bold: true, background: HeaderFill, foreground: NavyText, border: HeaderLine, isLast: i == titles.Length - 1));
        }
        header.Rows.Add(headerRow);
        table.RowGroups.Add(header);

        var data = new TableRowGroup();
        for (var r = 0; r < rows.Count; r++)
        {
            var background = r % 2 == 1 ? AltRow : Brushes.White;
            var row = new TableRow();
            var values = rows[r];
            for (var c = 0; c < titles.Length; c++)
            {
                var text = c < values.Length ? values[c] : "";
                row.Cells.Add(Cell(text, padding, font, background: background, isLast: c == titles.Length - 1, align: c == nameColumn ? TextAlignment.Right : TextAlignment.Center));
            }
            data.Rows.Add(row);
        }
        table.RowGroups.Add(data);
        return table;
    }

    private static UIElement BuildNote(string notes, double contentWidth, double scale, double baseFont) =>
        new Border
        {
            Width = contentWidth,
            Background = NoteFill,
            BorderBrush = NoteBorder,
            BorderThickness = new Thickness(1.3),
            CornerRadius = new CornerRadius(10 * scale),
            Padding = new Thickness(12 * scale, 8 * scale, 12 * scale, 8 * scale),
            Child = new TextBlock
            {
                FontFamily = Font,
                FontSize = baseFont,
                Foreground = Ink,
                TextWrapping = TextWrapping.Wrap,
                FlowDirection = FlowDirection.RightToLeft,
                Text = notes
            }
        };

    private static UIElement BuildSignatures(double contentWidth, double scale, double baseFont)
    {
        var grid = new Grid { Width = contentWidth, FlowDirection = FlowDirection.RightToLeft };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12 * scale) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var prepared = SignatureBox("إعداد", scale, baseFont);
        var approved = SignatureBox("اعتماد", scale, baseFont);
        Grid.SetColumn(prepared, 0);
        Grid.SetColumn(approved, 2);
        grid.Children.Add(prepared);
        grid.Children.Add(approved);
        return grid;
    }

    private static Grid SignatureBox(string label, double scale, double baseFont)
    {
        var box = new Grid { Height = 78 * scale, FlowDirection = FlowDirection.RightToLeft };
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
            FontSize = baseFont + 1,
            FontWeight = FontWeights.SemiBold,
            Foreground = NavyText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 8 * scale, 0, 0)
        });
        return box;
    }

    private static Paragraph SectionTitle(string text, double baseFont, double scale) => new(new Run(text))
    {
        FontFamily = Font,
        FontSize = baseFont + 2,
        FontWeight = FontWeights.Bold,
        Foreground = NavyText,
        Margin = new Thickness(0, 8 * scale, 0, 4 * scale)
    };

    private static TableCell Cell(
        string text,
        Thickness padding,
        double fontSize,
        bool bold = false,
        TextAlignment align = TextAlignment.Center,
        Brush? background = null,
        Brush? foreground = null,
        Brush? border = null,
        bool isLast = false) =>
        new(new Paragraph(new Run(text ?? string.Empty))
        {
            Margin = new Thickness(0),
            FontFamily = Font,
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = foreground ?? Ink,
            TextAlignment = align,
            FlowDirection = FlowDirection.RightToLeft
        })
        {
            Padding = padding,
            BorderBrush = border ?? Line,
            BorderThickness = isLast
                ? new Thickness(LineWidth, 0, 0, LineWidth)
                : new Thickness(0, 0, LineWidth, LineWidth),
            Background = background
        };

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

    private static string Money(decimal value) => value.ToString("N0");

    private static string Qty(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("N0") : value.ToString("N2");

    private static SolidColorBrush Brush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}

public sealed class ProductAnalyticsPrintModel
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string WarehouseName { get; init; } = "كل المستودعات";
    public string CategoryName { get; init; } = "كل التصنيفات";
    public string SupplierName { get; init; } = "كل الموردين";
    public string ActivityName { get; init; } = "كل المنتجات";
    public string SearchText { get; init; } = string.Empty;
    public string CurrencyNote { get; init; } = string.Empty;
    public string ProfitNote { get; init; } = string.Empty;
    public string? StockAgeNote { get; init; }
    public ProductAnalyticsReportResult Result { get; init; } = new();
}
