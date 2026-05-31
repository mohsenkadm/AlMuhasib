using MaterialDesignThemes.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class MenuVisibilityOption : ObservableObject
{
    public required NavigationMenuItem MenuItem { get; init; }
    public required string PreferenceKey { get; init; }
    public required string Title { get; init; }
    public required PackIconKind Icon { get; init; }

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isPinned;
}
