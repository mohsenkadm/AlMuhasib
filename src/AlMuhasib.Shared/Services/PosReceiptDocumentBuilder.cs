using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

/// <summary>Builds POS receipt FlowDocuments for A4 and thermal paper widths.</summary>
public static class PosReceiptDocumentBuilder
{
    private static readonly FontFamily ArabicFont = new("Segoe UI, Tahoma, Arial");
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
    private static readonly Brush LineBrush = new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90));

    public static FlowDocument Build(InvoicePrintModel model, string? paperSize)
    {
        var sizeKey = PosReceiptPaperSizes.Normalize(paperSize);
        var pageSize = PosReceiptPaperSizes.GetPageSize(sizeKey);
        return PosReceiptPaperSizes.IsThermal(sizeKey)
            ? BuildThermal(model, pageSize, sizeKey)
            : BuildA4(model, pageSize);
    }

    private static FlowDocument BuildA4(InvoicePrintModel model, Size pageSize)
    {
        var doc = new FlowDocument
        {
            FontFamily = ArabicFont,
            FontSize = 13,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(40, 36, 40, 36),
            PageWidth = pageSize.Width,
            PageHeight = pageSize.Height,
            ColumnWidth = pageSize.Width
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        var title = model.IsGoldInvoice
            ? (string.IsNullOrWhiteSpace(model.Title) ? "فاتورة ذهب" : model.Title)
            : "إيصال بيع سريع (POS)";
        doc.Blocks.Add(CenteredParagraph(title, 20, FontWeights.Bold));
        doc.Blocks.Add(MetaParagraph($"رقم الفاتورة: {model.InvoiceNumber}"));
        doc.Blocks.Add(MetaParagraph($"التاريخ: {model.Date:yyyy/MM/dd HH:mm}"));
        doc.Blocks.Add(MetaParagraph($"{model.PartyLabel}: {model.PartyName}"));
        if (!string.IsNullOrWhiteSpace(model.WarehouseName))
            doc.Blocks.Add(MetaParagraph($"المخزن: {model.WarehouseName}"));
        if (model.IsGoldInvoice && !string.IsNullOrWhiteSpace(model.PaymentMethod))
            doc.Blocks.Add(MetaParagraph($"الدفع: {model.PaymentMethod}"));
        if (model.IsGoldInvoice && model.FxRate > 0)
            doc.Blocks.Add(MetaParagraph($"سعر الصرف: {model.FxRate:N2}"));
        doc.Blocks.Add(Spacer(10));

        if (model.IsGoldInvoice)
            AppendGoldA4Items(doc, model);
        else
            AppendStandardA4Items(doc, model);

        doc.Blocks.Add(Spacer(6));

        if (model.IsGoldInvoice)
            AppendGoldTotals(doc, model, compact: false);
        else
        {
            var subtotal = model.Subtotal > 0 ? model.Subtotal : model.Items.Sum(x => x.TotalPrice);
            doc.Blocks.Add(RightAligned($"المجموع الفرعي: {FormatMoney(subtotal)} د.ع", 13));
            doc.Blocks.Add(RightAligned($"الإجمالي: {FormatMoney(model.GrandTotal)} د.ع", 18, FontWeights.Bold));
        }

        if (!string.IsNullOrWhiteSpace(model.Notes))
            doc.Blocks.Add(MetaParagraph($"ملاحظات: {model.Notes}"));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc,
            systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

        return doc;
    }

    private static void AppendStandardA4Items(FlowDocument doc, InvoicePrintModel model)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 12) };
        table.Columns.Add(new TableColumn { Width = new GridLength(40) });
        table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        group.Rows.Add(HeaderRow("#", "الصنف", "الكمية", "السعر", "المبلغ"));
        var i = 1;
        foreach (var item in model.Items)
        {
            var name = item.ItemName;
            if (!string.IsNullOrWhiteSpace(item.WarehouseName))
                name = $"{name}\nالمخزن: {item.WarehouseName}";
            if (model.PharmacyUsageReceipt && !string.IsNullOrWhiteSpace(item.UsageInstructions))
                name = $"{name}\nطريقة الاستخدام: {item.UsageInstructions.Trim()}";

            var priceCol = FormatMoney(item.UnitPrice);
            if (model.ShowLineDiscount && item.DiscountPercent > 0)
                priceCol = $"{priceCol}\nخصم {item.DiscountPercent:0.##}%";

            group.Rows.Add(BodyRow(
                i.ToString(CultureInfo.InvariantCulture),
                name,
                FormatQty(item.Quantity),
                priceCol,
                FormatMoney(item.TotalPrice)));
            i++;
        }

        doc.Blocks.Add(table);
    }

    private static void AppendGoldA4Items(FlowDocument doc, InvoicePrintModel model)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 12) };
        foreach (var _ in Enumerable.Range(0, 7))
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);

        group.Rows.Add(HeaderRow("#", "الصنف", "عيار", "وزن", "أجور", "ذهب", "إجمالي"));
        foreach (var item in model.Items)
        {
            var name = item.ItemName;
            if (!string.IsNullOrWhiteSpace(item.LineDirectionLabel))
                name = $"{name} ({item.LineDirectionLabel})";
            group.Rows.Add(BodyRow(
                item.Number.ToString(CultureInfo.InvariantCulture),
                name,
                (item.KaratValue ?? 0).ToString(CultureInfo.InvariantCulture),
                FormatWeight(item.WeightGrams ?? item.Quantity),
                FormatMoney(item.MakingCharge ?? 0),
                FormatMoney(item.GoldValue ?? 0),
                FormatMoney(item.TotalPrice)));
        }

        doc.Blocks.Add(table);
    }

    private static FlowDocument BuildThermal(InvoicePrintModel model, Size pageSize, string sizeKey)
    {
        var fontSize = sizeKey switch
        {
            PosReceiptPaperSizes.Mm50 => 9.5,
            PosReceiptPaperSizes.Mm58 => 10.5,
            _ => 11.5
        };
        var pad = sizeKey == PosReceiptPaperSizes.Mm50 ? 6.0 : 8.0;

        var doc = new FlowDocument
        {
            FontFamily = ArabicFont,
            FontSize = fontSize,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(pad, pad, pad, pad),
            PageWidth = pageSize.Width,
            PageHeight = pageSize.Height,
            ColumnWidth = pageSize.Width
        };

        var branding = PrintBrandingProvider.Current;
        if (branding?.HasThermalLogo == true)
        {
            var maxLogoHeight = sizeKey switch
            {
                PosReceiptPaperSizes.Mm50 => 48.0,
                PosReceiptPaperSizes.Mm58 => 56.0,
                _ => 72.0
            };
            var logoBlock = CreateThermalLogoBlock(branding.ThermalLogoData!, pageSize.Width - (pad * 2), maxLogoHeight);
            if (logoBlock is not null)
                doc.Blocks.Add(logoBlock);
        }

        var company = branding?.CompanyName;
        if (!string.IsNullOrWhiteSpace(company) && (branding?.ShowHeaderText ?? true))
            doc.Blocks.Add(CenteredParagraph(company!, fontSize + 2, FontWeights.Bold));

        var title = model.IsGoldInvoice
            ? (string.IsNullOrWhiteSpace(model.Title) ? "فاتورة ذهب" : model.Title)
            : "إيصال بيع";
        doc.Blocks.Add(CenteredParagraph(title, fontSize + 1, FontWeights.SemiBold));
        doc.Blocks.Add(DashedLine());
        if (!model.IsGoldInvoice && !string.IsNullOrWhiteSpace(model.CashierName))
            doc.Blocks.Add(MetaParagraph($"الكاشير: {model.CashierName}", fontSize - 0.5, FontWeights.SemiBold));
        doc.Blocks.Add(MetaParagraph($"#{model.InvoiceNumber}", fontSize, FontWeights.SemiBold));
        doc.Blocks.Add(MetaParagraph($"{model.Date:yyyy/MM/dd HH:mm}", fontSize - 0.5));
        doc.Blocks.Add(MetaParagraph($"{model.PartyLabel}: {model.PartyName}", fontSize - 0.5));
        if (!string.IsNullOrWhiteSpace(model.SalesRepresentativeName))
            doc.Blocks.Add(MetaParagraph($"المندوب: {model.SalesRepresentativeName}", fontSize - 0.5));
        if (!string.IsNullOrWhiteSpace(model.SalesRepresentativePhone))
            doc.Blocks.Add(MetaParagraph($"هاتف المندوب: {model.SalesRepresentativePhone}", fontSize - 0.5));
        if (model.IsGoldInvoice && !string.IsNullOrWhiteSpace(model.PaymentMethod))
            doc.Blocks.Add(MetaParagraph($"الدفع: {model.PaymentMethod}", fontSize - 0.5));
        if (model.IsGoldInvoice && model.FxRate > 0)
            doc.Blocks.Add(MetaParagraph($"صرف: {model.FxRate:N2}", fontSize - 0.5));
        doc.Blocks.Add(DashedLine());

        if (model.IsGoldInvoice)
        {
            for (var i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                var name = item.ItemName;
                if (!string.IsNullOrWhiteSpace(item.LineDirectionLabel))
                    name = $"{name} ({item.LineDirectionLabel})";

                doc.Blocks.Add(new Paragraph(new Run(name))
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = fontSize
                });

                var karat = item.KaratValue ?? 0;
                var weight = item.WeightGrams ?? item.Quantity;
                var making = item.MakingCharge ?? 0;
                doc.Blocks.Add(new Paragraph(new Run(
                    $"عيار {karat} | وزن {FormatWeight(weight)} غ | أجور {FormatMoney(making)}"))
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    FontSize = fontSize - 0.5,
                    Foreground = MutedBrush
                });
                doc.Blocks.Add(new Paragraph(new Run(
                    $"ذهب {FormatMoney(item.GoldValue ?? 0)} = {FormatMoney(item.TotalPrice)}"))
                {
                    Margin = new Thickness(0, 0, 0, 2),
                    FontSize = fontSize - 0.5,
                    Foreground = MutedBrush
                });

                if (i < model.Items.Count - 1)
                    doc.Blocks.Add(DashedLine());
            }
        }
        else
        {
            if (model.PharmacyUsageReceipt)
                doc.Blocks.Add(CenteredParagraph("وصفة / طريقة الاستخدام", fontSize, FontWeights.SemiBold));

            AppendThermalItemLines(doc, model, fontSize);

            if (model.PharmacyUsageReceipt)
            {
                foreach (var item in model.Items)
                {
                    var usage = string.IsNullOrWhiteSpace(item.UsageInstructions)
                        ? "—"
                        : item.UsageInstructions.Trim();
                    doc.Blocks.Add(new Paragraph(new Run($"{item.ItemName}: {usage}"))
                    {
                        Margin = new Thickness(0, 1, 0, 1),
                        FontSize = fontSize - 0.5,
                        Foreground = MutedBrush
                    });
                }
            }
        }

        doc.Blocks.Add(DashedLine());
        if (model.IsGoldInvoice)
            AppendGoldTotals(doc, model, compact: true, fontSize: fontSize);
        else
        {
            var subtotal = model.Subtotal > 0 ? model.Subtotal : model.Items.Sum(x => x.TotalPrice);
            if (model.DiscountAmount > 0)
            {
                doc.Blocks.Add(CenteredParagraph($"المجموع: {FormatMoney(subtotal)} د.ع", fontSize, FontWeights.Normal));
                doc.Blocks.Add(CenteredParagraph($"الخصم: {FormatMoney(model.DiscountAmount)} د.ع", fontSize, FontWeights.SemiBold));
            }
            doc.Blocks.Add(CenteredParagraph($"الإجمالي: {FormatMoney(model.GrandTotal)} د.ع", fontSize + 2, FontWeights.Bold));
        }
        doc.Blocks.Add(DashedLine());

        AppendThermalNotes(doc, branding?.ThermalNotes, fontSize);

        var footer = branding?.FooterText;
        if (!string.IsNullOrWhiteSpace(footer) && (branding?.ShowFooterText ?? true))
            doc.Blocks.Add(CenteredParagraph(footer!, fontSize - 1));

        AppendThermalEditionLabel(doc, fontSize);

        return doc;
    }

    private static void AppendThermalEditionLabel(FlowDocument doc, double fontSize)
    {
        var label = ProductEditionProvider.ThermalSupportLabel;
        if (string.IsNullOrWhiteSpace(label))
            return;

        doc.Blocks.Add(new Paragraph(new Run(label)
        {
            FontSize = Math.Max(6.5, fontSize - 3.5),
            FontWeight = FontWeights.Normal,
            Foreground = MutedBrush
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        });
    }

    private static void AppendGoldTotals(FlowDocument doc, InvoicePrintModel model, bool compact, double fontSize = 12)
    {
        void Line(string text, bool bold = false, double? size = null) =>
            doc.Blocks.Add(compact
                ? CenteredParagraph(text, size ?? fontSize, bold ? FontWeights.SemiBold : FontWeights.Normal)
                : RightAligned(text, size ?? (bold ? 16 : 13), bold ? FontWeights.Bold : FontWeights.Normal));

        Line($"ذهب: {FormatMoney(model.TotalGoldValue)}");
        Line($"أجور: {FormatMoney(model.TotalMakingCharge)}");
        if (model.DiscountAmount != 0)
            Line($"خصم: {FormatMoney(model.DiscountAmount)}");
        if (model.TotalAmountIqd != 0)
            Line($"IQD: {FormatMoney(model.TotalAmountIqd)}");
        if (model.TotalAmountUsd != 0)
            Line($"USD: {FormatMoney(model.TotalAmountUsd)}");
        Line($"الإجمالي: {FormatMoney(model.GrandTotal)}", bold: true, size: compact ? fontSize + 2 : 18);
        Line($"مدفوع: {FormatMoney(model.PaidAmount)} | متبقي: {FormatMoney(model.RemainingAmount)}",
            size: compact ? fontSize - 0.5 : 12);
    }

    private static void AppendThermalItemLines(FlowDocument doc, InvoicePrintModel model, double fontSize)
    {
        for (var i = 0; i < model.Items.Count; i++)
        {
            var item = model.Items[i];
            doc.Blocks.Add(new Paragraph(new Run(item.ItemName))
            {
                Margin = new Thickness(0, 3, 0, 0),
                FontWeight = FontWeights.SemiBold,
                FontSize = fontSize
            });

            if (!string.IsNullOrWhiteSpace(item.WarehouseName))
            {
                doc.Blocks.Add(new Paragraph(new Run(item.WarehouseName))
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    FontSize = fontSize - 0.5,
                    Foreground = MutedBrush
                });
            }

            doc.Blocks.Add(new Paragraph(new Run(
                $"{FormatQty(item.Quantity)} × {FormatMoney(item.UnitPrice)} = {FormatMoney(item.TotalPrice)}"))
            {
                Margin = new Thickness(0, 0, 0, 2),
                FontSize = fontSize - 0.5,
                Foreground = MutedBrush
            });

            if (i < model.Items.Count - 1)
                doc.Blocks.Add(DashedLine());
        }
    }

    private static void AppendThermalNotes(FlowDocument doc, string? notes, double fontSize)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return;

        foreach (var line in notes.Replace("\r\n", "\n", StringComparison.Ordinal)
                     .Split('\n', StringSplitOptions.None))
        {
            var text = line.TrimEnd();
            if (text.Length == 0)
            {
                doc.Blocks.Add(Spacer(4));
                continue;
            }

            doc.Blocks.Add(CenteredParagraph(text, fontSize - 0.5));
        }
    }

    private static Block? CreateThermalLogoBlock(byte[] data, double maxWidth, double maxHeight)
    {
        try
        {
            var bmp = new BitmapImage();
            using var ms = new MemoryStream(data);
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();

            var aspect = bmp.PixelHeight / (double)Math.Max(1, bmp.PixelWidth);
            var width = Math.Min(maxWidth, Math.Max(40, maxWidth * 0.85));
            var height = Math.Min(maxHeight, Math.Max(28, width * aspect));

            var image = new Image
            {
                Source = bmp,
                Width = width,
                Height = height,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                SnapsToDevicePixels = true
            };

            return new BlockUIContainer(image)
            {
                Margin = new Thickness(0, 0, 0, 6),
                TextAlignment = TextAlignment.Center
            };
        }
        catch
        {
            return null;
        }
    }

    private static TableRow HeaderRow(params string[] cells) => HeaderRow(12, cells);

    private static TableRow HeaderRow(double fontSize, params string[] cells)
    {
        var row = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)) };
        foreach (var c in cells)
            row.Cells.Add(Cell(c, FontWeights.Bold, fontSize));
        return row;
    }

    private static TableRow BodyRow(params string[] cells) => BodyRow(12, cells);

    private static TableRow BodyRow(double fontSize, params string[] cells)
    {
        var row = new TableRow();
        foreach (var c in cells)
            row.Cells.Add(Cell(c, FontWeights.Normal, fontSize));
        return row;
    }

    private static TableCell Cell(string text, FontWeight weight, double size) =>
        new(new Paragraph(new Run(text) { FontWeight = weight, FontSize = size })
        {
            Margin = new Thickness(2, 2, 2, 2)
        })
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x90, 0xA4, 0xAE)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(1)
        };

    private static Paragraph CenteredParagraph(string text, double size, FontWeight? weight = null) =>
        new(new Run(text) { FontSize = size, FontWeight = weight ?? FontWeights.Normal })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 1, 0, 1)
        };

    private static Paragraph MetaParagraph(string text, double size = 12, FontWeight? weight = null) =>
        new(new Run(text) { FontSize = size, FontWeight = weight ?? FontWeights.Normal })
        {
            Margin = new Thickness(0, 1, 0, 1)
        };

    private static Paragraph RightAligned(string text, double size, FontWeight? weight = null) =>
        new(new Run(text) { FontSize = size, FontWeight = weight ?? FontWeights.Normal })
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 2, 0, 2)
        };

    private static Paragraph Spacer(double height) =>
        new() { Margin = new Thickness(0, height, 0, 0) };

    private static Paragraph DashedLine() =>
        new(new Run("--------------------------------") { FontSize = 10, Foreground = LineBrush })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3)
        };

    private static string FormatMoney(decimal value) => value.ToString("N0", CultureInfo.CurrentCulture);
    private static string FormatQty(decimal value) => value.ToString("N0", CultureInfo.CurrentCulture);
    private static string FormatWeight(decimal value) => value.ToString("N3", CultureInfo.CurrentCulture);
}
