using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class PurchaseInvoiceViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExportService _exportService;

    private Invoice? _savedInvoice;
    private List<InvoiceItem> _savedItems = [];

    // ── Header ─────────────────────────────────────────────
    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _invoiceDate = DateTime.Now;

    // Supplier
    [ObservableProperty]
    private string _supplierSearchText = string.Empty;

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<Supplier> FilteredSuppliers { get; } = [];

    // Warehouse
    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    // Payment
    [ObservableProperty]
    private bool _isCashPayment = true;

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

    public PurchaseInvoiceViewModel(
        IInvoiceService invoiceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IExportService exportService)
    {
        _invoiceService = invoiceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _exportService = exportService;

        PageTitle = "فاتورة مشتريات";

        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            LoadPermissions(_currentUserService, "PurchaseInvoice");

            // Generate invoice number
            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Purchase);

            // Load lookup data
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            Suppliers.Clear();
            FilteredSuppliers.Clear();
            foreach (var s in suppliers)
            {
                Suppliers.Add(s);
                FilteredSuppliers.Add(s);
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

            // Start with one empty row
            AddRow();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Supplier search ────────────────────────────────────
    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value is not null)
            SupplierSearchText = value.Name;
    }

    partial void OnSupplierSearchTextChanged(string value)
    {
        if (SelectedSupplier is not null && SelectedSupplier.Name == value)
            return;

        SelectedSupplier = null;

        FilteredSuppliers.Clear();
        if (string.IsNullOrWhiteSpace(value))
        {
            foreach (var s in Suppliers)
                FilteredSuppliers.Add(s);
        }
        else
        {
            var term = value.Trim();
            foreach (var s in Suppliers.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                FilteredSuppliers.Add(s);
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
        RoundingAmount = _invoiceService.CalculateRounding(sub, InvoiceType.Purchase);
        GrandTotal = sub + RoundingAmount;
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

        var validItems = Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0 && i.UnitPrice > 0).ToList();
        if (validItems.Count == 0)
        {
            ErrorMessage = "يجب إضافة عنصر واحد على الأقل بالكمية والسعر";
            return;
        }

        IsBusy = true;

        try
        {
            // Determine supplier
            int? supplierId = SelectedSupplier?.Id;

            // If supplier name typed manually but not selected from list, create new
            if (supplierId is null && !string.IsNullOrWhiteSpace(SupplierSearchText))
            {
                var newSupplier = new Supplier
                {
                    Name = SupplierSearchText.Trim(),
                    CreatedBy = _currentUserService.Username,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Suppliers.AddAsync(newSupplier);
                await _unitOfWork.SaveChangesAsync();
                supplierId = newSupplier.Id;
            }

            var invoice = new Invoice
            {
                InvoiceType = InvoiceType.Purchase,
                SupplierId = supplierId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = IsCashPayment ? PaymentMethod.Cash : PaymentMethod.Credit,
                CashBoxId = IsCashPayment && SelectedCashBox is not null ? SelectedCashBox.Id : null,
                Date = InvoiceDate,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            var invoiceItems = new List<InvoiceItem>();
            foreach (var row in validItems)
            {
                // If product name matches an existing product, link it
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
            Title = "فاتورة مشتريات",
            InvoiceNumber = _savedInvoice.InvoiceNumber,
            Date = _savedInvoice.Date,
            PartyLabel = "المورد",
            PartyName = SelectedSupplier?.Name ?? SupplierSearchText,
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            PaymentMethod = IsCashPayment ? "نقدي" : "آجل",
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
        SupplierSearchText = string.Empty;
        SelectedSupplier = null;
        InvoiceDate = DateTime.Now;

        // Clear items and add fresh row
        foreach (var item in Items)
            item.TotalChanged -= RecalculateTotals;
        Items.Clear();
        AddRow();

        RecalculateTotals();
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Purchase);
    }
}
