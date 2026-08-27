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

public partial class GoldSupplierStatementViewModel : ViewModelBase
{
    private readonly IGoldSupplierService _supplierService;
    private readonly IExportService _exportService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldSupplierListItem> Suppliers { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> Invoices { get; } = [];

    [ObservableProperty] private string _supplierSearch = string.Empty;
    [ObservableProperty] private GoldSupplierListItem? _selectedSupplier;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private decimal _creditBalanceIqd;
    [ObservableProperty] private decimal _creditBalanceUsd;

    public GoldSupplierStatementViewModel(
        IGoldSupplierService supplierService,
        IExportService exportService,
        IWhatsAppShareService whatsAppShare,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _supplierService = supplierService;
        _exportService = exportService;
        _whatsAppShare = whatsAppShare;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "كشف حساب مورد";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.SupplierStatement);
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        IsBusy = true;
        try
        {
            Suppliers.Clear();
            var (items, _) = await _supplierService.GetPagedAsync(1, 500, SupplierSearch, activeOnly: null);
            foreach (var s in items)
                Suppliers.Add(s);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedSupplierChanged(GoldSupplierListItem? value) => _ = LoadStatementAsync();
    partial void OnSupplierSearchChanged(string value) => _ = LoadSuppliersAsync();

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        Invoices.Clear();
        TotalAmount = TotalPaid = TotalRemaining = 0;
        CreditBalanceIqd = CreditBalanceUsd = 0;

        if (SelectedSupplier is null)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var invoices = await _supplierService.GetSupplierInvoicesAsync(SelectedSupplier.Id);
            foreach (var inv in invoices)
                Invoices.Add(inv);

            TotalAmount = invoices.Sum(i => i.TotalAmount);
            TotalPaid = invoices.Sum(i => i.PaidAmount);
            TotalRemaining = invoices.Sum(i => i.RemainingAmount);
            CreditBalanceIqd = SelectedSupplier.CreditBalanceIqd;
            CreditBalanceUsd = SelectedSupplier.CreditBalanceUsd;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void PrintStatement()
    {
        if (SelectedSupplier is null || Invoices.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى تحميل كشف الحساب أولاً");
            return;
        }

        var columns = new[]
        {
            "رقم الفاتورة", "التاريخ", "النوع", "المورد", "الحالة",
            "الإجمالي", "المدفوع", "المتبقي"
        };
        IList<object[]> rows = Invoices.Select(i => new object[]
        {
            i.InvoiceNumber,
            i.InvoiceDate.ToString("yyyy/MM/dd"),
            i.InvoiceType.ToString(),
            i.SupplierName ?? SelectedSupplier.Name,
            i.Status.ToString(),
            i.TotalAmount.ToString("N0"),
            i.PaidAmount.ToString("N0"),
            i.RemainingAmount.ToString("N0")
        }).ToList();

        var summary = new List<string>
        {
            $"المورد: {SelectedSupplier.Name}",
            $"إجمالي الفواتير: {TotalAmount:N0}",
            $"المدفوع: {TotalPaid:N0}",
            $"المتبقي: {TotalRemaining:N0}",
            $"رصيد آجل د.ع: {CreditBalanceIqd:N0}",
            $"رصيد آجل $: {CreditBalanceUsd:N2}"
        };

        _exportService.PrintTable($"كشف حساب مورد — {SelectedSupplier.Name}", columns, rows, summary);
    }

    [RelayCommand]
    private void ShareWhatsApp()
    {
        if (SelectedSupplier is null || Invoices.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى تحميل كشف الحساب أولاً");
            return;
        }

        var model = new StatementPrintModel
        {
            Title = $"كشف حساب — {SelectedSupplier.Name}",
            PartyName = SelectedSupplier.Name,
            PartyPhone = SelectedSupplier.Phone,
            Columns =
            [
                "رقم الفاتورة", "التاريخ", "النوع", "المورد", "الحالة",
                "الإجمالي", "المدفوع", "المتبقي"
            ],
            Rows = Invoices.Select(i => new object[]
            {
                i.InvoiceNumber,
                i.InvoiceDate.ToString("yyyy/MM/dd"),
                i.InvoiceType.ToString(),
                i.SupplierName ?? SelectedSupplier.Name,
                i.Status.ToString(),
                i.TotalAmount,
                i.PaidAmount,
                i.RemainingAmount
            }).ToList(),
            SummaryLines =
            [
                $"إجمالي الفواتير: {TotalAmount:N0}",
                $"المدفوع: {TotalPaid:N0}",
                $"المتبقي: {TotalRemaining:N0}",
                $"رصيد آجل د.ع: {CreditBalanceIqd:N0}",
                $"رصيد آجل $: {CreditBalanceUsd:N2}"
            ]
        };

        _whatsAppShare.ShareStatement(model, SelectedSupplier.Phone, SelectedSupplier.Name);
    }
}
