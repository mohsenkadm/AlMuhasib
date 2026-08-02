using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Loyalty;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoyaltyTopCustomersReportViewModel : ReportViewModelBase
{
    private readonly ILoyaltyService _loyaltyService;
    private List<LoyaltyTopCustomerRow> _allRows = [];

    public ObservableCollection<LoyaltyTopCustomerRow> Rows { get; } = [];

    public LoyaltyTopCustomersReportViewModel(
        ILoyaltyService loyaltyService,
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _loyaltyService = loyaltyService;
        PageTitle = "أكثر الزبائن ولاءً";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "LoyaltyReports");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            _allRows = (await _loyaltyService.GetTopCustomersAsync(DateFrom, DateTo)).ToList();
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "اكثر_الزبائن_ولاء.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "الزبون", "الهاتف", "الرصيد", "مكتسب", "مستبدل", "المستوى" };
        var rows = _allRows.Select(r => new object[]
        {
            r.CustomerName, r.Phone ?? "", r.PointsBalance, r.LifetimeEarned, r.LifetimeRedeemed, r.TierName
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "أكثر الزبائن ولاءً", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }
}
