using System.Windows;
using AlMuhasib.Core.Models.Updates;

namespace AlMuhasib.UI.Windows;

public partial class UpdatePromptWindow : Window
{
    private readonly AppUpdateManifest _manifest;
    private readonly Version _current;
    private readonly Version _available;

    public bool UserAcceptedUpdate { get; private set; }

    public UpdatePromptWindow(AppUpdateCheckResult check)
    {
        InitializeComponent();
        _manifest = check.Manifest!;
        _current = check.CurrentVersion!;
        _available = check.AvailableVersion!;

        VersionText.Text = $"الإصدار الحالي: {_current}  →  الجديد: {_available}";
        ReleaseNotesText.Text = string.IsNullOrWhiteSpace(_manifest.ReleaseNotes)
            ? "لا توجد ملاحظات لهذا الإصدار."
            : _manifest.ReleaseNotes.Trim();

        if (_manifest.IsMandatory)
        {
            MandatoryHint.Visibility = Visibility.Visible;
            LaterButton.Visibility = Visibility.Collapsed;
        }
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        UserAcceptedUpdate = true;
        DialogResult = true;
        Close();
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        UserAcceptedUpdate = false;
        DialogResult = false;
        Close();
    }
}
