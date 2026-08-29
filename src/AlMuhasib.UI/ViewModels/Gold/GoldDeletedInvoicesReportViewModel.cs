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

public partial class GoldDeletedInvoicesReportViewModel : GoldReportViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IGoldPurchaseService _purchaseService;
    private List<GoldDeletedInvoiceRow> _allRows = [];

    public ObservableCollection<GoldDeletedInvoiceRow> Rows { get; } = [];

    [ObservableProperty] private string _deletedCount = "0";
    [ObservableProperty] private string _totalAmount = "0";
    [ObservableProperty] private string _userCount = "0";
    [ObservableProperty] private string _typeCount = "0";

    public GoldDeletedInvoicesReportViewModel(
        IGoldReportService reportService,
        IGoldSaleService saleService,
        IGoldPurchaseService purchaseService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        _saleService = saleService;
        _purchaseService = purchaseService;
        PageTitle = "الفواتير المحذوفة";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.DeletedInvoicesReport);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _allRows = (await ReportService.GetDeletedInvoicesReportAsync(DateFrom, DateTo)).ToList();
            DeletedCount = _allRows.Count.ToString("N0");
            TotalAmount = FormatCurrency(_allRows.Sum(r => r.TotalAmount));
            UserCount = _allRows.Select(r => r.DeletedBy).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString("N0");
            TypeCount = _allRows.Select(r => r.InvoiceType).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString("N0");
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
        var cols = new[] { "رقم الفاتورة", "التاريخ", "النوع", "الطرف", "الإجمالي", "تاريخ الحذف", "حُذف بواسطة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber,
            r.InvoiceDate.ToString("yyyy/MM/dd"),
            r.InvoiceType,
            r.PartyName,
            r.TotalAmount,
            r.DeletedAt?.ToString("yyyy/MM/dd HH:mm") ?? "—",
            r.DeletedBy
        }).ToList();
        ExportTable("فواتير_محذوفة_ذهب.xlsx", "الفواتير المحذوفة", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "النوع", "الطرف", "الإجمالي", "حُذف بواسطة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber,
            r.InvoiceDate.ToString("yyyy/MM/dd"),
            r.InvoiceType,
            r.PartyName,
            r.TotalAmount,
            r.DeletedBy
        }).ToList();
        PrintTable("الفواتير المحذوفة — الذهب", cols, rows);
    }

    [RelayCommand]
    private async Task OpenInvoiceDetail(GoldDeletedInvoiceRow? row)
    {
        if (row is null)
            return;

        try
        {
            var invoiceType = ParseInvoiceType(row.InvoiceType);
            await GoldInvoiceDetailDialog.ShowAsync(row.Id, invoiceType, _saleService, _purchaseService);
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
    }

    private static GoldInvoiceType ParseInvoiceType(string label) => label switch
    {
        "شراء" => GoldInvoiceType.Purchase,
        "تبديل" => GoldInvoiceType.Exchange,
        "مرتجع بيع" => GoldInvoiceType.SaleReturn,
        _ => GoldInvoiceType.Sale
    };
}
