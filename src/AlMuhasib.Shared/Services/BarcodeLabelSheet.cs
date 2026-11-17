using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Shared.Services;

public partial class BarcodeLabelService
{
    public byte[]? CreateSingleLabelPreviewPng(BarcodeLabelItem item, PriceLabelDesign design)
    {
        design.Normalize();
        var label = CreateLabelVisual(item, design);
        return RenderElementPng(label, label.Width, label.Height, 4);
    }

    private static FrameworkElement CreateLabelVisual(BarcodeLabelItem item, PriceLabelDesign design)
    {
        var labelW = BarcodeLabelSizeHelper.MmToDip(design.LabelWidthMm);
        var labelH = BarcodeLabelSizeHelper.MmToDip(design.LabelHeightMm);
        var padL = BarcodeLabelSizeHelper.MmToDip(design.MarginLeftMm);
        var padR = BarcodeLabelSizeHelper.MmToDip(design.MarginRightMm);
        var innerW = Math.Max(16, labelW - padL - padR - 4);
        var barcodeHeight = BarcodeLabelSizeHelper.MmToDip(design.BarcodeHeightMm);
        var ean = design.ShowBarcode
            ? EanBarcode.Normalize(item.Barcode, design.Symbology)
            : new EanBarcode.Result(false, string.Empty, null);
        EanSymbolElement? symbol = null;
        if (ean.IsValid)
            symbol = EanSymbolElement.Create(ean.Code, design.Symbology, innerW, barcodeHeight);

        return BuildSheetLabel(item, design, labelW, labelH, ean, symbol);
    }

    public IReadOnlyList<byte[]> CreateSheetPreviewPngs(BarcodeLabelItem item, PriceLabelDesign design, int copies)
    {
        var pages = BuildSheetPages(item, design, copies);
        return pages.Select(page => RenderElementPng(page.Visual, page.WidthDip, page.HeightDip, 1.35)).ToList();
    }

    public bool PrintSheet(BarcodeLabelItem item, PriceLabelDesign design, int copies)
    {
        var pages = BuildSheetPages(item, design, copies);
        if (pages.Count == 0)
            return false;

        var fixedDoc = new FixedDocument();
        var pageSize = new Size(pages[0].WidthDip, pages[0].HeightDip);
        fixedDoc.DocumentPaginator.PageSize = pageSize;

        foreach (var sheet in pages)
        {
            var page = new FixedPage
            {
                Width = sheet.WidthDip,
                Height = sheet.HeightDip,
                Background = Brushes.White
            };
            FixedPage.SetLeft(sheet.Visual, 0);
            FixedPage.SetTop(sheet.Visual, 0);
            page.Children.Add(sheet.Visual);

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            fixedDoc.Pages.Add(pageContent);
        }

        var owner = Application.Current?.MainWindow;
        owner?.Activate();
        var pd = new PrintDialog();
        if (pd.ShowDialog() != true)
            return false;

        pd.PrintTicket.PageMediaSize = new PageMediaSize(pageSize.Width, pageSize.Height);
        pd.PrintDocument(fixedDoc.DocumentPaginator, "بطاقات الأسعار");
        return true;
    }

    private static List<SheetPage> BuildSheetPages(BarcodeLabelItem item, PriceLabelDesign design, int copies)
    {
        design.Normalize();
        var fit = design.Measure(copies);
        if (fit.PlacedCards == 0 || fit.CardsPerPage == 0)
            return [];

        var (paperW, paperH) = design.PaperSizeMm();
        var pageW = BarcodeLabelSizeHelper.MmToDip(paperW);
        var pageH = BarcodeLabelSizeHelper.MmToDip(paperH);
        var labelW = BarcodeLabelSizeHelper.MmToDip(design.LabelWidthMm);
        var labelH = BarcodeLabelSizeHelper.MmToDip(design.LabelHeightMm);
        var colGap = BarcodeLabelSizeHelper.MmToDip(design.ColumnSpacingMm);
        var rowGap = BarcodeLabelSizeHelper.MmToDip(design.RowSpacingMm);

        var pages = new List<SheetPage>(fit.PageCount);
        for (var pageIndex = 0; pageIndex < fit.PageCount; pageIndex++)
        {
            var canvas = new Canvas
            {
                Width = pageW,
                Height = pageH,
                Background = Brushes.White,
                SnapsToDevicePixels = true
            };

            var start = pageIndex * fit.CardsPerPage;
            var count = Math.Min(fit.CardsPerPage, fit.PlacedCards - start);
            for (var i = 0; i < count; i++)
            {
                var col = i % fit.Columns;
                var row = i / fit.Columns;
                var label = CreateLabelVisual(item, design);
                Canvas.SetLeft(label, col * (labelW + colGap));
                Canvas.SetTop(label, row * (labelH + rowGap));
                canvas.Children.Add(label);
            }

            pages.Add(new SheetPage(canvas, pageW, pageH));
        }

        return pages;
    }

    private static FrameworkElement BuildSheetLabel(
        BarcodeLabelItem item,
        PriceLabelDesign design,
        double labelW,
        double labelH,
        EanBarcode.Result ean,
        EanSymbolElement? symbol)
    {
        var root = new Border
        {
            Width = labelW,
            Height = labelH,
            Background = Brushes.White,
            BorderBrush = design.ShowBorder ? Brushes.Black : Brushes.Transparent,
            BorderThickness = design.ShowBorder ? new Thickness(0.8) : new Thickness(0),
            Padding = new Thickness(
                Math.Max(2, BarcodeLabelSizeHelper.MmToDip(design.MarginLeftMm)),
                Math.Max(1, BarcodeLabelSizeHelper.MmToDip(design.MarginTopMm)),
                Math.Max(2, BarcodeLabelSizeHelper.MmToDip(design.MarginRightMm)),
                Math.Max(1, BarcodeLabelSizeHelper.MmToDip(design.MarginBottomMm))),
            ClipToBounds = true,
            FlowDirection = FlowDirection.RightToLeft,
            SnapsToDevicePixels = true
        };

        var textWidth = Math.Max(8, labelW - root.Padding.Left - root.Padding.Right);
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        if (design.ShowBranchName && !string.IsNullOrWhiteSpace(item.BranchName))
        {
            var branch = new TextBlock
            {
                Text = item.BranchName.Trim(),
                FontSize = Math.Max(7, design.ProductNameFontSize - 2),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = textWidth,
                MaxHeight = labelH * 0.28,
                Margin = new Thickness(0, 0, 0, 1)
            };
            Grid.SetRow(branch, 0);
            grid.Children.Add(branch);
        }

        var content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (design.ShowProductName && !string.IsNullOrWhiteSpace(item.ProductName))
        {
            content.Children.Add(new TextBlock
            {
                Text = item.ProductName.Trim(),
                FontSize = design.ProductNameFontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = textWidth,
                MaxHeight = labelH * 0.36
            });
        }

        if (design.ShowSku && !string.IsNullOrWhiteSpace(item.Sku))
        {
            content.Children.Add(new TextBlock
            {
                Text = item.Sku.Trim(),
                FontSize = Math.Max(8, design.ProductNameFontSize - 2),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0)
            });
        }

        if (design.ShowBarcode)
        {
            if (ean.IsValid && symbol is not null)
            {
                symbol.HorizontalAlignment = HorizontalAlignment.Center;
                symbol.Margin = new Thickness(0, 1, 0, 0);
                content.Children.Add(symbol);
            }
            else
            {
                content.Children.Add(new TextBlock
                {
                    Text = ean.IsValid
                        ? "تعذر إنشاء صورة الباركود."
                        : ean.Error ?? "الباركود غير صالح للنوع المختار.",
                    FontSize = 8,
                    Foreground = Brushes.DarkRed,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = textWidth
                });
            }
        }

        if (design.ShowPrice && item.Price is > 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = $"{item.Price:N0} د.ع",
                FontSize = design.PriceFontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            });
        }

        Grid.SetRow(content, 1);
        grid.Children.Add(content);
        root.Child = grid;
        return root;
    }

    private static byte[] RenderElementPng(FrameworkElement visual, double width, double height, double pixelScale)
    {
        visual.Measure(new Size(width, height));
        visual.Arrange(new Rect(0, 0, width, height));
        visual.UpdateLayout();

        var pxW = Math.Max(1, (int)Math.Ceiling(width * pixelScale));
        var pxH = Math.Max(1, (int)Math.Ceiling(height * pixelScale));
        var dpi = 96.0 * pixelScale;
        var rtb = new RenderTargetBitmap(pxW, pxH, dpi, dpi, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    private sealed record SheetPage(FrameworkElement Visual, double WidthDip, double HeightDip);
}
