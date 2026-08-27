using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldProfitabilityReportViewModel : GoldReportViewModelBase
{
    private List<GoldProfitabilityRow> _allRows = [];

    public ObservableCollection<GoldProfitabilityRow> Rows { get; } = [];

    [ObservableProperty] private string _salesValue = "0";
    [ObservableProperty] private string _makingCharges = "0";
    [ObservableProperty] private string _estimatedCost = "0";
    [ObservableProperty] private string _grossProfit = "0";

    public GoldProfitabilityReportViewModel(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        PageTitle = "ربحية الذهب (التكلفة تقديرية حسب متوسط المخزون الحالي)";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.ProfitabilityReport);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _allRows = (await ReportService.GetProfitabilityReportAsync(DateFrom, DateTo)).ToList();
            SalesValue = FormatCurrency(_allRows.Sum(r => r.SalesGoldValue));
            MakingCharges = FormatCurrency(_allRows.Sum(r => r.MakingCharges));
            EstimatedCost = FormatCurrency(_allRows.Sum(r => r.EstimatedCost));
            GrossProfit = FormatCurrency(_allRows.Sum(r => r.GrossProfit));
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var cols = new[] { "العيار", "الوزن المباع", "قيمة الذهب", "أجور الصياغة", "التكلفة التقديرية", "إجمالي الربح", "العملة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.KaratName, r.WeightSoldGrams, r.SalesGoldValue, r.MakingCharges, r.EstimatedCost, r.GrossProfit, r.Currency.ToString()
        }).ToList();
        ExportTable("ربحية_الذهب.xlsx", "ربحية الذهب", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العيار", "الوزن", "المبيعات", "الأجور", "التكلفة", "الربح" };
        var rows = _allRows.Select(r => new object[]
        {
            r.KaratName, r.WeightSoldGrams, r.SalesGoldValue, r.MakingCharges, r.EstimatedCost, r.GrossProfit
        }).ToList();
        PrintTable("ربحية الذهب", cols, rows);
    }
}
