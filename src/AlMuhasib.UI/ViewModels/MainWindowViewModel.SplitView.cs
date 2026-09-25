using System.Collections.ObjectModel;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

/// <summary>Target screen for split-pane open (from open tabs or main menu).</summary>
public sealed record SplitTargetItem(
    string Title,
    PackIconKind Icon,
    Type ViewModelType,
    string? PermissionScreenName,
    bool IsOpenTab);

public partial class MainWindowViewModel
{
    private bool _suppressSplitSync;

    [ObservableProperty]
    private bool _isSplitViewActive;

    [ObservableProperty]
    private ViewModelBase? _secondaryViewModel;

    [ObservableProperty]
    private DocumentTab? _secondaryTab;

    /// <summary>Tab that was right-clicked when the chrome context menu opened.</summary>
    [ObservableProperty]
    private DocumentTab? _tabContextMenuTarget;

    /// <summary>Raised when split layout columns must be reset (GridSplitter breaks Width bindings).</summary>
    public event Action<bool>? SplitLayoutChanged;

    public ObservableCollection<SplitTargetItem> SplitScreenTargets { get; } = [];

    public void PrepareTabContextMenu(DocumentTab? tab)
    {
        TabContextMenuTarget = tab;
        RefreshSplitScreenTargets();
        OnPropertyChanged(nameof(IsSplitViewActive));
    }

    public void RefreshSplitScreenTargets()
    {
        SplitScreenTargets.Clear();
        var anchor = TabContextMenuTarget ?? SelectedTab;
        var seen = new HashSet<Type>();

        foreach (var open in OpenTabs)
        {
            if (anchor is not null && open.Id == anchor.Id)
                continue;
            if (!seen.Add(open.ViewModelType))
                continue;

            SplitScreenTargets.Add(new SplitTargetItem(
                open.Title,
                open.Icon,
                open.ViewModelType,
                open.PermissionScreenName,
                IsOpenTab: true));
        }

        foreach (var item in FlattenMenuItems())
        {
            if (item.ViewModelType is null
                || item.IsGroupHeader
                || item.IsMenuSectionLabel
                || item.IsReportCategory
                || !item.IsVisible
                || string.IsNullOrWhiteSpace(item.Title))
                continue;

            if (anchor?.ViewModelType == item.ViewModelType)
                continue;
            if (!seen.Add(item.ViewModelType))
                continue;

            SplitScreenTargets.Add(new SplitTargetItem(
                item.Title,
                item.Icon,
                item.ViewModelType,
                string.IsNullOrWhiteSpace(item.ScreenName) ? null : item.ScreenName,
                IsOpenTab: false));
        }
    }

    [RelayCommand]
    private async Task SplitWithScreenAsync(SplitTargetItem? target)
    {
        if (target?.ViewModelType is null)
            return;

        var primary = TabContextMenuTarget ?? SelectedTab;
        if (primary is null)
            return;

        ActivateTab(primary);

        var secondary = OpenTabs.FirstOrDefault(t =>
            t.ViewModelType == target.ViewModelType && t.Id != primary.Id);

        if (secondary is null)
        {
            await OpenTabAsync(
                target.ViewModelType,
                target.Title,
                target.Icon,
                activateIfExists: false,
                target.PermissionScreenName);

            secondary = OpenTabs.LastOrDefault(t =>
                t.ViewModelType == target.ViewModelType && t.Id != primary.Id);
        }

        if (secondary is null)
        {
            _toast.ShowWarning("تعذّر فتح الواجهة في النصف الآخر.");
            return;
        }

        ActivateTab(primary);
        EnterSplitView(secondary);
    }

    /// <summary>Show the right-clicked tab in the other pane (or duplicate if it is already active).</summary>
    [RelayCommand]
    private async Task ShowTabInOtherPaneAsync(DocumentTab? tab)
    {
        tab ??= TabContextMenuTarget;
        if (tab is null)
            return;

        var primary = SelectedTab;
        if (primary is null || tab.Id == primary.Id)
        {
            await SplitThisScreenAsync(tab);
            return;
        }

        // Keep the currently selected tab as primary; put the clicked tab on the right.
        EnterSplitView(tab);
    }

    /// <summary>Duplicate this screen and show both halves side by side.</summary>
    [RelayCommand]
    private async Task SplitThisScreenAsync(DocumentTab? tab)
    {
        tab ??= TabContextMenuTarget ?? SelectedTab;
        if (tab is null)
            return;

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لتقسيم هذه الشاشة.");
            return;
        }

        ActivateTab(tab);

        await OpenTabAsync(
            tab.ViewModelType,
            tab.Title,
            tab.Icon,
            activateIfExists: false,
            tab.PermissionScreenName);

        var secondary = OpenTabs.LastOrDefault(t =>
            t.ViewModelType == tab.ViewModelType && t.Id != tab.Id);

        if (secondary is null)
        {
            _toast.ShowWarning("تعذّر تقسيم هذه الشاشة إلى نصفين.");
            return;
        }

        ActivateTab(tab);
        EnterSplitView(secondary);
    }

    [RelayCommand]
    private void ExitSplitView()
    {
        if (SecondaryTab is not null)
            SecondaryTab.IsInSecondaryPane = false;

        SecondaryViewModel = null;
        SecondaryTab = null;
        IsSplitViewActive = false;
        ApplySplitLayout(false);

        if (SelectedTab is not null)
            PageTitle = SelectedTab.Title;
    }

    [RelayCommand]
    private void SwapSplitPanes()
    {
        if (!IsSplitViewActive || SelectedTab is null || SecondaryTab is null)
            return;

        var left = SelectedTab;
        var right = SecondaryTab;

        _suppressSplitSync = true;
        try
        {
            // Detach both panes first — a ViewModel cannot live in two ContentControls.
            SecondaryViewModel = null;
            left.IsInSecondaryPane = false;
            right.IsInSecondaryPane = false;

            ActivateTab(right);

            SecondaryTab = left;
            left.IsInSecondaryPane = true;
            SecondaryViewModel = left.ViewModel;
            IsSplitViewActive = true;
            ApplySplitLayout(true);
            PageTitle = $"{right.Title} | {left.Title}";
        }
        finally
        {
            _suppressSplitSync = false;
        }
    }

    private void EnterSplitView(DocumentTab secondary)
    {
        if (SelectedTab is not null && secondary.Id == SelectedTab.Id)
            return;

        if (SecondaryTab is not null && SecondaryTab.Id != secondary.Id)
            SecondaryTab.IsInSecondaryPane = false;

        // Clear first to avoid visual-tree conflict if secondary was previously primary.
        if (ReferenceEquals(CurrentViewModel, secondary.ViewModel))
            return;

        SecondaryTab = secondary;
        secondary.IsInSecondaryPane = true;
        SecondaryViewModel = secondary.ViewModel;
        IsSplitViewActive = true;
        ApplySplitLayout(true);
        PageTitle = SelectedTab is null
            ? secondary.Title
            : $"{SelectedTab.Title} | {secondary.Title}";
    }

    private void ApplySplitLayout(bool split) => SplitLayoutChanged?.Invoke(split);

    /// <summary>
    /// Called from <see cref="ApplyActiveTabState"/> after the primary pane view model changes.
    /// </summary>
    private void SyncSplitAfterTabChange(DocumentTab active, ViewModelBase? previousPrimaryVm)
    {
        if (_suppressSplitSync || !IsSplitViewActive)
            return;

        if (SecondaryTab is null || !OpenTabs.Contains(SecondaryTab))
        {
            ExitSplitView();
            return;
        }

        // User selected the tab that was in the secondary pane → swap instead of collapsing.
        if (SecondaryTab.Id == active.Id)
        {
            var newSecondary = OpenTabs.FirstOrDefault(t =>
                previousPrimaryVm is not null
                && ReferenceEquals(t.ViewModel, previousPrimaryVm)
                && t.Id != active.Id);

            if (newSecondary is null)
            {
                ExitSplitView();
                return;
            }

            _suppressSplitSync = true;
            try
            {
                SecondaryViewModel = null;
                SecondaryTab.IsInSecondaryPane = false;

                SecondaryTab = newSecondary;
                newSecondary.IsInSecondaryPane = true;
                SecondaryViewModel = newSecondary.ViewModel;
                PageTitle = $"{active.Title} | {newSecondary.Title}";
                ApplySplitLayout(true);
            }
            finally
            {
                _suppressSplitSync = false;
            }

            return;
        }

        SecondaryViewModel = SecondaryTab.ViewModel;
        PageTitle = $"{active.Title} | {SecondaryTab.Title}";
    }

    private void SyncSplitAfterTabClosed(DocumentTab closed)
    {
        if (!IsSplitViewActive)
            return;

        if (SecondaryTab?.Id == closed.Id)
            ExitSplitView();
    }
}
