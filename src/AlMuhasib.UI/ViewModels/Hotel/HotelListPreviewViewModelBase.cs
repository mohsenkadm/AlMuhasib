using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels.Hotel;

/// <summary>
/// قاعدة مشتركة لشاشات قوائم الفندق مع لوحة معاينة جانبية.
/// </summary>
public abstract partial class HotelListPreviewViewModelBase : PagedViewModelBase
{
    [ObservableProperty] private bool _isPreviewOpen;
    [ObservableProperty] private int _previewSelectedTab;
    [ObservableProperty] private string _previewTitle = string.Empty;
    [ObservableProperty] private string _previewSubtitle = string.Empty;
    [ObservableProperty] private PackIconKind _previewIconKind = PackIconKind.InformationOutline;

    public bool HasPreviewSelection => IsPreviewOpen;

    partial void OnIsPreviewOpenChanged(bool value) => OnPropertyChanged(nameof(HasPreviewSelection));

    [RelayCommand]
    protected void ClosePreview()
    {
        IsPreviewOpen = false;
        PreviewTitle = string.Empty;
        PreviewSubtitle = string.Empty;
        OnPreviewClosed();
    }

    [RelayCommand]
    protected void OpenPreviewHistoryTab() => PreviewSelectedTab = 1;

    protected virtual void OnPreviewClosed()
    {
    }

    protected void SetPreviewHeader(string title, string subtitle, PackIconKind iconKind)
    {
        PreviewTitle = title;
        PreviewSubtitle = subtitle;
        PreviewIconKind = iconKind;
        IsPreviewOpen = true;
        PreviewSelectedTab = 0;
    }
}
