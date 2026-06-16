using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// In-app print preview (FlowDocumentPageViewer) then system print dialog.
/// DocumentViewer only supports FixedDocument and shows "not supported" for FlowDocument.
/// </summary>
public static class DocumentPrintHelper
{
    private const double DefaultPageWidth = 793.7;  // A4 @ 96 DPI
    private const double DefaultPageHeight = 1122.5;

    public static void PrintWithPreview(FlowDocument document, string jobName, Size? pageSize = null, int defaultCopies = 1)
    {
        var size = pageSize ?? new Size(DefaultPageWidth, DefaultPageHeight);
        ApplyPageLayout(document, size);

        var preview = new PrintPreviewWindow(document, jobName, defaultCopies);
        preview.ShowDialog();
    }

    internal static void ApplyPageLayout(FlowDocument document, Size pageSize)
    {
        document.PageWidth = pageSize.Width;
        document.PageHeight = pageSize.Height;
        document.ColumnWidth = pageSize.Width;
    }

    internal sealed class PrintPreviewWindow : Window
    {
        private readonly FlowDocument _document;
        private readonly string _jobName;
        private readonly TextBox _copiesInput;

        public PrintPreviewWindow(FlowDocument document, string jobName, int defaultCopies = 1)
        {
            _document = document;
            _jobName = jobName;

            Title = "معاينة الطباعة";
            Width = 900;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Tahoma, Arial");

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xF5, 0xF7, 0xFA)),
                Padding = new Thickness(12, 10, 12, 10),
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var printButton = new Button
            {
                Content = "طباعة",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                FontWeight = FontWeights.SemiBold
            };
            printButton.Click += OnPrintClick;

            var closeButton = new Button
            {
                Content = "إغلاق",
                Padding = new Thickness(20, 8, 20, 8),
                MinWidth = 100,
                Margin = new Thickness(0, 0, 16, 0)
            };
            closeButton.Click += (_, _) => Close();

            _copiesInput = new TextBox
            {
                Width = 56,
                Text = Math.Max(1, defaultCopies).ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            var copiesPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            copiesPanel.Children.Add(new TextBlock
            {
                Text = "عدد النسخ:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            copiesPanel.Children.Add(_copiesInput);

            toolbarPanel.Children.Add(printButton);
            toolbarPanel.Children.Add(closeButton);
            toolbarPanel.Children.Add(copiesPanel);
            toolbar.Child = toolbarPanel;
            Grid.SetRow(toolbar, 0);
            root.Children.Add(toolbar);

            var viewer = new FlowDocumentPageViewer
            {
                Document = document,
                Zoom = 90,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Grid.SetRow(viewer, 1);
            root.Children.Add(viewer);

            Content = root;
        }

        private void OnPrintClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_copiesInput.Text.Trim(), out var copies) || copies < 1)
            {
                MessageBox.Show("أدخل عدداً صحيحاً للنسخ (1 أو أكثر).", "طباعة", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
                return;

            var paginator = ((IDocumentPaginatorSource)_document).DocumentPaginator;
            paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

            try
            {
                printDialog.PrintTicket.CopyCount = copies;
            }
            catch
            {
                for (var i = 0; i < copies; i++)
                    printDialog.PrintDocument(paginator, _jobName);
                Close();
                return;
            }

            printDialog.PrintDocument(paginator, _jobName);
            Close();
        }
    }
}
