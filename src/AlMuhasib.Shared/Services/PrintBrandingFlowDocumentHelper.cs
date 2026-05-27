using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AlMuhasib.Core;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Shared.Services;

public static class PrintBrandingFlowDocumentHelper
{
    public static void PrependBrandingHeader(FlowDocument doc, PrintBrandingSnapshot? branding = null)
    {
        branding ??= PrintBrandingProvider.Current;
        if (!branding.HasHeaderContent)
            return;

        var blocks = BuildHeaderBlocks(branding);
        var existing = doc.Blocks.ToList();
        doc.Blocks.Clear();
        foreach (var block in blocks)
            doc.Blocks.Add(block);
        foreach (var block in existing)
            doc.Blocks.Add(block);
    }

    public static void AppendBrandingFooter(FlowDocument doc, PrintBrandingSnapshot? branding = null, string? systemLine = null)
    {
        branding ??= PrintBrandingProvider.Current;

        if (branding.HasFooterContent)
        {
            foreach (var block in BuildFooterBlocks(branding))
                doc.Blocks.Add(block);
        }

        if (!string.IsNullOrWhiteSpace(systemLine))
        {
            doc.Blocks.Add(new Paragraph(new Run(systemLine))
            {
                FontSize = 10,
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            });
        }
    }

    public static FlowDocument BuildPreviewDocument(PrintBrandingSnapshot branding, string sampleTitle = "مثال: تقرير المبيعات")
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(36),
            PageWidth = 520,
            ColumnWidth = 520
        };

        PrependBrandingHeader(doc, branding);

        doc.Blocks.Add(new Paragraph(new Run(sampleTitle))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(new Paragraph(new Run($"التاريخ: {DateTime.Now:yyyy/MM/dd}"))
        {
            FontSize = 10,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn());
        table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        var header = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)) };
        foreach (var h in new[] { "الصنف", "المبلغ" })
        {
            header.Cells.Add(new TableCell(new Paragraph(new Run(h))
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            })
            { Padding = new Thickness(6, 4, 6, 4) });
        }
        group.Rows.Add(header);
        foreach (var row in new[] { new[] { "منتج تجريبي", "25,000" }, new[] { "منتج آخر", "18,500" } })
        {
            var tr = new TableRow();
            foreach (var cell in row)
                tr.Cells.Add(new TableCell(new Paragraph(new Run(cell)) { TextAlignment = TextAlignment.Center })
                { Padding = new Thickness(6, 3, 6, 3), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
            group.Rows.Add(tr);
        }
        table.RowGroups.Add(group);
        doc.Blocks.Add(table);

        AppendBrandingFooter(doc, branding, $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        return doc;
    }

    private static List<Block> BuildHeaderBlocks(PrintBrandingSnapshot branding)
    {
        var blocks = new List<Block>();

        if (branding.ShowHeaderImage && branding.HeaderImageData is { Length: > 0 })
        {
            var image = CreateImage(branding.HeaderImageData, 680, 120);
            if (image is not null)
            {
                blocks.Add(new BlockUIContainer(image)
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    TextAlignment = TextAlignment.Center
                });
            }
        }

        if (branding.ShowHeaderText)
        {
            if (!string.IsNullOrWhiteSpace(branding.CompanyName))
            {
                blocks.Add(new Paragraph(new Run(branding.CompanyName))
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x47, 0xA1)),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }

            if (!string.IsNullOrWhiteSpace(branding.Email))
            {
                blocks.Add(new Paragraph(new Run(branding.Email))
                {
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.DimGray,
                    Margin = new Thickness(0, 0, 0, 2)
                });
            }

            if (!string.IsNullOrWhiteSpace(branding.Details))
            {
                blocks.Add(new Paragraph(new Run(branding.Details))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }
        }

        blocks.Add(CreateSeparator());
        return blocks;
    }

    private static IEnumerable<Block> BuildFooterBlocks(PrintBrandingSnapshot branding)
    {
        yield return CreateSeparator();

        if (branding.ShowFooterImage && branding.FooterImageData is { Length: > 0 })
        {
            var image = CreateImage(branding.FooterImageData, 680, 90);
            if (image is not null)
            {
                yield return new BlockUIContainer(image)
                {
                    Margin = new Thickness(0, 6, 0, 6),
                    TextAlignment = TextAlignment.Center
                };
            }
        }

        if (branding.ShowFooterText && !string.IsNullOrWhiteSpace(branding.FooterText))
        {
            yield return new Paragraph(new Run(branding.FooterText))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 2)
            };
        }

        var contact = BuildFooterContactLine(branding);
        if (!string.IsNullOrWhiteSpace(contact))
        {
            yield return new Paragraph(new Run(contact))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Gray
            };
        }
    }

    private static Block CreateSeparator()
    {
        var line = new Table { CellSpacing = 0, Margin = new Thickness(0, 6, 0, 6) };
        line.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var group = new TableRowGroup();
        var row = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)) };
        row.Cells.Add(new TableCell(new Paragraph(new Run(" ")) { FontSize = 1 }) { Padding = new Thickness(0, 1, 0, 1) });
        group.Rows.Add(row);
        line.RowGroups.Add(group);
        return line;
    }

    private static string BuildFooterContactLine(PrintBrandingSnapshot branding)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(branding.Address))
            parts.Add(branding.Address);
        if (!string.IsNullOrWhiteSpace(branding.PhonePrimary))
            parts.Add($"هاتف: {branding.PhonePrimary}");
        if (!string.IsNullOrWhiteSpace(branding.PhoneSecondary))
            parts.Add($"هاتف: {branding.PhoneSecondary}");
        return string.Join("  |  ", parts);
    }

    private static Image? CreateImage(byte[] data, double maxWidth, double maxHeight)
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

            return new Image
            {
                Source = bmp,
                MaxWidth = maxWidth,
                MaxHeight = maxHeight,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }
        catch
        {
            return null;
        }
    }
}
