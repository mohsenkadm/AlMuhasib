using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class ToastNotification : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private ToastDisplayState _state = ToastDisplayState.Loading;

    public bool IsLoading => State == ToastDisplayState.Loading;

    partial void OnStateChanged(ToastDisplayState value) => OnPropertyChanged(nameof(IsLoading));

    /// <summary>1 = full bar, 0 = empty (auto-dismiss progress).</summary>
    [ObservableProperty]
    private double _dismissProgress = 1;

    [ObservableProperty]
    private bool _isExiting;

    [ObservableProperty]
    private bool _isEntering = true;
}
