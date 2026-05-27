using System.Collections.ObjectModel;
using System.Windows.Controls;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Controls;

public partial class ToastHost : UserControl
{
    public ObservableCollection<ToastNotification> Toasts { get; } = [];

    public ToastHost()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void PushToast(ToastNotification toast)
    {
        Toasts.Insert(0, toast);

        while (Toasts.Count > 6)
            Toasts.RemoveAt(Toasts.Count - 1);
    }

    public void RemoveToast(ToastNotification toast) => Toasts.Remove(toast);
}
