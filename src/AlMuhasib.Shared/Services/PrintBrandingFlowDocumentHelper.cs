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
  /// <summary>A4 @ 96 DPI — used when PageWidth is not set yet at document build time.</summary>
  public const double DefaultPrintPageWidth = 793.7;

  private sealed class BrandingImageState
  {
    public double MaxHeight { get; init; }
    public bool BleedTop { get; init; }
    public bool BleedSides { get; init; } = true;
  }

  public static double GetEffectivePageWidth(FlowDocument doc) =>
      doc.PageWidth > 0 ? doc.PageWidth : DefaultPrintPageWidth;

  /// <summary>
  /// Re-sizes branding header/footer images after the real print page width is applied (e.g. A4).
  /// </summary>
  public static void SyncBrandingToPageWidth(FlowDocument doc, double pageWidth)
  {
    if (pageWidth <= 0)
      return;

    foreach (var block in doc.Blocks)
      SyncBrandingBlock(block, doc, pageWidth);
  }

  public static void PrependBrandingHeader(FlowDocument doc, PrintBrandingSnapshot? branding = null)
      => PrependBrandingHeader(doc, branding, maxHeaderImageHeight: null);

  /// <param name="maxHeaderImageHeight">اختياري لضغط الهيدر (مثلاً عقود الطباعة على صفحة واحدة).</param>
  public static void PrependBrandingHeader(
      FlowDocument doc,
      PrintBrandingSnapshot? branding,
      double? maxHeaderImageHeight)
  {
    branding ??= PrintBrandingProvider.Current;
    if (!branding.HasHeaderContent)
      return;

    var blocks = BuildHeaderBlocks(branding, doc, maxHeaderImageHeight);
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
      foreach (var block in BuildFooterBlocks(branding, doc))
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
    const double pageWidth = 420;
    const double horizontalPadding = 28;
    var contentWidth = pageWidth - (horizontalPadding * 2);

    var doc = new FlowDocument
    {
      FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
      FontSize = 12,
      FlowDirection = FlowDirection.RightToLeft,
      PagePadding = new Thickness(horizontalPadding),
      PageWidth = pageWidth,
      ColumnWidth = contentWidth
    };

    PrependBrandingHeader(doc, branding);
    SyncBrandingToPageWidth(doc, pageWidth);

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
    SyncBrandingToPageWidth(doc, pageWidth);
    return doc;
  }

  private static List<Block> BuildHeaderBlocks(
      PrintBrandingSnapshot branding,
      FlowDocument doc,
      double? maxHeaderImageHeight = null)
  {
    var blocks = new List<Block>();

    if (branding.ShowHeaderImage && branding.HeaderImageData is { Length: > 0 })
    {
      var imageOnlyHeader = IsImageOnlyHeader(branding);
      var defaultMax = imageOnlyHeader ? 235.0 : 175.0;
      var maxHeight = maxHeaderImageHeight.HasValue
          ? Math.Min(maxHeaderImageHeight.Value, defaultMax)
          : defaultMax;
      var imageBlock = CreateFullWidthImageBlock(
          branding.HeaderImageData,
          doc,
          maxHeight: maxHeight,
          bleedTop: imageOnlyHeader && maxHeaderImageHeight is null);
      if (imageBlock is not null)
        blocks.Add(imageBlock);
    }

    if (branding.ShowHeaderText)
    {
      var compact = maxHeaderImageHeight.HasValue;
      if (!string.IsNullOrWhiteSpace(branding.CompanyName))
      {
        blocks.Add(new Paragraph(new Run(branding.CompanyName))
        {
          FontSize = compact ? 14 : 20,
          FontWeight = FontWeights.Bold,
          Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x47, 0xA1)),
          TextAlignment = TextAlignment.Center,
          Margin = new Thickness(0, 0, 0, compact ? 1 : 4)
        });
      }

      if (!string.IsNullOrWhiteSpace(branding.Email))
      {
        blocks.Add(new Paragraph(new Run(branding.Email))
        {
          FontSize = compact ? 9 : 11,
          TextAlignment = TextAlignment.Center,
          Foreground = Brushes.DimGray,
          Margin = new Thickness(0, 0, 0, compact ? 1 : 2)
        });
      }

      if (!string.IsNullOrWhiteSpace(branding.Details))
      {
        blocks.Add(new Paragraph(new Run(branding.Details))
        {
          FontSize = compact ? 8.5 : 10,
          TextAlignment = TextAlignment.Center,
          Foreground = Brushes.Gray,
          Margin = new Thickness(0, 0, 0, 4)
        });
      }
    }

    blocks.Add(CreateSeparator());
    return blocks;
  }

  private static IEnumerable<Block> BuildFooterBlocks(PrintBrandingSnapshot branding, FlowDocument doc)
  {
    yield return CreateSeparator();

    if (branding.ShowFooterImage && branding.FooterImageData is { Length: > 0 })
    {
      var imageBlock = CreateFullWidthImageBlock(
          branding.FooterImageData,
          doc,
          maxHeight: 100,
          bleedTop: false);
      if (imageBlock is not null)
      {
        imageBlock.Margin = new Thickness(0, 6, 0, 6);
        yield return imageBlock;
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

  private static bool IsImageOnlyHeader(PrintBrandingSnapshot branding) =>
      !branding.ShowHeaderText
      || (string.IsNullOrWhiteSpace(branding.CompanyName)
          && string.IsNullOrWhiteSpace(branding.Email)
          && string.IsNullOrWhiteSpace(branding.Details));

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

  private static Block? CreateFullWidthImageBlock(
      byte[] data,
      FlowDocument doc,
      double maxHeight,
      bool bleedTop)
  {
    var bmp = LoadBitmap(data);
    if (bmp is null) return null;

    var state = new BrandingImageState
    {
      MaxHeight = maxHeight,
      BleedTop = bleedTop,
      BleedSides = true
    };

    var pageWidth = GetEffectivePageWidth(doc);
    var image = CreateBleedingImage(bmp, doc, pageWidth, state);

    return new BlockUIContainer(image)
    {
      Margin = new Thickness(0, 0, 0, 8),
      Tag = state
    };
  }

  private static void SyncBrandingBlock(Block block, FlowDocument doc, double pageWidth)
  {
    if (block is not BlockUIContainer { Child: Image img, Tag: BrandingImageState state })
      return;

    if (img.Source is not BitmapSource bmp)
      return;

    ApplyBleedingImageLayout(img, doc, pageWidth, bmp, state);
  }

  private static Image CreateBleedingImage(
      BitmapSource bmp,
      FlowDocument doc,
      double pageWidth,
      BrandingImageState state)
  {
    var image = new Image
    {
      Source = bmp,
      Stretch = Stretch.Fill,
      HorizontalAlignment = HorizontalAlignment.Stretch,
      VerticalAlignment = VerticalAlignment.Top,
      SnapsToDevicePixels = true
    };

    ApplyBleedingImageLayout(image, doc, pageWidth, bmp, state);
    return image;
  }

  private static void ApplyBleedingImageLayout(
      Image image,
      FlowDocument doc,
      double pageWidth,
      BitmapSource bmp,
      BrandingImageState state)
  {
    var pad = doc.PagePadding;
    var bleedLeft = state.BleedSides ? pad.Left : 0;
    var bleedRight = state.BleedSides ? pad.Right : 0;
    var bleedTop = state.BleedTop ? pad.Top : 0;

    var aspect = bmp.PixelHeight / (double)Math.Max(1, bmp.PixelWidth);
    var targetHeight = Math.Min(state.MaxHeight, Math.Max(56, pageWidth * aspect));

    image.Width = pageWidth;
    image.Height = targetHeight;
    image.Margin = new Thickness(-bleedLeft, -bleedTop, -bleedRight, 0);
  }

  private static BitmapImage? LoadBitmap(byte[] data)
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
      return bmp;
    }
    catch
    {
      return null;
    }
  }
}
