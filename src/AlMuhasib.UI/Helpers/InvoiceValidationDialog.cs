using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.Helpers;

/// <summary>حوارات التحقق الموحدة عند حفظ الفواتير.</summary>
public static class InvoiceValidationDialog
{
    public static void ShowBlockingError(string message, string title = "تعذّر الحفظ")
    {
        BeautifulMessageDialog.ShowError(message, title);
    }

    public static bool ShowWarningConfirm(string message, string title = "تنبيه")
    {
        return BeautifulMessageDialog.ShowConfirm(message, title);
    }
}
