using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

public class BarcodeLabelService : IBarcodeLabelService
{
    public void PrintLabels(IEnumerable<BarcodeLabelItem> items)
    {
        var doc = new FlowDocument
        {
            PageWidth = 280,
            PagePadding = new Thickness(8),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FlowDirection = FlowDirection.RightToLeft
        };

        foreach (var item in items)
        {
            var block = new Paragraph { Margin = new Thickness(0, 0, 0, 12) };
            block.Inlines.Add(new Run(item.ProductName) { FontWeight = FontWeights.Bold, FontSize = 12 });
            block.Inlines.Add(new LineBreak());
            block.Inlines.Add(new Run(item.Barcode) { FontSize = 16, FontFamily = new System.Windows.Media.FontFamily("Consolas") });
            if (item.Price is > 0)
            {
                block.Inlines.Add(new LineBreak());
                block.Inlines.Add(new Run($"{item.Price:N0} د.ع") { FontSize = 11 });
            }
            doc.Blocks.Add(block);
        }

        var pd = new PrintDialog();
        if (pd.ShowDialog() != true) return;
        pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "ملصقات باركود");
    }
}
