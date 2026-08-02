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

public partial class GoldAgingReportViewModel : GoldReportViewModelBase
{
    private List<GoldAgingRow> _allRows = [];

    public ObservableCollection<GoldAgingRow> Rows { get; } = [];

    [ObservableProperty] private string _totalOutstanding = "0";
    [ObservableProperty] private string _rowCount = "0";
    [ObservableProperty] private string _customerCount = "0";
    [ObservableProperty] private string _over90Total = "0";

    public GoldAgingReportViewModel(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        PageTitle = "أعمار ذمم الذهب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.AgingReport);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _allRows = (await ReportService.GetAgingReportAsync()).ToList();
            TotalOutstanding = FormatCurrency(_allRows.Sum(r => r.TotalIqd));
            RowCount = _allRows.Sum(r => r.OpenInvoiceCount).ToString("N0");
            CustomerCount = _allRows.Count.ToString("N0");
            Over90Total = FormatCurrency(_allRows.Sum(r => r.Over90Iqd));
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
        var cols = new[] { "الزبون", "الهاتف", "جاري", "1-30", "31-60", "61-90", "+90", "الإجمالي د.ع", "الإجمالي $", "فواتير مفتوحة", "أقدم فاتورة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.CustomerName, r.Phone, r.CurrentIqd, r.Days1To30Iqd, r.Days31To60Iqd, r.Days61To90Iqd,
            r.Over90Iqd, r.TotalIqd, r.TotalUsd, r.OpenInvoiceCount,
            r.OldestOpenDate?.ToString("yyyy/MM/dd") ?? "—"
        }).ToList();
        ExportTable("أعمار_ذمم_الذهب.xlsx", "أعمار ذمم الذهب", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "الزبون", "جاري", "1-30", "31-60", "61-90", "+90", "الإجمالي", "فواتير" };
        var rows = _allRows.Select(r => new object[]
        {
            r.CustomerName, r.CurrentIqd, r.Days1To30Iqd, r.Days31To60Iqd, r.Days61To90Iqd,
            r.Over90Iqd, r.TotalIqd, r.OpenInvoiceCount
        }).ToList();
        PrintTable("أعمار ذمم الذهب", cols, rows);
    }
}
