using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using ZXing;
using ZXing.Common;

namespace AlMuhasib.Shared.Services;

public class BarcodeLabelService : IBarcodeLabelService
{
    public byte[]? CreateBarcodePng(string barcode, int width = 280, int height = 90)
    {
        var bitmap = CreateBarcodeBitmap(barcode, width, height);
        if (bitmap is null) return null;

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    public byte[]? CreateLabelPreviewPng(BarcodeLabelItem item, BarcodeLabelSize size, double scale = 2.0) =>
        CreateLabelPreviewPng(item, size, null, null, scale);

    public byte[]? CreateLabelPreviewPng(
        BarcodeLabelItem item,
        BarcodeLabelSize size,
        double? customWidthMm,
        double? customHeightMm,
        double scale = 2.0)
    {
        if (string.IsNullOrWhiteSpace(item.Barcode))
            return null;

        var layout = BarcodeLabelSizeHelper.GetLayout(size, customWidthMm, customHeightMm);
        var visual = BuildLabelElement(item, layout);
        var pixelScale = Math.Clamp(scale, 1.0, 4.0);
        var dpi = 96.0 * pixelScale;
        var pxW = Math.Max(1, (int)Math.Ceiling(layout.PageWidth * pixelScale));
        var pxH = Math.Max(1, (int)Math.Ceiling(layout.PageHeight * pixelScale));

        visual.Measure(new Size(layout.PageWidth, layout.PageHeight));
        visual.Arrange(new Rect(0, 0, layout.PageWidth, layout.PageHeight));
        visual.UpdateLayout();

        var rtb = new RenderTargetBitmap(pxW, pxH, dpi, dpi, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    public void PrintLabels(IEnumerable<BarcodeLabelItem> items) =>
        PrintLabels(items, BarcodeLabelSizeHelper.DefaultSize, null, null, BarcodeLabelSizeHelper.DefaultGapMm);

    public void PrintLabels(IEnumerable<BarcodeLabelItem> items, BarcodeLabelSize size) =>
        PrintLabels(items, size, null, null, BarcodeLabelSizeHelper.DefaultGapMm);

    public void PrintLabels(
        IEnumerable<BarcodeLabelItem> items,
        BarcodeLabelSize size,
        double? customWidthMm,
        double? customHeightMm,
        double gapMm = 0)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i.Barcode)).ToList();
        if (list.Count == 0) return;

        var layout = BarcodeLabelSizeHelper.GetLayout(size, customWidthMm, customHeightMm);
        var gapDip = BarcodeLabelSizeHelper.MmToDip(Math.Max(0, gapMm));
        var pageWidth = layout.PageWidth;
        var pageHeight = layout.PageHeight + gapDip;
        var pageSize = new Size(pageWidth, pageHeight);

        var fixedDoc = new FixedDocument();
        fixedDoc.DocumentPaginator.PageSize = pageSize;

        foreach (var item in list)
        {
            var page = new FixedPage
            {
                Width = pageWidth,
                Height = pageHeight,
                Background = Brushes.White,
                FlowDirection = FlowDirection.RightToLeft
            };

            var label = BuildLabelElement(item, layout);
            FixedPage.SetLeft(label, 0);
            FixedPage.SetTop(label, 0);
            page.Children.Add(label);

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            fixedDoc.Pages.Add(pageContent);
        }

        var pd = new PrintDialog();
        if (pd.ShowDialog() != true) return;

        pd.PrintTicket.PageMediaSize = new PageMediaSize(pageWidth, pageHeight);
        pd.PrintDocument(fixedDoc.DocumentPaginator, "ملصقات باركود");
    }

    private static FrameworkElement BuildLabelElement(
        BarcodeLabelItem item,
        BarcodeLabelLayoutMetrics layout)
    {
        var root = new Grid
        {
            Width = layout.PageWidth,
            Height = layout.PageHeight,
            Background = Brushes.White,
            ClipToBounds = true,
            FlowDirection = FlowDirection.RightToLeft,
            SnapsToDevicePixels = true
        };

        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(layout.Padding)
        };

        if (item.ShowCompanyName)
        {
            var companyName = ResolveCompanyName(item);
            if (!string.IsNullOrWhiteSpace(companyName) && layout.CompanyFontSize > 0)
            {
                content.Children.Add(new TextBlock
                {
                    Text = Truncate(companyName.Trim(), 28),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = layout.CompanyFontSize,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 1),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }
        }

        if (item.ShowBranchName && !string.IsNullOrWhiteSpace(item.BranchName) && layout.CompanyFontSize > 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = Truncate(item.BranchName.Trim(), 28),
                FontWeight = FontWeights.SemiBold,
                FontSize = layout.CompanyFontSize,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 1),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        if (item.ShowProductName && !string.IsNullOrWhiteSpace(item.ProductName) && layout.ProductFontSize > 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = Truncate(item.ProductName.Trim(), 32),
                FontWeight = FontWeights.Bold,
                FontSize = layout.ProductFontSize,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 1),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap
            });
        }

        var barcodePxW = (int)Math.Ceiling(layout.BarcodeWidth * 2);
        var barcodePxH = (int)Math.Ceiling(layout.BarcodeHeight * 2);
        var barcodeImage = CreateBarcodeBitmap(item.Barcode, barcodePxW, barcodePxH);
        if (barcodeImage is not null)
        {
            content.Children.Add(new Image
            {
                Source = barcodeImage,
                Width = layout.BarcodeWidth,
                Height = layout.BarcodeHeight,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Center,
                SnapsToDevicePixels = true,
                Margin = new Thickness(0, 1, 0, 1)
            });
        }

        if (item.ShowBarcodeNumber && layout.CodeFontSize > 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = item.Barcode,
                FontSize = layout.CodeFontSize,
                FontFamily = new FontFamily("Consolas"),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 1)
            });
        }

        if (item.KaratValue is > 0 || item.WeightGrams is > 0)
        {
            var parts = new List<string>();
            if (item.KaratValue is > 0)
                parts.Add($"عيار {item.KaratValue}");
            if (item.WeightGrams is > 0)
                parts.Add($"وزن {item.WeightGrams.Value:0.###} غ");

            content.Children.Add(new TextBlock
            {
                Text = string.Join(" — ", parts),
                FontSize = layout.GoldFontSize,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 1)
            });
        }

        if (item.ShowPrice && item.Price is > 0 && layout.PriceFontSize > 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = $"{item.Price:N0} د.ع",
                FontSize = layout.PriceFontSize,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            });
        }

        root.Children.Add(content);
        return root;
    }

    private static string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
            return value;
        return value[..Math.Max(0, maxChars - 1)] + "…";
    }

    private static string ResolveCompanyName(BarcodeLabelItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CompanyName))
            return item.CompanyName;

        return PrintBrandingProvider.Current.CompanyName ?? string.Empty;
    }

    private static BitmapSource? CreateBarcodeBitmap(string barcode, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = Math.Max(width, 80),
                    Height = Math.Max(height, 20),
                    Margin = 2,
                    PureBarcode = true
                }
            };

            var pixelData = writer.Write(barcode.Trim());
            var bitmap = new WriteableBitmap(
                pixelData.Width,
                pixelData.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null);
            bitmap.WritePixels(
                new Int32Rect(0, 0, pixelData.Width, pixelData.Height),
                pixelData.Pixels,
                pixelData.Width * 4,
                0);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
