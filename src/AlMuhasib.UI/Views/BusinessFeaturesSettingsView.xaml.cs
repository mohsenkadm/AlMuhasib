using System.Windows;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class BusinessFeaturesSettingsView
{
    public BusinessFeaturesSettingsView() => InitializeComponent();

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        PageEntranceAnimator.AnimateFadeSlide(HeroHeader, 0, axisY: true, from: 16);
        PageEntranceAnimator.StaggerChildren(
        [
            SectionReminders,
            SectionBackup,
            SectionAccounting,
            SectionTemplates,
            SectionSecurity,
            SavePanel
        ], startDelayMs: 80, staggerMs: 80);
    }
}
