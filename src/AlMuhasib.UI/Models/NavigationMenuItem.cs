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

    public ObservableCollection<NavigationMenuItem> Children { get; } = [];
}
