using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AlMuhasib.UI.Views;

public partial class PersonProfileView : UserControl
{
    public PersonProfileView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PlayEntranceAnimation();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ViewModels.PersonProfileViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModels.PersonProfileViewModel.HasSelection)
            or nameof(ViewModels.PersonProfileViewModel.ProfileContentKey))
        {
            Dispatcher.BeginInvoke(PlayContentRevealAnimation);
        }
    }

    private void PlayEntranceAnimation()
    {
        if (RootPanel is null) return;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slide = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(420))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        RootPanel.BeginAnimation(OpacityProperty, fade);
        if (RootPanel.RenderTransform is System.Windows.Media.TranslateTransform transform)
            transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
    }

    private void PlayContentRevealAnimation()
    {
        if (ProfileContent is null || ProfileContent.Visibility != Visibility.Visible)
            return;

        ProfileContent.Opacity = 0;
        if (ProfileContent.RenderTransform is not System.Windows.Media.TranslateTransform transform)
            return;

        transform.Y = 24;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(380))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slide = new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(420))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        ProfileContent.BeginAnimation(OpacityProperty, fade);
        transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
    }
}
