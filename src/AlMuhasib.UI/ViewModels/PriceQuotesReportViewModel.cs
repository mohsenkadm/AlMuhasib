using System.Collections.ObjectModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class PriceQuotesReportViewModel : ReportViewModelBase
{
    private readonly IInvoiceService _invoiceService;

    [ObservableProperty] private string _totalAmount = "0";
    [ObservableProperty] private string _totalAmountUsd = "0";
    [ObservableProperty] private bool _showTotalAmountUsd;
    [ObservableProperty] private string _invoiceCount = "0";
    [ObservableProperty] private string _averageInvoice = "0";

    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearchText = string.Empty;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];

    private List<PriceQuoteReportRow> _allRows = [];
    public ObservableCollection<PriceQuoteReportRow> Rows { get; } = [];

    public PriceQuotesReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IInvoiceService invoiceService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _invoiceService = invoiceService;
        PageTitle = "فواتير عرض السعر";
        DateFrom = DateTime.Today.AddMonths(-1);
        DateTo = DateTime.Today;
        InitReportActionServices(invoiceService);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, ScreenPermissionRegistry.PriceQuotesReport);
        Customers.Clear();
        FilteredCustomers.Clear();
        foreach (var c in (await _unitOfWork.Customers.GetAllAsync()).OrderBy(c => c.Name))
        {
            Customers.Add(c);
            FilteredCustomers.Add(c);
        }
        await LoadDataAsync();
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is not null)
            CustomerSearchText = value.Name;
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;

        SelectedCustomer = null;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, value);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetPriceQuotesReportAsync(DateFrom, DateTo, SelectedCustomer?.Id);
            TotalAmount = FormatCurrency(result.TotalAmount);
            ShowTotalAmountUsd = result.TotalAmountUsd != 0;
            TotalAmountUsd = ShowTotalAmountUsd
                ? FormatCurrency(result.TotalAmountUsd, AccountingCurrency.USD)
                : "0";
            InvoiceCount = result.InvoiceCount.ToString("N0");
            AverageInvoice = FormatCurrency(result.AverageInvoice);
            _allRows = result.Rows;
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
    private async Task ViewDetails(PriceQuoteReportRow? row)
    {
        if (row is null) return;
        try
        {
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(row.InvoiceId);
            if (invoice is null)
            {
                BeautifulMessageDialog.ShowWarning("الفاتورة غير موجودة");
                return;
            }

            InvoiceDetailDialog.Show(invoice, "عرض سعر");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditInvoice(PriceQuoteReportRow? row)
    {
        if (row is null) return;
        if (InvoiceNavigationBridge.EditSalesInvoiceAsync is null)
        {
            BeautifulMessageDialog.ShowWarning("تعذر فتح شاشة فاتورة عرض السعر");
            return;
        }

        try
        {
            await InvoiceNavigationBridge.EditSalesInvoiceAsync(row.InvoiceId);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintRow(PriceQuoteReportRow? row)
    {
        if (row is null) return;
        try
        {
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(row.InvoiceId);
            if (invoice is null)
            {
                BeautifulMessageDialog.ShowWarning("الفاتورة غير موجودة");
                return;
            }

            _exportService.PrintInvoice(BuildQuotePrintModel(invoice));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteRow(PriceQuoteReportRow? row)
    {
        if (row is null) return;
        if (!BeautifulMessageDialog.ShowConfirm($"هل تريد حذف عرض السعر {row.InvoiceNumber}؟"))
            return;

        try
        {
            IsBusy = true;
            await _invoiceService.DeleteInvoiceAsync(row.InvoiceId);
            BeautifulMessageDialog.ShowSuccess("تم حذف عرض السعر بنجاح");
            await LoadDataAsync();
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

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "فواتير_عرض_السعر.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "رقم الفاتورة", "التاريخ", "العميل", "رقم العميل", "العملة", "الصافي", "ملاحظات" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber,
            r.Date.ToString("yyyy/MM/dd"),
            r.CustomerName,
            r.CustomerFileNumber ?? "",
            r.CurrencyLabel,
            r.NetAmount,
            r.Notes ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "عرض السعر", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "العميل", "الصافي", "العملة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber,
            r.Date.ToString("yyyy/MM/dd"),
            r.CustomerName,
            r.NetAmount,
            r.CurrencyLabel
        }).ToList();
        _exportService.PrintTable("فواتير عرض السعر", cols, rows);
    }

    private static InvoicePrintModel BuildQuotePrintModel(Invoice invoice)
    {
        return new InvoicePrintModel
        {
            Title = "عرض سعر",
            InvoiceNumber = invoice.InvoiceNumber,
            Date = invoice.Date,
            PartyLabel = "العميل",
            PartyName = invoice.Customer is null
                ? "—"
                : CustomerDisplayHelper.FormatDisplayName(invoice.Customer.Name, invoice.Customer.FileNumber),
            PartyPhone = invoice.Customer?.Phone,
            PartyAddress = invoice.Customer?.Address,
            FileNumber = invoice.Customer?.FileNumber,
            WarehouseName = string.Empty,
            PaymentMethod = "—",
            Notes = invoice.Notes,
            Subtotal = invoice.TotalAmount,
            DiscountAmount = invoice.DiscountAmount,
            PaidAmount = 0m,
            RemainingAmount = 0m,
            GrandTotal = invoice.NetAmount,
            CurrencyLabel = AccountingCurrencyHelper.GetLabel(invoice.Currency),
            Items = invoice.Items.Select((item, i) => new InvoicePrintItem
            {
                Number = i + 1,
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(item.ItemName, item.CustomFieldsJson),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
    }
}
