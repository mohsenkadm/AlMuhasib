using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class VoiceAssistantOverlay : UserControl
{
    private Storyboard? _waveStoryboard;
    private Storyboard? _innerWaveStoryboard;

    public VoiceAssistantOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        IsVisibleChanged += OnIsVisibleChanged;
        DataContextChanged += (_, _) => HookViewModel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BeginStoryboard("OrbPulse");
        BeginStoryboard("OrbHueShift");
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            BeginStoryboard("OverlayEntrance");
            if (DataContext is MainWindowViewModel vm)
                UpdateWaveAnimation(vm.VoiceAssistantState);
        }
        else
        {
            StopWaveAnimation();
            StopInnerWaveAnimation();
        }
    }

    private void BeginStoryboard(string key)
    {
        if (Resources[key] is not Storyboard template)
            return;

        var storyboard = template.Clone();
        storyboard.Begin(this);
    }

    private void HookViewModel()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.VoiceAssistantState))
                UpdateWaveAnimation(vm.VoiceAssistantState);
            if (args.PropertyName == nameof(MainWindowViewModel.IsVoicePackInstalling))
                UpdateInstallVisuals(vm.IsVoicePackInstalling);
        };
    }

    private void UpdateInstallVisuals(bool isInstalling)
    {
        if (isInstalling)
            BeginStoryboard("OrbPulse");
        else if (DataContext is MainWindowViewModel vm)
            UpdateWaveAnimation(vm.VoiceAssistantState);
    }

    private void UpdateWaveAnimation(VoiceAssistantState state)
    {
        var isActive = state is VoiceAssistantState.Listening or VoiceAssistantState.Processing;

        if (isActive)
        {
            StartWaveAnimation();
            StartInnerWaveAnimation();
        }
        else
        {
            StopWaveAnimation();
            StopInnerWaveAnimation();
        }

        UpdateOrbCenterVisual(isActive);
    }

    private void UpdateOrbCenterVisual(bool isListening)
    {
        if (FindName("OrbCenterLight") is FrameworkElement centerLight)
            centerLight.Opacity = isListening ? 0 : 0.85;

        if (FindName("OrbInnerWaves") is FrameworkElement innerWaves)
            innerWaves.Opacity = isListening ? 0.95 : 0;
    }

    private void StartInnerWaveAnimation()
    {
        _innerWaveStoryboard?.Stop();
        _innerWaveStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

        foreach (var name in new[] { "InnerWave1", "InnerWave2", "InnerWave3", "InnerWave4", "InnerWave5" })
        {
            if (FindName(name) is not FrameworkElement element)
                continue;

            var anim = new DoubleAnimation
            {
                From = 8,
                To = 28,
                Duration = TimeSpan.FromMilliseconds(340 + Random.Shared.Next(140)),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("Height"));
            _innerWaveStoryboard.Children.Add(anim);
        }

        _innerWaveStoryboard.Begin(this);
    }

    private void StopInnerWaveAnimation()
    {
        _innerWaveStoryboard?.Stop();
        _innerWaveStoryboard = null;
    }

    private void StartWaveAnimation()
    {
        _waveStoryboard?.Stop();
        _waveStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

        foreach (var name in new[] { "Wave1", "Wave2", "Wave3", "Wave4", "Wave5", "Wave6", "Wave7" })
        {
            if (FindName(name) is not FrameworkElement element)
                continue;

            var anim = new DoubleAnimation
            {
                From = 6,
                To = 36,
                Duration = TimeSpan.FromMilliseconds(380 + Random.Shared.Next(180)),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("Height"));
            _waveStoryboard.Children.Add(anim);
        }

        _waveStoryboard.Begin(this);
    }

    private void StopWaveAnimation()
    {
        _waveStoryboard?.Stop();
        _waveStoryboard = null;
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is MainWindowViewModel vm)
        {
            vm.CloseVoiceAssistantCommand.Execute(null);
            e.Handled = true;
        }
    }
}
