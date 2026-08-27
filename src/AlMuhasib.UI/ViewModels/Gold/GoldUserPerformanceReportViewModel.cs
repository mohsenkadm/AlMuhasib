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

public partial class GoldUserPerformanceReportViewModel : GoldReportViewModelBase
{
    private List<GoldUserPerformanceRow> _allRows = [];

    public ObservableCollection<GoldUserPerformanceRow> Rows { get; } = [];

    [ObservableProperty] private string _userNameFilter = string.Empty;
    [ObservableProperty] private string _userCount = "0";
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _totalPurchases = "0";
    [ObservableProperty] private string _totalActions = "0";

    public GoldUserPerformanceReportViewModel(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        PageTitle = "أداء المستخدمين";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.UserPerformanceReport);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var user = string.IsNullOrWhiteSpace(UserNameFilter) ? null : UserNameFilter.Trim();
            _allRows = (await ReportService.GetUserPerformanceReportAsync(DateFrom, DateTo, user)).ToList();
            UserCount = _allRows.Count.ToString("N0");
            TotalSales = FormatCurrency(_allRows.Sum(r => r.SalesAmountIqd));
            TotalPurchases = FormatCurrency(_allRows.Sum(r => r.PurchasesAmountIqd));
            TotalActions = _allRows.Sum(r => r.AuditActionsCount).ToString("N0");
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
        var cols = new[]
        {
            "المستخدم", "مبيعات", "مشتريات", "تبديل", "دفعات", "إجراءات",
            "مبلغ مبيعات د.ع", "مبلغ مشتريات د.ع"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.UserName, r.SalesCount, r.PurchasesCount, r.ExchangeCount, r.PaymentsCount,
            r.AuditActionsCount, r.SalesAmountIqd, r.PurchasesAmountIqd
        }).ToList();
        ExportTable("أداء_المستخدمين.xlsx", "أداء المستخدمين", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[]
        {
            "المستخدم", "مبيعات", "مشتريات", "تبديل", "دفعات", "إجراءات",
            "مبلغ مبيعات د.ع", "مبلغ مشتريات د.ع"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.UserName, r.SalesCount, r.PurchasesCount, r.ExchangeCount, r.PaymentsCount,
            r.AuditActionsCount, r.SalesAmountIqd, r.PurchasesAmountIqd
        }).ToList();
        PrintTable("أداء مستخدمي الذهب", cols, rows);
    }
}
