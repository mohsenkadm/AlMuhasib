using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AlMuhasib.UI.Views;

public partial class ProductFormView : UserControl
{
    public ProductFormView() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (TryFindResource("SectionEnter") is Storyboard sb && FormRoot is not null)
        {
            Storyboard.SetTarget(sb, FormRoot);
            sb.Begin();
        }

        Dispatcher.BeginInvoke(() =>
        {
            NameBox?.Focus();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }
}
