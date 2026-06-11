using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public abstract partial class ReportViewModelBase : ViewModelBase
{
    protected readonly IReportService _reportService;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IExportService _exportService;
    protected readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalRecords;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private string _paginationText = string.Empty;

    public ObservableCollection<PaymentMethodItem> PaymentMethods { get; } =
    [
        new(null,                      "الكل"),
        new(PaymentMethod.Cash,        "نقدي"),
        new(PaymentMethod.Credit,      "آجل"),
        new(PaymentMethod.Installment, "أقساط"),
    ];

    protected ReportViewModelBase(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
    }

    protected void UpdatePagination<T>(IList<T> allItems, ObservableCollection<T> displayItems)
    {
        TotalRecords = allItems.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalRecords / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        displayItems.Clear();
        var paged = allItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
        foreach (var item in paged) displayItems.Add(item);

        var start = TotalRecords == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        var end = Math.Min(CurrentPage * PageSize, TotalRecords);
        PaginationText = $"عرض {start}-{end} من {TotalRecords}";
    }

    protected void UpdatePaginationWithFilters<T>(IList<T> allRows, ObservableCollection<T> displayRows)
    {
        var filtered = ColumnFilterEngine.Apply(allRows, ColumnFilters);
        UpdatePagination(filtered, displayRows);
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        OnPageChanged();
    }

    [RelayCommand]
    protected void FirstPage() { CurrentPage = 1; OnPageChanged(); }

    [RelayCommand]
    protected void PreviousPage() { if (CurrentPage > 1) { CurrentPage--; OnPageChanged(); } }

    [RelayCommand]
    protected void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; OnPageChanged(); } }

    [RelayCommand]
    protected void LastPage() { CurrentPage = TotalPages; OnPageChanged(); }

    protected virtual void OnPageChanged() { }

    protected static string FormatCurrency(decimal value)
        => $"{value:N0} د.ع";

    /// <summary>Re-runs chart data load when the user toggles dark/light theme.</summary>
    protected void RegisterThemeChartReload(Func<Task> reload)
        => ThemeChartRefresh.Register(reload);
}

public record PaymentMethodItem(PaymentMethod? Value, string Label)
{
    public override string ToString() => Label;
}
