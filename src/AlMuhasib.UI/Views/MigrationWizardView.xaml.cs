using System.ComponentModel;
using System.Windows;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class MigrationWizardView
{
    private MigrationWizardViewModel? _vm;
    private int _lastTransitionToken;

    public MigrationWizardView() => InitializeComponent();

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MigrationWizardViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }

        PageEntranceAnimator.AnimateFadeSlide(HeroHeader, 0, axisY: true, from: 12);
        PageEntranceAnimator.AnimateFadeSlide(StepContentScroller, 120, axisY: false, from: -16);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MigrationWizardViewModel.StepTransitionToken) || _vm is null)
            return;

        if (_vm.StepTransitionToken == _lastTransitionToken)
            return;

        _lastTransitionToken = _vm.StepTransitionToken;
        PageEntranceAnimator.AnimateStepTransition(StepContentPanel, slideFromRight: true);
    }
}
