using AlMuhasib.UI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Models;

/// <summary>One open screen tab (own DI scope + view model).</summary>
public partial class DocumentTab : ObservableObject, IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private PackIconKind _icon;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _canClose = true;

    [ObservableProperty]
    private bool _isPinned;

    public Type ViewModelType { get; init; } = null!;
    public ViewModelBase ViewModel { get; init; } = null!;
    public IServiceScope Scope { get; init; } = null!;

    public void Dispose() => Scope.Dispose();
}
