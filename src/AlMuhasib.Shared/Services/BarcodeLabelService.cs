using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    public void PrintLabels(IEnumerable<BarcodeLabelItem> items)
    {
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i.Barcode)).ToList();
        if (list.Count == 0) return;

        var doc = new FlowDocument
        {
            PageWidth = 300,
            PagePadding = new Thickness(10),
            FontFamily = new FontFamily("Segoe UI"),
            FlowDirection = FlowDirection.RightToLeft
        };

        foreach (var item in list)
        {
            var section = new Section { Margin = new Thickness(0, 0, 0, 16) };
            var block = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 4),
                TextAlignment = TextAlignment.Center
            };
            block.Inlines.Add(new Run(item.ProductName)
            {
                FontWeight = FontWeights.Bold,
                FontSize = 13
            });
            section.Blocks.Add(block);

            var barcodeImage = CreateBarcodeBitmap(item.Barcode, 260, 80);
            if (barcodeImage is not null)
            {
                section.Blocks.Add(new BlockUIContainer(new Image
                {
                    Source = barcodeImage,
                    Width = 260,
                    Height = 80,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 2)
                }));
            }

            var codeLine = new Paragraph
            {
                Margin = new Thickness(0, 2, 0, 2),
                TextAlignment = TextAlignment.Center
            };
            codeLine.Inlines.Add(new Run(item.Barcode)
            {
                FontSize = 12,
                FontFamily = new FontFamily("Consolas")
            });
            section.Blocks.Add(codeLine);

            if (item.Price is > 0)
            {
                var priceLine = new Paragraph
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    TextAlignment = TextAlignment.Center
                };
                priceLine.Inlines.Add(new Run($"{item.Price:N0} د.ع")
                {
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold
                });
                section.Blocks.Add(priceLine);
            }

            doc.Blocks.Add(section);
        }

        var pd = new PrintDialog();
        if (pd.ShowDialog() != true) return;
        pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "ملصقات باركود");
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
                    Width = Math.Max(width, 120),
                    Height = Math.Max(height, 40),
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
