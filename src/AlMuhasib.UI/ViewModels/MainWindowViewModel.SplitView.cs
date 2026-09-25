using System.Collections.ObjectModel;
using System.Windows;
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
    [ObservableProperty]
    private bool _isSplitViewActive;

    [ObservableProperty]
    private ViewModelBase? _secondaryViewModel;

    [ObservableProperty]
    private DocumentTab? _secondaryTab;

    /// <summary>Tab that was right-clicked when the chrome context menu opened.</summary>
    [ObservableProperty]
    private DocumentTab? _tabContextMenuTarget;

    [ObservableProperty]
    private GridLength _primaryPaneWidth = new(1, GridUnitType.Star);

    [ObservableProperty]
    private GridLength _splitSplitterWidth = new(0);

    [ObservableProperty]
    private GridLength _secondaryPaneWidth = new(0);

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

        // Prefer an already-open tab that is not the primary pane.
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

            secondary = OpenTabs.LastOrDefault(t => t.ViewModelType == target.ViewModelType);
        }

        if (secondary is null || secondary.Id == primary.Id)
        {
            _toast.ShowWarning("تعذّر فتح الواجهة في النصف الآخر.");
            return;
        }

        // OpenTabAsync activates the new tab — restore primary as the focused pane.
        ActivateTab(primary);
        EnterSplitView(secondary);
    }

    [RelayCommand]
    private void ShowTabInOtherPane(DocumentTab? tab)
    {
        tab ??= TabContextMenuTarget;
        var primary = SelectedTab;
        if (tab is null || primary is null || tab.Id == primary.Id)
            return;

        EnterSplitView(tab);
    }

    [RelayCommand]
    private void ExitSplitView()
    {
        IsSplitViewActive = false;
        SecondaryViewModel = null;
        if (SecondaryTab is not null)
            SecondaryTab.IsInSecondaryPane = false;
        SecondaryTab = null;
        ApplySplitLayout(false);
    }

    [RelayCommand]
    private void SwapSplitPanes()
    {
        if (!IsSplitViewActive || SelectedTab is null || SecondaryTab is null)
            return;

        var previousSecondary = SecondaryTab;
        EnterSplitView(SelectedTab);
        ActivateTab(previousSecondary);
    }

    private void EnterSplitView(DocumentTab secondary)
    {
        if (SelectedTab is not null && secondary.Id == SelectedTab.Id)
            return;

        if (SecondaryTab is not null)
            SecondaryTab.IsInSecondaryPane = false;

        SecondaryTab = secondary;
        secondary.IsInSecondaryPane = true;
        SecondaryViewModel = secondary.ViewModel;
        IsSplitViewActive = true;
        ApplySplitLayout(true);
        PageTitle = SelectedTab is null
            ? secondary.Title
            : $"{SelectedTab.Title} | {secondary.Title}";
    }

    private void ApplySplitLayout(bool split)
    {
        if (split)
        {
            PrimaryPaneWidth = new GridLength(1, GridUnitType.Star);
            SplitSplitterWidth = new GridLength(6);
            SecondaryPaneWidth = new GridLength(1, GridUnitType.Star);
        }
        else
        {
            PrimaryPaneWidth = new GridLength(1, GridUnitType.Star);
            SplitSplitterWidth = new GridLength(0);
            SecondaryPaneWidth = new GridLength(0);
            if (SelectedTab is not null)
                PageTitle = SelectedTab.Title;
        }
    }

    private void SyncSplitAfterTabChange(DocumentTab active)
    {
        if (!IsSplitViewActive)
            return;

        if (SecondaryTab is null || !OpenTabs.Contains(SecondaryTab))
        {
            ExitSplitView();
            return;
        }

        // Same tab cannot host in both panes.
        if (SecondaryTab.Id == active.Id)
        {
            ExitSplitView();
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
