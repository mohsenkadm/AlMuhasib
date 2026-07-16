using System.Windows;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.License;

namespace AlMuhasib.UI.Windows;

public partial class DesktopActivationWindow : Window
{
    private readonly IDesktopLicenseService _licenseService;

    public bool ActivatedSuccessfully { get; private set; }

    public DesktopActivationWindow(IDesktopLicenseService licenseService, DesktopLicenseStatus status, bool allowDismissWhileValid)
    {
        InitializeComponent();
        _licenseService = licenseService;

        InstallationIdBox.Text = status.InstallationId == Guid.Empty
            ? "—"
            : status.InstallationId.ToString("D");

        if (status.IsUsable && status.IsTrial)
        {
            TitleText.Text = "تفعيل النظام";
            MessageText.Text =
                $"النظام في فترة تجريبية (متبقي {status.DaysRemaining ?? 0} يوماً). يمكنك إدخال مفتاح التفعيل مدى الحياة في أي وقت.";
            ContinueButton.Visibility = Visibility.Visible;
        }
        else if (status.IsUsable)
        {
            TitleText.Text = "حالة الترخيص";
            MessageText.Text = status.Summary;
            ContinueButton.Visibility = Visibility.Visible;
        }
        else
        {
            TitleText.Text = "انتهت الفترة التجريبية";
            MessageText.Text =
                "انتهت مدة تجربة برنامج قيد. أدخل مفتاح التفعيل مدى الحياة الذي يزوّدك به المطوّر للمتابعة. بياناتك محفوظة ولن تُحذف.";
            ContinueButton.Visibility = allowDismissWhileValid ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void CopyId_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(InstallationIdBox.Text);
        }
        catch
        {
            // ignore clipboard failures
        }
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        if (!_licenseService.TryActivate(ActivationKeyBox.Text, out var error))
        {
            ErrorText.Text = error ?? "تعذّر التفعيل.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ActivatedSuccessfully = true;
        DialogResult = true;
        Close();
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
