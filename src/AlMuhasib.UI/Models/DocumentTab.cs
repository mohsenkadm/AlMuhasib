using System.Windows;
using System.Windows.Controls;
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

    /// <summary>True when this tab is shown in the secondary split pane.</summary>
    [ObservableProperty]
    private bool _isInSecondaryPane;

    public Type ViewModelType { get; init; } = null!;
    public ViewModelBase ViewModel { get; init; } = null!;
    public IServiceScope Scope { get; init; } = null!;

    /// <summary>Cached visual for this tab (null when trimmed or never shown).</summary>
    public FrameworkElement? CachedView { get; set; }

    /// <summary>
    /// True while the tab is moving between windows. Collection removal must keep the view and DI scope.
    /// </summary>
    public bool IsTransferring { get; set; }

    /// <summary>Header-only flag while the user drags this tab.</summary>
    [ObservableProperty]
    private bool _isBeingDragged;

    /// <summary>اسم شاشة الصلاحية إن وُجد — يُستخدم لإعادة الفتح.</summary>
    public string? PermissionScreenName { get; init; }

    /// <summary>Unhooks the cached visual from its current parent without dropping the instance.</summary>
    public void DetachVisualFromParent()
    {
        if (CachedView is null)
            return;

        switch (CachedView.Parent)
        {
            case ContentControl host:
                host.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, CachedView):
                decorator.Child = null;
                break;
            case Panel panel:
                panel.Children.Remove(CachedView);
                break;
        }
    }

    public void ReleaseCachedView()
    {
        if (CachedView is null)
            return;

        DetachVisualFromParent();
        CachedView = null;
    }

    public void Dispose()
    {
        ReleaseCachedView();
        Scope.Dispose();
    }
}

/// <summary>سجل خفيف لإعادة فتح تبويب مغلق (بدون حالة النموذج).</summary>
public sealed record ClosedTabInfo(Type ViewModelType, string Title, PackIconKind Icon, string? PermissionScreenName);
