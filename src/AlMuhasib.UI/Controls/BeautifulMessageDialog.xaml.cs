using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

public enum MessageDialogType
{
    Info,
    Success,
    Warning,
    Error,
    Confirm
}

public partial class BeautifulMessageDialog : Window
{
    public bool ResultYes { get; private set; }

    public BeautifulMessageDialog()
    {
        InitializeComponent();
    }

    private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    public static void ShowInfo(string message, string title = "معلومة")
    {
        Show(message, title, MessageDialogType.Info, false);
    }

    public static void ShowSuccess(string message, string title = "نجاح")
    {
        Show(message, title, MessageDialogType.Success, false);
    }

    public static void ShowWarning(string message, string title = "تنبيه")
    {
        Show(message, title, MessageDialogType.Warning, false);
    }

    public static void ShowError(string message, string title = "خطأ")
    {
        Show(message, title, MessageDialogType.Error, false);
    }

    public static bool ShowConfirm(string message, string title = "تأكيد")
    {
        return Show(message, title, MessageDialogType.Confirm, true);
    }

    private static bool Show(string message, string title, MessageDialogType type, bool isConfirm)
    {
        var dialog = new BeautifulMessageDialog();
        dialog.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                     ?? Application.Current.MainWindow;
        dialog.MessageText.Text = message;
        dialog.TitleText.Text = title;

        ApplyTheme(dialog, type);
        CreateButtons(dialog, isConfirm);

        dialog.ShowDialog();
        return dialog.ResultYes;
    }

    private static void ApplyTheme(BeautifulMessageDialog dialog, MessageDialogType type)
    {
        switch (type)
        {
            case MessageDialogType.Info:
                dialog.HeaderBorder.Background = CreateGradient("#1565C0", "#1E88E5", "#42A5F5");
                dialog.IconCircle.Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                dialog.DialogIcon.Kind = PackIconKind.InformationOutline;
                break;
            case MessageDialogType.Success:
                dialog.HeaderBorder.Background = CreateGradient("#2E7D32", "#43A047", "#66BB6A");
                dialog.IconCircle.Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                dialog.DialogIcon.Kind = PackIconKind.CheckCircleOutline;
                break;
            case MessageDialogType.Warning:
                dialog.HeaderBorder.Background = CreateGradient("#E65100", "#F57C00", "#FB8C00");
                dialog.IconCircle.Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                dialog.DialogIcon.Kind = PackIconKind.AlertOutline;
                break;
            case MessageDialogType.Error:
                dialog.HeaderBorder.Background = CreateGradient("#B71C1C", "#C62828", "#E53935");
                dialog.IconCircle.Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                dialog.DialogIcon.Kind = PackIconKind.CloseCircleOutline;
                break;
            case MessageDialogType.Confirm:
                dialog.HeaderBorder.Background = CreateGradient("#1565C0", "#1E88E5", "#42A5F5");
                dialog.IconCircle.Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                dialog.DialogIcon.Kind = PackIconKind.HelpCircleOutline;
                break;
        }
    }

    private static LinearGradientBrush CreateGradient(string c1, string c2, string c3)
    {
        return new LinearGradientBrush(
        [
            new GradientStop((Color)ColorConverter.ConvertFromString(c1), 0),
            new GradientStop((Color)ColorConverter.ConvertFromString(c2), 0.5),
            new GradientStop((Color)ColorConverter.ConvertFromString(c3), 1)
        ], 45);
    }

    private static void CreateButtons(BeautifulMessageDialog dialog, bool isConfirm)
    {
        dialog.ButtonPanel.Children.Clear();

        if (isConfirm)
        {
            var yesBtn = CreateButton("نعم", "#1565C0", "#0D47A1", PackIconKind.Check, true);
            yesBtn.Click += (_, _) => { dialog.ResultYes = true; dialog.Close(); };

            var noBtn = CreateButton("لا", "#F5F5F5", "#E0E0E0", PackIconKind.Close, false);
            noBtn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161"));
            noBtn.Click += (_, _) => { dialog.ResultYes = false; dialog.Close(); };

            dialog.ButtonPanel.Children.Add(yesBtn);
            dialog.ButtonPanel.Children.Add(noBtn);
        }
        else
        {
            var okBtn = CreateButton("حسناً", "#1565C0", "#0D47A1", PackIconKind.Check, true);
            okBtn.Click += (_, _) => dialog.Close();
            dialog.ButtonPanel.Children.Add(okBtn);
        }
    }

    private static Button CreateButton(string text, string bgColor, string hoverColor, PackIconKind icon, bool isPrimary)
    {
        var btn = new Button
        {
            Height = 44,
            MinWidth = 130,
            Padding = new Thickness(20, 0, 20, 0),
            Margin = new Thickness(6, 0, 6, 0),
            Cursor = Cursors.Hand,
            FontSize = 14,
            FontWeight = FontWeights.DemiBold,
            Foreground = isPrimary ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
            BorderThickness = new Thickness(0),
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        sp.Children.Add(new PackIcon { Kind = icon, Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        sp.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
        btn.Content = sp;

        // Use a custom template for rounded corners
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(PaddingProperty));
        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(contentPresenter);
        template.VisualTree = border;
        btn.Template = template;

        // Hover effect
        var bg = (Color)ColorConverter.ConvertFromString(bgColor);
        var hover = (Color)ColorConverter.ConvertFromString(hoverColor);
        btn.MouseEnter += (_, _) =>
        {
            btn.Background = new SolidColorBrush(hover);
            ((ScaleTransform)btn.RenderTransform).ScaleX = 1.04;
            ((ScaleTransform)btn.RenderTransform).ScaleY = 1.04;
        };
        btn.MouseLeave += (_, _) =>
        {
            btn.Background = new SolidColorBrush(bg);
            ((ScaleTransform)btn.RenderTransform).ScaleX = 1.0;
            ((ScaleTransform)btn.RenderTransform).ScaleY = 1.0;
        };

        return btn;
    }
}
