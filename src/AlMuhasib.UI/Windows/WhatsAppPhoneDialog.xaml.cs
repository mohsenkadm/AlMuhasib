using System.Windows;
using System.Windows.Input;

namespace AlMuhasib.UI.Windows;

public partial class WhatsAppPhoneDialog : Window
{
    public string PhoneNumber => PhoneTextBox.Text.Trim();

    public WhatsAppPhoneDialog(string customerName, string? existingPhone)
    {
        InitializeComponent();
        SubtitleText.Text = string.IsNullOrWhiteSpace(existingPhone)
            ? $"لا يوجد رقم مسجّل للعميل «{customerName}». أدخل رقم واتساب (يُحوَّل تلقائياً إلى +964)."
            : $"العميل: {customerName}\nيمكنك تعديل الرقم قبل الإرسال.";
        PhoneTextBox.Text = existingPhone ?? string.Empty;
        Loaded += (_, _) => PhoneTextBox.Focus();
        PhoneTextBox.SelectAll();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            MessageBox.Show(this, "يرجى إدخال رقم الهاتف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void PhoneTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Ok_Click(sender, e);
    }
}
