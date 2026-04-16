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

public partial class InstallmentInvoiceViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInstallmentService _installmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;

    private Invoice? _savedInvoice;
    private List<InvoiceItem> _savedItems = [];
    private InstallmentPlan? _savedPlan;

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

    // CashBox (not used for payment, but for system reference)
    [ObservableProperty]
    private CashBox? _selectedCashBox;

    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    // ── Items ──────────────────────────────────────────────
    public ObservableCollection<InvoiceItemRow> Items { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    // ── Installment Plan Fields ────────────────────────────
    [ObservableProperty]
    private int _numberOfInstallments = 6;

    [ObservableProperty]
    private DateTime _installmentStartDate = DateTime.Now.AddMonths(1);

    [ObservableProperty]
    private string _fileNumber = string.Empty;

    [ObservableProperty]
    private decimal _installmentAmount;

    public ObservableCollection<InstallmentScheduleRow> SchedulePreview { get; } = [];

    // ── Totals ─────────────────────────────────────────────
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

    public InstallmentInvoiceViewModel(
        IInvoiceService invoiceService,
        IInstallmentService installmentService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INavigationService navigationService,
        IExportService exportService)
    {
        _invoiceService = invoiceService;
        _installmentService = installmentService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _navigationService = navigationService;
        _exportService = exportService;

        PageTitle = "فاتورة أقساط";
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            LoadPermissions(_currentUserService, "InstallmentInvoice");

            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Installment);

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
            GenerateSchedulePreview();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Customer search ────────────────────────────────────
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

    // ── Schedule regeneration triggers ─────────────────────
    partial void OnNumberOfInstallmentsChanged(int value) => GenerateSchedulePreview();
    partial void OnInstallmentStartDateChanged(DateTime value) => GenerateSchedulePreview();
    partial void OnGrandTotalChanged(decimal value) => GenerateSchedulePreview();

    private void GenerateSchedulePreview()
    {
        SchedulePreview.Clear();

        if (NumberOfInstallments <= 0 || GrandTotal <= 0) return;

        decimal perInstallment = Math.Ceiling(GrandTotal / NumberOfInstallments);
        InstallmentAmount = perInstallment;

        decimal remaining = GrandTotal;
        for (int i = 0; i < NumberOfInstallments; i++)
        {
            decimal amount = (i < NumberOfInstallments - 1) ? perInstallment : remaining;
            SchedulePreview.Add(new InstallmentScheduleRow
            {
                Number = i + 1,
                DueDate = InstallmentStartDate.AddMonths(i),
                Amount = amount
            });
            remaining -= amount;
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
        RoundingAmount = _invoiceService.CalculateRounding(sub, InvoiceType.Installment);
        GrandTotal = sub + RoundingAmount;
    }

    // ── Save ───────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveInvoice()
    {
        ErrorMessage = string.Empty;

        // Validation
        if (SelectedCustomer is null && string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            ErrorMessage = "يجب اختيار العميل لفاتورة الأقساط";
            return;
        }

        if (SelectedWarehouse is null)
        {
            ErrorMessage = "يرجى اختيار المخزن";
            return;
        }

        if (NumberOfInstallments <= 0)
        {
            ErrorMessage = "عدد الأقساط يجب أن يكون أكبر من صفر";
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
                    FileNumber = string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber.Trim(),
                    CreatedBy = _currentUserService.Username,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(newCustomer);
                await _unitOfWork.SaveChangesAsync();
                customerId = newCustomer.Id;
            }

            var invoice = new Invoice
            {
                InvoiceType = InvoiceType.Installment,
                CustomerId = customerId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = PaymentMethod.Installment,
                CashBoxId = SelectedCashBox?.Id ?? 0,
                Date = InvoiceDate,
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

            // Create invoice (handles stock decrease)
            var savedInvoice = await _invoiceService.CreateInvoiceAsync(invoice, invoiceItems);

            // Create installment plan
            var plan = await _installmentService.CreatePlanAsync(
                savedInvoice.Id,
                customerId!.Value,
                string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber.Trim(),
                savedInvoice.NetAmount,
                NumberOfInstallments,
                InstallmentStartDate);

            IsSaved = true;
            InvoiceNumber = savedInvoice.InvoiceNumber;

            _savedInvoice = savedInvoice;
            _savedItems = invoiceItems;
            _savedPlan = plan;

            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ فاتورة الأقساط بنجاح\n" +
                $"رقم الفاتورة: {savedInvoice.InvoiceNumber}\n" +
                $"المبلغ الكلي: {savedInvoice.NetAmount:N0} د.ع\n" +
                $"عدد الأقساط: {NumberOfInstallments}\n" +
                $"مبلغ القسط: {plan.InstallmentAmount:N0} د.ع");

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
            Title = "فاتورة أقساط",
            InvoiceNumber = _savedInvoice.InvoiceNumber,
            Date = _savedInvoice.Date,
            PartyLabel = "العميل",
            PartyName = SelectedCustomer?.Name ?? CustomerSearchText,
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            PaymentMethod = "أقساط",
            Notes = _savedInvoice.Notes,
            FileNumber = string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber,
            Subtotal = Subtotal,
            RoundingAmount = RoundingAmount,
            GrandTotal = GrandTotal,
            NumberOfInstallments = NumberOfInstallments,
            InstallmentAmount = _savedPlan?.InstallmentAmount,
            Items = _savedItems.Select((item, i) => new InvoicePrintItem
            {
                Number = i + 1,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList(),
            Schedule = SchedulePreview.Select(s => new InstallmentPrintRow
            {
                Number = s.Number,
                DueDate = s.DueDate,
                Amount = s.Amount
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
        _savedPlan = null;
        ErrorMessage = string.Empty;
        Notes = string.Empty;
        CustomerSearchText = string.Empty;
        SelectedCustomer = null;
        FileNumber = string.Empty;
        NumberOfInstallments = 6;
        InstallmentStartDate = DateTime.Now.AddMonths(1);
        InvoiceDate = DateTime.Now;

        foreach (var item in Items)
            item.TotalChanged -= RecalculateTotals;
        Items.Clear();
        AddRow();

        RecalculateTotals();
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Installment);
    }
}
