using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Models;

public partial class NavigationMenuItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private PackIconKind _icon;

    [ObservableProperty]
    private Type? _viewModelType;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private string _screenName = string.Empty;

    [ObservableProperty]
    private bool _isGroupHeader;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSubItem;

    /// <summary>عنوان قسم غير قابل للنقر (مثل «التقارير»).</summary>
    [ObservableProperty]
    private bool _isMenuSectionLabel;

    /// <summary>فئة تقارير تفتح لوحة جانبية بدل قائمة فرعية.</summary>
    [ObservableProperty]
    private bool _isReportCategory;

    [ObservableProperty]
    private string _categoryKey = string.Empty;

    [ObservableProperty]
    private string _categoryAccentColor = "#1565C0";

    [ObservableProperty]
    private string _categoryAccentLightColor = "#E3F2FD";

    public ObservableCollection<NavigationMenuItem> Children { get; } = [];
}
