using System.Windows;
using System.Windows.Controls;

namespace AlMuhasib.UI.Views;

public partial class ProductAnalyticsReportView : UserControl
{
    public ProductAnalyticsReportView()
    {
        InitializeComponent();
    }

    private void ColumnsButton_Click(object sender, RoutedEventArgs e)
    {
        ColumnPanel.Children.Clear();
        foreach (var column in AnalyticsGrid.Columns)
        {
            if (column.Header is not string header || string.IsNullOrWhiteSpace(header))
                continue;
            var box = new CheckBox
            {
                Content = header,
                IsChecked = column.Visibility == Visibility.Visible,
                Margin = new Thickness(0, 0, 0, 6),
                FlowDirection = FlowDirection.RightToLeft
            };
            box.Checked += (_, _) => column.Visibility = Visibility.Visible;
            box.Unchecked += (_, _) => column.Visibility = Visibility.Collapsed;
            ColumnPanel.Children.Add(box);
        }
        ColumnPopup.IsOpen = true;
    }
}
