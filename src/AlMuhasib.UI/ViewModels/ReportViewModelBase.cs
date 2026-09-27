using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public sealed class ReportCurrencyScopeOption
{
    public ReportCurrencyScope Scope { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    public static IReadOnlyList<ReportCurrencyScopeOption> All { get; } =
    [
        new() { Scope = ReportCurrencyScope.Iqd, DisplayName = "دينار" },
        new() { Scope = ReportCurrencyScope.Usd, DisplayName = "دولار" },
        new() { Scope = ReportCurrencyScope.All, DisplayName = "الكل (منفصل)" },
    ];
}

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
    [ObservableProperty] private ReportCurrencyScopeOption? _selectedCurrencyScopeOption;

    public ObservableCollection<PaymentMethodItem> PaymentMethods { get; } =
    [
        new(null,                      "الكل"),
        new(PaymentMethod.Cash,        "نقدي"),
        new(PaymentMethod.Credit,      "آجل"),
        new(PaymentMethod.Installment, "أقساط"),
    ];

    public ObservableCollection<ReportCurrencyScopeOption> CurrencyScopeOptions { get; } = new(ReportCurrencyScopeOption.All);

    /// <summary>نطاق عملة التقرير المحاسبي — افتراضي دينار للتوافق.</summary>
    protected ReportCurrencyScope CurrencyScope =>
        SelectedCurrencyScopeOption?.Scope ?? ReportCurrencyScope.Iqd;

    protected ReportViewModelBase(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        SelectedCurrencyScopeOption = CurrencyScopeOptions.FirstOrDefault(o => o.Scope == ReportCurrencyScope.Iqd);
    }

    protected void UpdatePagination<T>(IList<T> allItems, ObservableCollection<T> displayItems)
    {
        PaginationHelper.ApplyPage(allItems, displayItems, CurrentPage, PageSize,
            out var totalRecords, out var totalPages, out var paginationText);

        TotalRecords = totalRecords;
        TotalPages = totalPages;
        PaginationText = paginationText;

        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;
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
        => FormatCurrency(value, AccountingCurrency.IQD);

    protected static string FormatCurrency(decimal value, AccountingCurrency currency)
        => AccountingCurrencyHelper.Format(value, currency);

    /// <summary>توحيد مبلغ إلى الدينار (العملة الأساسية) للتقارير الإجمالية.</summary>
    protected static decimal ToBaseIqd(decimal value, AccountingCurrency currency, decimal fxRate)
        => AccountingCurrencyHelper.ToBaseIqd(value, currency, fxRate);

    /// <summary>Re-runs chart data load when the user toggles dark/light theme.</summary>
    protected void RegisterThemeChartReload(Func<Task> reload)
        => ThemeChartRefresh.Register(reload);
}

public record PaymentMethodItem(PaymentMethod? Value, string Label)
{
    public override string ToString() => Label;
}
