using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// In-app print preview with Chrome-like zoom/scroll, then system print dialog.
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
        PrintBrandingFlowDocumentHelper.SyncBrandingToPageWidth(document, pageSize.Width);

        var pad = document.PagePadding;
        document.ColumnWidth = Math.Max(1, pageSize.Width - pad.Left - pad.Right);
    }

    internal sealed class PrintPreviewWindow : Window
    {
        private const double MinZoom = 50;
        private const double MaxZoom = 300;
        private const double ZoomStep = 10;

        private readonly FlowDocument _document;
        private readonly string _jobName;
        private readonly TextBox _copiesInput;
        private readonly ScrollViewer _scrollViewer;
        private readonly FlowDocumentScrollViewer _documentViewer;
        private readonly ScaleTransform _scaleTransform;
        private readonly TextBlock _zoomLabel;

        public PrintPreviewWindow(FlowDocument document, string jobName, int defaultCopies = 1)
        {
            _document = document;
            _jobName = jobName;

            Title = "معاينة الطباعة";
            Width = 960;
            Height = 760;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial");

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var toolbar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xFA)),
                Padding = new Thickness(12, 10, 12, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
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
                Margin = new Thickness(0, 0, 16, 0)
            };
            copiesPanel.Children.Add(new TextBlock
            {
                Text = "عدد النسخ:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            copiesPanel.Children.Add(_copiesInput);

            var zoomOutButton = CreateZoomButton("−", "تصغير");
            zoomOutButton.Click += (_, _) => AdjustZoom(-ZoomStep);

            _zoomLabel = new TextBlock
            {
                Text = "100%",
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 52,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0),
                FontWeight = FontWeights.SemiBold
            };

            var zoomInButton = CreateZoomButton("+", "تكبير");
            zoomInButton.Click += (_, _) => AdjustZoom(ZoomStep);

            var zoomResetButton = new Button
            {
                Content = "100%",
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(4, 0, 4, 0),
                MinWidth = 56,
                ToolTip = "إعادة التعيين"
            };
            zoomResetButton.Click += (_, _) => SetZoom(100);

            var zoomFitButton = new Button
            {
                Content = "ملاءمة",
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 64,
                ToolTip = "ملاءمة العرض"
            };
            zoomFitButton.Click += (_, _) => FitToWidth();

            var zoomPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            zoomPanel.Children.Add(new TextBlock
            {
                Text = "التكبير:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            zoomPanel.Children.Add(zoomOutButton);
            zoomPanel.Children.Add(_zoomLabel);
            zoomPanel.Children.Add(zoomInButton);
            zoomPanel.Children.Add(zoomResetButton);
            zoomPanel.Children.Add(zoomFitButton);

            toolbarPanel.Children.Add(printButton);
            toolbarPanel.Children.Add(closeButton);
            toolbarPanel.Children.Add(copiesPanel);
            toolbarPanel.Children.Add(zoomPanel);
            toolbar.Child = toolbarPanel;
            Grid.SetRow(toolbar, 0);
            root.Children.Add(toolbar);

            _scaleTransform = new ScaleTransform(1, 1);
            _documentViewer = new FlowDocumentScrollViewer
            {
                Document = document,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = Brushes.White,
                Padding = new Thickness(24),
                LayoutTransform = _scaleTransform
            };

            var pageHost = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCF, 0xD8, 0xDC)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(24, 16, 24, 24),
                Child = _documentViewer
            };

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x52)),
                Content = pageHost,
                Focusable = true
            };
            _scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

            Grid.SetRow(_scrollViewer, 1);
            root.Children.Add(_scrollViewer);

            Content = root;
            Loaded += (_, _) => FitToWidth();
        }

        private static Button CreateZoomButton(string content, string toolTip) => new()
        {
            Content = content,
            Padding = new Thickness(10, 6, 10, 6),
            MinWidth = 36,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            ToolTip = toolTip
        };

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;

            AdjustZoom(e.Delta > 0 ? ZoomStep : -ZoomStep);
            e.Handled = true;
        }

        private void AdjustZoom(double delta) => SetZoom(_scaleTransform.ScaleX * 100 + delta);

        private void SetZoom(double percent)
        {
            var clamped = Math.Clamp(percent, MinZoom, MaxZoom) / 100.0;
            _scaleTransform.ScaleX = clamped;
            _scaleTransform.ScaleY = clamped;
            _documentViewer.LayoutTransform = _scaleTransform;
            UpdateZoomLabel();
        }

        private void FitToWidth()
        {
            if (_scrollViewer.ActualWidth <= 0)
            {
                SetZoom(100);
                return;
            }

            var available = _scrollViewer.ActualWidth - 80;
            var pageWidth = _document.PageWidth > 0 ? _document.PageWidth + 48 : DefaultPageWidth;
            var fitZoom = available / pageWidth * 100;
            SetZoom(Math.Clamp(fitZoom, MinZoom, MaxZoom));
        }

        private void UpdateZoomLabel() => _zoomLabel.Text = $"{_scaleTransform.ScaleX * 100:0}%";

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
