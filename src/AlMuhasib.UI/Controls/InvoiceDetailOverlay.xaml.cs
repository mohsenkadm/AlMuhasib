using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AlMuhasib.UI.Controls;

public partial class InvoiceDetailOverlay : UserControl
{
  private Popup? _popup;

  public InvoiceDetailOverlay()
  {
    InitializeComponent();
  }

  public void ShowCentered()
  {
    var window = Window.GetWindow(this) ?? Application.Current.MainWindow;
    if (window is null)
      return;

    _popup = new Popup
    {
      AllowsTransparency = true,
      StaysOpen = true,
      PlacementTarget = window,
      Placement = PlacementMode.Relative,
      Child = this
    };

    _popup.Opened += (_, _) =>
    {
      RootHost.Width = window.ActualWidth;
      RootHost.Height = window.ActualHeight;
      DialogCard.Width = Math.Min(920, window.ActualWidth - 48);
      DialogCard.MaxHeight = Math.Max(560, window.ActualHeight * 0.88);

      Backdrop.Opacity = 0;
      DialogCard.Opacity = 0;
      if (DialogCard.RenderTransform is ScaleTransform scale)
        scale.ScaleX = scale.ScaleY = 0.92;

      Backdrop.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
      DialogCard.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320))
      {
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
      });

      var grow = new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(320))
      {
        EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.28 }
      };
      if (DialogCard.RenderTransform is ScaleTransform st)
      {
        st.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
      }
    };

    _popup.IsOpen = true;
  }

  private void Close()
  {
    if (_popup is null || !_popup.IsOpen)
      return;

    var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180))
    {
      EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
    };
    fade.Completed += (_, _) => _popup.IsOpen = false;
    Backdrop.BeginAnimation(OpacityProperty, fade);
  }

  private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

  private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
  {
    if (e.OriginalSource == Backdrop)
      Close();
  }
}
