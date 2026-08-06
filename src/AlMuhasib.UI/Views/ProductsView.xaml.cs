using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        ProductFeatureColumnSync.Attach(this, ColScientificName, ColUsageInstructions);
        CustomFieldColumnSync.Attach(
            this,
            ProductsGrid,
            [ColCf1, ColCf2, ColCf3, ColCf4, ColCf5, ColCf6, ColCf7, ColCf8],
            vm => vm is ProductsViewModel p ? p.GetCustomFieldColumnStates() : null,
            nameof(ProductsViewModel.CustomFieldColumnsVersion));
    }

    private void ProductsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProductsViewModel vm && sender is DataGrid grid)
            vm.UpdateBulkDiscountSelectionFromGrid(grid);
    }

    private void ProductDialogRoot_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement root)
            return;

        root.Opacity = 0;
        if (root.RenderTransform is not ScaleTransform)
            root.RenderTransform = new ScaleTransform(0.94, 0.94);
        root.RenderTransformOrigin = new Point(0.5, 0.5);

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        root.BeginAnimation(UIElement.OpacityProperty, fade);

        if (root.RenderTransform is ScaleTransform st)
        {
            var sx = new DoubleAnimation(0.94, 1, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
            };
            var sy = new DoubleAnimation(0.94, 1, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
        }
    }
}
