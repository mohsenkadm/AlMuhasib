using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

    public void PrintLabels(IEnumerable<BarcodeLabelItem> items) =>
        PrintLabels(items, BarcodeLabelSize.Mm38x25);

    public void PrintLabels(IEnumerable<BarcodeLabelItem> items, BarcodeLabelSize size)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i.Barcode)).ToList();
        if (list.Count == 0) return;

        var layout = BarcodeLabelSizeHelper.GetLayout(size);
        var doc = new FlowDocument
        {
            PageWidth = layout.PageWidth,
            PageHeight = layout.PageHeight,
            PagePadding = new Thickness(layout.Padding),
            FontFamily = new FontFamily("Segoe UI"),
            FlowDirection = FlowDirection.RightToLeft,
            ColumnWidth = layout.PageWidth
        };

        foreach (var item in list)
        {
            var section = new Section
            {
                Margin = new Thickness(0, 0, 0, layout.SectionBottomMargin),
                BreakPageBefore = doc.Blocks.Count > 0
            };

            var companyName = ResolveCompanyName(item);
            if (!string.IsNullOrWhiteSpace(companyName))
            {
                var companyLine = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 1),
                    TextAlignment = TextAlignment.Center
                };
                companyLine.Inlines.Add(new Run(Truncate(companyName.Trim(), size == BarcodeLabelSize.Mm30x15 ? 22 : 28))
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = layout.CompanyFontSize
                });
                section.Blocks.Add(companyLine);
            }

            if (!string.IsNullOrWhiteSpace(item.ProductName))
            {
                var productLine = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 1),
                    TextAlignment = TextAlignment.Center
                };
                productLine.Inlines.Add(new Run(Truncate(item.ProductName.Trim(), size == BarcodeLabelSize.Mm30x15 ? 24 : 32))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = layout.ProductFontSize
                });
                section.Blocks.Add(productLine);
            }

            var barcodePxW = (int)Math.Ceiling(layout.BarcodeWidth);
            var barcodePxH = (int)Math.Ceiling(layout.BarcodeHeight);
            var barcodeImage = CreateBarcodeBitmap(item.Barcode, barcodePxW, barcodePxH);
            if (barcodeImage is not null)
            {
                section.Blocks.Add(new BlockUIContainer(new Image
                {
                    Source = barcodeImage,
                    Width = layout.BarcodeWidth,
                    Height = layout.BarcodeHeight,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 1, 0, 1)
                }));
            }

            var codeLine = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 1),
                TextAlignment = TextAlignment.Center
            };
            codeLine.Inlines.Add(new Run(item.Barcode)
            {
                FontSize = layout.CodeFontSize,
                FontFamily = new FontFamily("Consolas")
            });
            section.Blocks.Add(codeLine);

            if (item.KaratValue is > 0 || item.WeightGrams is > 0)
            {
                var goldMeta = new Paragraph
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var parts = new List<string>();
                if (item.KaratValue is > 0)
                    parts.Add($"عيار {item.KaratValue}");
                if (item.WeightGrams is > 0)
                    parts.Add($"وزن {item.WeightGrams.Value:0.###} غ");
                goldMeta.Inlines.Add(new Run(string.Join(" — ", parts))
                {
                    FontSize = layout.GoldFontSize,
                    FontWeight = FontWeights.SemiBold
                });
                section.Blocks.Add(goldMeta);
            }

            if (item.Price is > 0)
            {
                var priceLine = new Paragraph
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                priceLine.Inlines.Add(new Run($"{item.Price:N0} د.ع")
                {
                    FontSize = layout.PriceFontSize,
                    FontWeight = FontWeights.SemiBold
                });
                section.Blocks.Add(priceLine);
            }

            doc.Blocks.Add(section);
        }

        var pd = new PrintDialog();
        if (pd.ShowDialog() != true) return;
        pd.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
            layout.PageWidth, layout.PageHeight);
        pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "ملصقات باركود");
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
                    Margin = 1,
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
