using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldPurchasesReportViewModel : GoldReportViewModelBase
{
    private List<GoldInvoiceListItem> _allRows = [];

    public ObservableCollection<GoldInvoiceListItem> Rows { get; } = [];

    public IReadOnlyList<GoldStatusFilterOption> StatusFilters { get; } =
    [
        new(null, "الكل"),
        new(GoldInvoiceStatus.Completed, "مكتمل"),
        new(GoldInvoiceStatus.Open, "مفتوح"),
        new(GoldInvoiceStatus.PartiallyPaid, "جزئي"),
        new(GoldInvoiceStatus.Cancelled, "ملغى")
    ];

    [ObservableProperty] private GoldInvoiceStatus? _statusFilter;
    [ObservableProperty] private string _purchaseCount = "0";
    [ObservableProperty] private string _totalIqd = "0";
    [ObservableProperty] private string _totalUsd = "0";
    [ObservableProperty] private string _totalWeight = "0";

    public GoldPurchasesReportViewModel(
        IGoldReportService reportService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        PageTitle = "تقرير المشتريات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.PurchasesReport);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var summary = await ReportService.GetSummaryAsync(DateFrom, DateTo);
            _allRows = (await ReportService.GetPurchasesReportAsync(DateFrom, DateTo, StatusFilter)).ToList();
            PurchaseCount = summary.PurchaseCount.ToString("N0");
            TotalIqd = FormatCurrency(summary.TotalPurchasesIqd);
            TotalUsd = $"{summary.TotalPurchasesUsd:N2} $";
            TotalWeight = summary.TotalWeightPurchasedGrams.ToString("N3");
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
        var cols = new[] { "رقم الفاتورة", "التاريخ", "الزبون", "الدفع", "الحالة", "الوزن", "الإجمالي", "د.ع", "$", "المتبقي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName ?? "—",
            r.PaymentMethod.ToString(), r.Status.ToString(), r.TotalWeightGrams,
            r.TotalAmount, r.TotalAmountIqd, r.TotalAmountUsd, r.RemainingAmount
        }).ToList();
        ExportTable("مشتريات_الذهب.xlsx", "تقرير المشتريات", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "الزبون", "الوزن", "الإجمالي", "المتبقي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName ?? "—",
            r.TotalWeightGrams, r.TotalAmount, r.RemainingAmount
        }).ToList();
        PrintTable("تقرير مشتريات الذهب", cols, rows);
    }
}
