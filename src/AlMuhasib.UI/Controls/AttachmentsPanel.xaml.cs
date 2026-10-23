using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Controls;

public partial class AttachmentsPanel : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(AttachmentsPanel),
            new PropertyMetadata("المرفقات"));

    public static readonly DependencyProperty SaveFirstHintProperty =
        DependencyProperty.Register(nameof(SaveFirstHint), typeof(string), typeof(AttachmentsPanel),
            new PropertyMetadata("احفظ السجل أولاً لتفعيل المرفقات"));

    public static readonly DependencyProperty AddButtonTextProperty =
        DependencyProperty.Register(nameof(AddButtonText), typeof(string), typeof(AttachmentsPanel),
            new PropertyMetadata("+ إضافة مرفق"));

    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(AttachmentsPanel),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HasSavedOwnerProperty =
        DependencyProperty.Register(nameof(HasSavedOwner), typeof(bool), typeof(AttachmentsPanel),
            new PropertyMetadata(false, OnAddEnablementChanged));

    public static readonly DependencyProperty CanManageProperty =
        DependencyProperty.Register(nameof(CanManage), typeof(bool), typeof(AttachmentsPanel),
            new PropertyMetadata(false, OnAddEnablementChanged));

    public static readonly DependencyProperty CanDeleteProperty =
        DependencyProperty.Register(nameof(CanDelete), typeof(bool), typeof(AttachmentsPanel),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShowHeaderProperty =
        DependencyProperty.Register(nameof(ShowHeader), typeof(bool), typeof(AttachmentsPanel),
            new PropertyMetadata(true));

    public static readonly DependencyProperty MaxGridHeightProperty =
        DependencyProperty.Register(nameof(MaxGridHeight), typeof(double), typeof(AttachmentsPanel),
            new PropertyMetadata(180d));

    public static readonly DependencyProperty AddCommandProperty =
        DependencyProperty.Register(nameof(AddCommand), typeof(ICommand), typeof(AttachmentsPanel),
            new PropertyMetadata(null));

    public static readonly DependencyProperty OpenCommandProperty =
        DependencyProperty.Register(nameof(OpenCommand), typeof(ICommand), typeof(AttachmentsPanel),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(AttachmentsPanel),
            new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string SaveFirstHint
    {
        get => (string)GetValue(SaveFirstHintProperty);
        set => SetValue(SaveFirstHintProperty, value);
    }

    public string AddButtonText
    {
        get => (string)GetValue(AddButtonTextProperty);
        set => SetValue(AddButtonTextProperty, value);
    }

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public bool HasSavedOwner
    {
        get => (bool)GetValue(HasSavedOwnerProperty);
        set => SetValue(HasSavedOwnerProperty, value);
    }

    public bool CanManage
    {
        get => (bool)GetValue(CanManageProperty);
        set => SetValue(CanManageProperty, value);
    }

    public bool CanDelete
    {
        get => (bool)GetValue(CanDeleteProperty);
        set => SetValue(CanDeleteProperty, value);
    }

    public bool ShowHeader
    {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public double MaxGridHeight
    {
        get => (double)GetValue(MaxGridHeightProperty);
        set => SetValue(MaxGridHeightProperty, value);
    }

    public ICommand? AddCommand
    {
        get => (ICommand?)GetValue(AddCommandProperty);
        set => SetValue(AddCommandProperty, value);
    }

    public ICommand? OpenCommand
    {
        get => (ICommand?)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public AttachmentsPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateAddEnabled();
    }

    private static void OnAddEnablementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AttachmentsPanel panel)
            panel.UpdateAddEnabled();
    }

    private void UpdateAddEnabled()
    {
        if (AddButton is not null)
            AddButton.IsEnabled = HasSavedOwner && CanManage;
    }
}
