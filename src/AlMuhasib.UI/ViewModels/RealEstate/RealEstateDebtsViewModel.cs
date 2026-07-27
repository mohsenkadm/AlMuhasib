using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateDebtsViewModel : ViewModelBase
{
    private readonly IRealEstateContractService _contractService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IUserPreferencesService _prefs;

    public ObservableCollection<RealEstateDebtItem> Debts { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _overdueOnly;
    [ObservableProperty] private decimal _totalDebt;
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private string _totalDebtText = "0";
    [ObservableProperty] private string _debtorsCountText = "0";
    [ObservableProperty] private string _overdueCountText = "0";
    [ObservableProperty] private string _overdueAmountText = "0";
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private ISeries[] _statusSeries = [];

    public RealEstateDebtsViewModel(
        IRealEstateContractService contractService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IUserPreferencesService prefs)
    {
        _contractService = contractService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        _prefs = prefs;
        PageTitle = "كشف المدينين";
        IsCardView = ListViewModeHelper.LoadIsCardView(_prefs, "RealEstateDebts");
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.Debts);
        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();
    partial void OnOverdueOnlyChanged(bool value) => _ = LoadAsync();
    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_prefs, "RealEstateDebts", value);

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _contractService.GetDebtsAsync(OverdueOnly);
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                items = items.Where(i =>
                    i.DebtorName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.ContractNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.DebtorPhone.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
                items = ColumnFilterEngine.Apply(items, ColumnFilters).ToList();

            Debts.Clear();
            foreach (var item in items)
                Debts.Add(item);

            TotalDebt = items.Sum(i => i.RemainingAmount);
            OverdueCount = items.Count(i => i.IsOverdue);
            TotalDebtText = TotalDebt.ToString("N0");
            DebtorsCountText = items.Count.ToString("N0");
            OverdueCountText = OverdueCount.ToString("N0");
            OverdueAmountText = items.Where(i => i.IsOverdue).Sum(i => i.RemainingAmount).ToString("N0");

            var current = items.Count(i => !i.IsOverdue);
            StatusSeries =
            [
                ChartThemeConfig.Pie(current, "حالية", 0),
                ChartThemeConfig.Pie(OverdueCount, "متأخرة", 1)
            ];
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnColumnFiltersChanged() => _ = LoadAsync();

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport) return;
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"RealEstateDebts_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;
        var headers = new[] { "العقد", "المدين", "الهاتف", "الطرف", "المتبقي", "الاستحقاق", "متأخر" };
        var data = Debts.Select(d => new object?[]
        {
            d.ContractNumber, d.DebtorName, d.DebtorPhone, d.DebtorParty,
            d.RemainingAmount, d.DueDate?.ToString("yyyy/MM/dd"), d.IsOverdue ? "نعم" : "لا"
        }).ToList();
        _exportService.ExportToExcel(dialog.FileName, "المدينون", headers, data);
        _toast.ShowSuccess("تم التصدير");
    }

    [RelayCommand]
    private void PrintTable()
    {
        if (!CanPrint) return;
        var headers = new[] { "العقد", "المدين", "المتبقي", "الاستحقاق" };
        var data = Debts.Select(d => new object?[]
        {
            d.ContractNumber, d.DebtorName, d.RemainingAmount, d.DueDate?.ToString("yyyy/MM/dd")
        }).ToList();
        _exportService.PrintTable("كشف مديني عقود العقارات", headers, data);
    }
}
