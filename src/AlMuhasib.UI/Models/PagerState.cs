using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.Models;

/// <summary>حالة باجنيشن قابلة للربط بـ <see cref="Controls.PaginationBar"/> (مثلاً تبويبات متعددة).</summary>
public partial class PagerState : ObservableObject
{
    private Func<Task>? _onPageChanged;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _paginationText = string.Empty;

    public void Bind(Func<Task> onPageChanged) => _onPageChanged = onPageChanged;

    public void ApplyStats(int totalCount, int? currentPage = null)
    {
        var page = currentPage ?? CurrentPage;
        PaginationHelper.ComputeStats(totalCount, page, PageSize, out var totalPages, out var text);
        TotalCount = totalCount;
        TotalPages = totalPages;
        PaginationText = text;

        if (CurrentPage > TotalPages)
            CurrentPage = Math.Max(1, TotalPages);
        if (CurrentPage < 1)
            CurrentPage = 1;
    }

    public void ResetToFirstPage() => CurrentPage = 1;

    [RelayCommand]
    private async Task FirstPageAsync()
    {
        if (CurrentPage == 1)
            return;

        CurrentPage = 1;
        await InvokePageChangedAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage <= 1)
            return;

        CurrentPage--;
        await InvokePageChangedAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage >= TotalPages)
            return;

        CurrentPage++;
        await InvokePageChangedAsync();
    }

    [RelayCommand]
    private async Task LastPageAsync()
    {
        if (CurrentPage == TotalPages)
            return;

        CurrentPage = TotalPages;
        await InvokePageChangedAsync();
    }

    private async Task InvokePageChangedAsync()
    {
        if (_onPageChanged is not null)
            await _onPageChanged();
    }
}
