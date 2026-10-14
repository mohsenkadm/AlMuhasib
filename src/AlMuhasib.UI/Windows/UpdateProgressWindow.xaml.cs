using System.Windows;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Windows;

public partial class UpdateProgressWindow : Window
{
    public UpdateProgressWindow()
    {
        InitializeComponent();
        Title = $"تحديث {AppInfo.AppNameAr}";
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
    }
}
