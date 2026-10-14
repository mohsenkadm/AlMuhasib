using System.Windows;
using System.Windows.Input;

namespace AlMuhasib.UI.Windows;

public partial class DuhokBranchActivationDialog : Window
{
    private const string ExpectedPassword = "wss_123";

    public DuhokBranchActivationDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Activate_Click(sender, e);
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (string.Equals(PasswordBox.Password, ExpectedPassword, StringComparison.Ordinal))
        {
            DialogResult = true;
            Close();
            return;
        }

        ErrorText.Text = "كلمة المرور غير صحيحة";
        ErrorText.Visibility = Visibility.Visible;
        PasswordBox.Clear();
        PasswordBox.Focus();
    }
}
