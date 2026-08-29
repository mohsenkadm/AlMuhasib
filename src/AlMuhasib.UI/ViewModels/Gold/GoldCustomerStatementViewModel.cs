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

public partial class GoldCustomerStatementViewModel : ViewModelBase
{
    private readonly IGoldCustomerService _customerService;
    private readonly IGoldSaleService _saleService;
    private readonly IExportService _exportService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> Invoices { get; } = [];

    [ObservableProperty] private string _customerSearch = string.Empty;
    [ObservableProperty] private GoldCustomerListItem? _selectedCustomer;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private decimal _creditBalanceIqd;
    [ObservableProperty] private decimal _creditBalanceUsd;
    [ObservableProperty] private decimal _goldCreditGrams;

    public GoldCustomerStatementViewModel(
        IGoldCustomerService customerService,
        IGoldSaleService saleService,
        IExportService exportService,
        IWhatsAppShareService whatsAppShare,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _customerService = customerService;
        _saleService = saleService;
        _exportService = exportService;
        _whatsAppShare = whatsAppShare;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "كشف حساب زبون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.CustomerStatement);
        await LoadCustomersAsync();
        await SelectPendingCustomerAsync();
    }

    private async Task SelectPendingCustomerAsync()
    {
        var pendingId = GoldNavigationContext.TakePendingCustomerId();
        if (pendingId is null)
            return;

        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == pendingId.Value);
        if (SelectedCustomer is not null)
            return;

        try
        {
            var entity = await _customerService.GetByIdAsync(pendingId.Value);
            if (entity is null)
                return;

            var item = new GoldCustomerListItem
            {
                Id = entity.Id,
                Name = entity.Name,
                Phone = entity.Phone,
                Address = entity.Address,
                CreditBalanceIqd = entity.CreditBalanceIqd,
                CreditBalanceUsd = entity.CreditBalanceUsd,
                GoldCreditGrams = entity.GoldCreditGrams,
                IsActive = entity.IsActive
            };
            Customers.Insert(0, item);
            SelectedCustomer = item;
        }
        catch
        {
            // Ignore pre-selection failures; user can pick manually.
        }
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            Customers.Clear();
            var (items, _) = await _customerService.GetPagedAsync(1, 500, CustomerSearch, activeOnly: null);
            foreach (var c in items)
                Customers.Add(c);
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

    partial void OnSelectedCustomerChanged(GoldCustomerListItem? value) => _ = LoadStatementAsync();
    partial void OnCustomerSearchChanged(string value) => _ = LoadCustomersAsync();

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        Invoices.Clear();
        TotalAmount = TotalPaid = TotalRemaining = 0;
        CreditBalanceIqd = CreditBalanceUsd = GoldCreditGrams = 0;

        if (SelectedCustomer is null)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var invoices = await _customerService.GetCustomerInvoicesAsync(SelectedCustomer.Id);
            foreach (var inv in invoices)
                Invoices.Add(inv);

            TotalAmount = invoices.Sum(i => i.TotalAmount);
            TotalPaid = invoices.Sum(i => i.PaidAmount);
            TotalRemaining = invoices.Sum(i => i.RemainingAmount);
            CreditBalanceIqd = SelectedCustomer.CreditBalanceIqd;
            CreditBalanceUsd = SelectedCustomer.CreditBalanceUsd;
            GoldCreditGrams = SelectedCustomer.GoldCreditGrams;
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
        if (SelectedCustomer is null)
        {
            _toast.ShowWarning("اختر زبوناً أولاً");
            return;
        }

        var columns = new[] { "رقم الفاتورة", "التاريخ", "النوع", "الإجمالي", "المدفوع", "المتبقي" };
        IList<object[]> rows = Invoices.Select(i => new object[]
        {
            i.InvoiceNumber,
            i.InvoiceDate.ToString("yyyy/MM/dd"),
            i.InvoiceType.ToString(),
            i.TotalAmount.ToString("N0"),
            i.PaidAmount.ToString("N0"),
            i.RemainingAmount.ToString("N0")
        }).ToList();

        var summary = new List<string>
        {
            $"إجمالي الفواتير: {TotalAmount:N0}",
            $"المدفوع: {TotalPaid:N0}",
            $"المتبقي: {TotalRemaining:N0}",
            $"رصيد آجل د.ع: {CreditBalanceIqd:N0}",
            $"رصيد آجل $: {CreditBalanceUsd:N2}",
            $"ذهب آجل (غ): {GoldCreditGrams:N3}"
        };
        _exportService.PrintTable($"كشف حساب زبون — {SelectedCustomer.Name}", columns, rows, summary);
    }

    [RelayCommand]
    private void ShareWhatsApp()
    {
        if (SelectedCustomer is null)
        {
            _toast.ShowWarning("اختر زبوناً أولاً");
            return;
        }

        try
        {
            var model = new StatementPrintModel
            {
                Title = "كشف حساب زبون ذهب",
                PartyName = SelectedCustomer.Name,
                PartyPhone = SelectedCustomer.Phone,
                Columns = ["رقم الفاتورة", "التاريخ", "الإجمالي", "المدفوع", "المتبقي"],
                Rows = Invoices.Select(i => new object[]
                {
                    i.InvoiceNumber,
                    i.InvoiceDate.ToString("yyyy/MM/dd"),
                    i.TotalAmount.ToString("N0"),
                    i.PaidAmount.ToString("N0"),
                    i.RemainingAmount.ToString("N0")
                }).ToList(),
                SummaryLines =
                [
                    $"إجمالي: {TotalAmount:N0}",
                    $"مدفوع: {TotalPaid:N0}",
                    $"متبقي: {TotalRemaining:N0}",
                    $"آجل د.ع: {CreditBalanceIqd:N0}",
                    $"آجل $: {CreditBalanceUsd:N2}",
                    $"ذهب آجل غ: {GoldCreditGrams:N3}"
                ]
            };
            _whatsAppShare.ShareStatement(model, SelectedCustomer.Phone, SelectedCustomer.Name);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message, "واتساب");
        }
    }

    [RelayCommand]
    private async Task OpenInvoiceDetail(GoldInvoiceListItem? invoice)
    {
        if (invoice is null)
            return;

        try
        {
            var full = invoice.InvoiceType == GoldInvoiceType.Purchase
                ? null
                : await _saleService.GetByIdAsync(invoice.Id);

            if (full is null)
            {
                _toast.ShowError("لم يتم العثور على الفاتورة.");
                return;
            }

            GoldInvoiceDetailDialog.Show(full);
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
