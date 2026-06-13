using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public abstract partial class PagedViewModelBase : ViewModelBase
{
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _paginationText = string.Empty;

    protected void ApplyPaginationStats(int totalCount, int? currentPage = null)
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

    protected virtual Task OnPageChangedAsync() => Task.CompletedTask;

    [RelayCommand]
    protected async Task FirstPageAsync()
    {
        if (CurrentPage == 1)
            return;

        CurrentPage = 1;
        await OnPageChangedAsync();
    }

    [RelayCommand]
    protected async Task PreviousPageAsync()
    {
        if (CurrentPage <= 1)
            return;

        CurrentPage--;
        await OnPageChangedAsync();
    }

    [RelayCommand]
    protected async Task NextPageAsync()
    {
        if (CurrentPage >= TotalPages)
            return;

        CurrentPage++;
        await OnPageChangedAsync();
    }

    [RelayCommand]
    protected async Task LastPageAsync()
    {
        if (CurrentPage == TotalPages)
            return;

        CurrentPage = TotalPages;
        await OnPageChangedAsync();
    }
}
