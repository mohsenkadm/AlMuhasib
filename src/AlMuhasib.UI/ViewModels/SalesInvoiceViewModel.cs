using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;

    // saved invoice reference for printing
    private Invoice? _savedInvoice;
    private List<InvoiceItem> _savedItems = [];

    // ── Header ─────────────────────────────────────────────
    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _invoiceDate = DateTime.Now;

    // Customer
    [ObservableProperty]
    private string _customerSearchText = string.Empty;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];

    // Warehouse
    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    // Payment — three-way: Cash / Credit / Installment
    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    /// <summary>تاريخ استحقاق التسديد — يظهر فقط عند اختيار الآجل</summary>
    [ObservableProperty]
    private DateTime? _creditDueDate = DateTime.Today.AddMonths(1);

    [ObservableProperty]
    private CashBox? _selectedCashBox;

    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    // ── Items ──────────────────────────────────────────────
    public ObservableCollection<InvoiceItemRow> Items { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    // ── Footer / Totals ────────────────────────────────────
    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _roundingAmount;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private string _notes = string.Empty;

    // ── State ──────────────────────────────────────────────
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(CanPrint))]
    private bool _isSaved;

    public bool CanSave => !IsSaved;
    public bool CanPrint => IsSaved;

    // Helpers for payment visibility
    public bool IsCashPayment => SelectedPaymentMethod == PaymentMethod.Cash;
    public bool IsCreditPayment => SelectedPaymentMethod == PaymentMethod.Credit;
    public bool IsInstallmentPayment => SelectedPaymentMethod == PaymentMethod.Installment;

    public SalesInvoiceViewModel(
        IInvoiceService invoiceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INavigationService navigationService,
        IExportService exportService)
    {
        _invoiceService = invoiceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _navigationService = navigationService;
        _exportService = exportService;

        PageTitle = "فاتورة مبيعات";

        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
    {
        OnPropertyChanged(nameof(IsCashPayment));
        OnPropertyChanged(nameof(IsCreditPayment));
        OnPropertyChanged(nameof(IsInstallmentPayment));
        // Reset CreditDueDate when switching away from Credit
        if (value != PaymentMethod.Credit)
            CreditDueDate = null;
        else
            CreditDueDate = DateTime.Today.AddMonths(1);
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            LoadPermissions(_currentUserService, "SaleInvoice");

            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Sale);

            var customers = await _unitOfWork.Customers.GetAllAsync();
            Customers.Clear();
            FilteredCustomers.Clear();
            foreach (var c in customers)
            {
                Customers.Add(c);
                FilteredCustomers.Add(c);
            }

            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            Warehouses.Clear();
            foreach (var w in warehouses)
                Warehouses.Add(w);
            if (Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];

            var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
            CashBoxes.Clear();
            foreach (var cb in cashBoxes)
                CashBoxes.Add(cb);
            if (CashBoxes.Count > 0)
                SelectedCashBox = CashBoxes[0];

            var products = await _unitOfWork.Products.GetAllAsync();
            Products.Clear();
            foreach (var p in products)
                Products.Add(p);

            AddRow();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Customer search ────────────────────────────────────
    partial void OnSelectedCustomerChanged(Customer? value)
    {
        // When user picks from dropdown, update search text to show selected name
        if (value is not null)
            CustomerSearchText = value.Name;
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        // If the text matches the selected customer, don't clear selection
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;

        // Text changed by user typing → clear selection and filter
        SelectedCustomer = null;

        FilteredCustomers.Clear();
        if (string.IsNullOrWhiteSpace(value))
        {
            foreach (var c in Customers)
                FilteredCustomers.Add(c);
        }
        else
        {
            var term = value.Trim();
            foreach (var c in Customers.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                FilteredCustomers.Add(c);
        }
    }

    // ── Items management ───────────────────────────────────
    [RelayCommand]
    private void AddRow()
    {
        var row = new InvoiceItemRow();
        row.TotalChanged += RecalculateTotals;
        Items.Add(row);
    }

    [RelayCommand]
    private void RemoveRow(InvoiceItemRow? row)
    {
        if (row is null) return;
        row.TotalChanged -= RecalculateTotals;
        Items.Remove(row);
        RecalculateTotals();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        decimal sub = 0m;
        foreach (var item in Items)
            sub += item.TotalPrice;

        Subtotal = sub;
        RoundingAmount = _invoiceService.CalculateRounding(sub, InvoiceType.Sale);
        GrandTotal = sub + RoundingAmount; // RoundingAmount is negative for sales
    }

    // ── Save ───────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveInvoice()
    {
        ErrorMessage = string.Empty;

        // Validation
        if (SelectedWarehouse is null)
        {
            ErrorMessage = "يرجى اختيار المخزن";
            return;
        }

        if (IsCashPayment && SelectedCashBox is null)
        {
            ErrorMessage = "يرجى اختيار القاصة";
            return;
        }

        if (IsCreditPayment && CreditDueDate is null)
        {
            ErrorMessage = "يرجى تحديد تاريخ استحقاق التسديد للدفع الآجل";
            return;
        }

        if (IsCreditPayment && CreditDueDate.HasValue && CreditDueDate.Value.Date <= InvoiceDate.Date)
        {
            ErrorMessage = "تاريخ الاستحقاق يجب أن يكون بعد تاريخ الفاتورة";
            return;
        }

        if (IsInstallmentPayment && SelectedCustomer is null && string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            ErrorMessage = "يجب اختيار العميل لفاتورة الأقساط";
            return;
        }

        var validItems = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0 && i.UnitPrice > 0)
            .ToList();

        if (validItems.Count == 0)
        {
            ErrorMessage = "يجب إضافة عنصر واحد على الأقل بالكمية والسعر";
            return;
        }

        IsBusy = true;

        try
        {
            // Determine customer
            int? customerId = SelectedCustomer?.Id;

            if (customerId is null && !string.IsNullOrWhiteSpace(CustomerSearchText))
            {
                var newCustomer = new Customer
                {
                    Name = CustomerSearchText.Trim(),
                    CreatedBy = _currentUserService.Username,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(newCustomer);
                await _unitOfWork.SaveChangesAsync();
                customerId = newCustomer.Id;
            }

            var invoiceType = IsInstallmentPayment ? InvoiceType.Installment : InvoiceType.Sale;

            var invoice = new Invoice
            {
                InvoiceType = invoiceType,
                CustomerId = customerId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = SelectedPaymentMethod,
                CashBoxId = IsCashPayment && SelectedCashBox is not null ? SelectedCashBox.Id : null,
                Date = InvoiceDate,
                CreditDueDate = IsCreditPayment ? CreditDueDate : null,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            var invoiceItems = new List<InvoiceItem>();
            foreach (var row in validItems)
            {
                int? productId = row.ProductId;
                if (productId is null)
                {
                    var matchedProduct = Products.FirstOrDefault(p =>
                        p.Name.Equals(row.ItemName.Trim(), StringComparison.OrdinalIgnoreCase));
                    productId = matchedProduct?.Id;
                }

                invoiceItems.Add(new InvoiceItem
                {
                    ProductId = productId,
                    ItemName = row.ItemName.Trim(),
                    Quantity = row.Quantity,
                    UnitPrice = row.UnitPrice,
                    TotalPrice = row.TotalPrice
                });
            }

            await _invoiceService.CreateInvoiceAsync(invoice, invoiceItems);

            _savedInvoice = invoice;
            _savedItems = invoiceItems;
            IsSaved = true;
            InvoiceNumber = invoice.InvoiceNumber;

            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ الفاتورة بنجاح\nرقم الفاتورة: {invoice.InvoiceNumber}\nالمبلغ الكلي: {invoice.NetAmount:N0} د.ع");

            PrintInvoice();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Print ──────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanPrint))]
    private void PrintInvoice()
    {
        if (_savedInvoice is null) return;
        var model = new InvoicePrintModel
        {
            Title = "فاتورة مبيعات",
            InvoiceNumber = _savedInvoice.InvoiceNumber,
            Date = _savedInvoice.Date,
            CreditDueDate = _savedInvoice.CreditDueDate,
            PartyLabel = "العميل",
            PartyName = SelectedCustomer?.Name ?? CustomerSearchText,
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            PaymentMethod = _savedInvoice.PaymentMethod switch
            {
                PaymentMethod.Cash => "نقدي",
                PaymentMethod.Credit => "آجل",
                PaymentMethod.Installment => "أقساط",
                _ => "—"
            },
            Notes = _savedInvoice.Notes,
            Subtotal = Subtotal,
            RoundingAmount = RoundingAmount,
            GrandTotal = GrandTotal,
            Items = _savedItems.Select((item, i) => new InvoicePrintItem
            {
                Number = i + 1,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
        _exportService.PrintInvoice(model);
    }

    // ── New invoice (reset) ────────────────────────────────
    [RelayCommand]
    private async Task NewInvoice()
    {
        IsSaved = false;
        _savedInvoice = null;
        _savedItems = [];
        ErrorMessage = string.Empty;
        Notes = string.Empty;
        CustomerSearchText = string.Empty;
        SelectedCustomer = null;
        SelectedPaymentMethod = PaymentMethod.Cash;
        CreditDueDate = null;
        InvoiceDate = DateTime.Now;

        foreach (var item in Items)
            item.TotalChanged -= RecalculateTotals;
        Items.Clear();
        AddRow();

        RecalculateTotals();
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Sale);
    }
}
