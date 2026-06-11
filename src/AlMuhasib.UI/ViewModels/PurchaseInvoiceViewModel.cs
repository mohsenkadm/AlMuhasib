using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
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

    public ProductPickerViewModel ProductPicker { get; }

    [ObservableProperty]
    private bool _isProductPickerOpen;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    // ── Footer / Totals ────────────────────────────────────
    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _roundingAmount;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private int _totalItemCount;

    [ObservableProperty]
    private decimal _totalQuantity;

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
        IExportService exportService,
        IInvoiceTemplateService templateService,
        IInvoiceDraftService draftService,
        IInvoiceQueueService queueService)
    {
        _invoiceService = invoiceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _exportService = exportService;
        _templateService = templateService;
        _draftService = draftService;
        _queueService = queueService;

        PageTitle = "فاتورة مشتريات";

        ProductPicker = new ProductPickerViewModel(_unitOfWork);
        ProductPicker.Confirmed += OnProductPickerConfirmed;
        ProductPicker.Cancelled += () => IsProductPickerOpen = false;

        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    public override bool HasUnsavedChanges =>
        !IsSaved && Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0);

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

            if (InvoiceNavigationBridge.PendingPurchaseEditInvoiceId is int pendingEditId)
            {
                InvoiceNavigationBridge.PendingPurchaseEditInvoiceId = null;
                await LoadInvoiceForEditAsync(pendingEditId);
            }
            else if (InvoiceNavigationBridge.PendingPurchaseCopyInvoiceId is int pendingCopyId)
            {
                InvoiceNavigationBridge.PendingPurchaseCopyInvoiceId = null;
                await CopyFromInvoiceAsync(pendingCopyId);
            }
            else
            {
                TryRestoreDraft();
                ApplyDefaultSupplierIfAny();
            }

            TryOpenPendingQueuePicker();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CopyFromInvoiceAsync(int invoiceId)
    {
        var invoice = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
        if (invoice is null)
        {
            BeautifulMessageDialog.ShowWarning("تعذر تحميل الفاتورة للنسخ");
            return;
        }

        IsSaved = false;
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Purchase);
        InvoiceDate = DateTime.Now;
        Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? string.Empty : $"{invoice.Notes} (نسخة)";

        if (invoice.SupplierId.HasValue)
        {
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == invoice.SupplierId);
            if (SelectedSupplier is not null)
                SupplierSearchText = SelectedSupplier.Name;
        }

        if (invoice.WarehouseId > 0)
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == invoice.WarehouseId);

        IsCashPayment = invoice.PaymentMethod == PaymentMethod.Cash;

        foreach (var row in Items.ToList())
            UnwireItemRow(row);
        Items.Clear();

        foreach (var item in invoice.Items)
        {
            var row = new InvoiceItemRow
            {
                ProductId = item.ProductId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            WireItemRow(row);
            Items.Add(row);
        }

        if (!Items.Any())
            AddRow();

        RecalculateTotals();
        BeautifulMessageDialog.ShowSuccess($"تم نسخ {invoice.Items.Count} بند من الفاتورة {invoice.InvoiceNumber}");
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
        WireItemRow(row);
        Items.Add(row);
    }

    [RelayCommand]
    private async Task OpenProductPickerAsync()
    {
        try
        {
            await ProductPicker.InitializeAsync(SelectedWarehouse?.Id, InvoicePickerMode.Purchase);
            ProductPicker.SeedFromInvoiceItems(Items);
            IsProductPickerOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر فتح اختيار المنتجات:\n{ex.Message}");
        }
    }

    private void OnProductPickerConfirmed()
    {
        InvoiceProductMergeHelper.Merge(
            ProductPicker.BuildResults(),
            Items,
            WireItemRow,
            UnwireItemRow);

        RecalculateTotals();
        IsProductPickerOpen = false;
    }

    private void WireItemRow(InvoiceItemRow row) => row.TotalChanged += RecalculateTotals;

    private void UnwireItemRow(InvoiceItemRow row) => row.TotalChanged -= RecalculateTotals;

    [RelayCommand]
    private void ProcessBarcode()
    {
        if (!InvoiceBarcodeHelper.TryAddByBarcode(
                BarcodeInput,
                Products,
                Items,
                WireItemRow,
                UnwireItemRow,
                null,
                out var error))
        {
            BeautifulMessageDialog.ShowWarning(error);
            return;
        }

        BarcodeInput = string.Empty;
        RecalculateTotals();
    }

    [RelayCommand]
    private void IncreaseRowQuantity(InvoiceItemRow? row)
    {
        if (row is null) return;
        row.Quantity += 1;
        RecalculateTotals();
    }

    [RelayCommand]
    private void DecreaseRowQuantity(InvoiceItemRow? row)
    {
        if (row is null || row.Quantity <= 1) return;
        row.Quantity -= 1;
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveRow(InvoiceItemRow? row)
    {
        if (row is null) return;
        UnwireItemRow(row);
        Items.Remove(row);
        RecalculateTotals();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculateTotals();
        ScheduleDraftSave();
    }

    private bool _isManualGrandTotal;
    private bool _isRecalculating;

    partial void OnGrandTotalChanged(decimal value)
    {
        if (!_isRecalculating)
            _isManualGrandTotal = true;
    }

    private void RecalculateTotals()
    {
        decimal sub = 0m;
        int itemCount = 0;
        decimal totalQty = 0m;
        foreach (var item in Items)
        {
            sub += item.TotalPrice;
            if (!string.IsNullOrWhiteSpace(item.ItemName))
            {
                itemCount++;
                totalQty += item.Quantity;
            }
        }

        Subtotal = sub;
        TotalItemCount = itemCount;
        TotalQuantity = totalQty;
        RoundingAmount = _invoiceService.CalculateRounding(sub, InvoiceType.Purchase);

        if (!_isManualGrandTotal)
        {
            _isRecalculating = true;
            GrandTotal = sub + RoundingAmount;
            _isRecalculating = false;
        }
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

        var validItems = Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0 && (i.UnitPrice > 0 || i.TotalPrice > 0)).ToList();
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
                InvoiceNumber = InvoiceNumber,
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

            Invoice saved;
            if (_editingInvoiceId is int editId)
            {
                saved = await _invoiceService.ReplaceInvoiceAsync(editId, invoice, invoiceItems);
                ClearEditingInvoiceId();
            }
            else
            {
                saved = await _invoiceService.CreateInvoiceAsync(invoice, invoiceItems);
            }

            _savedInvoice = saved;
            _savedItems = invoiceItems;
            IsSaved = true;
            InvoiceNumber = saved.InvoiceNumber;
            _draftService.ClearDraft(DraftKey);

            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ الفاتورة بنجاح\nرقم الفاتورة: {saved.InvoiceNumber}\nالمبلغ الكلي: {saved.NetAmount:N0} د.ع");

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
        ClearEditingInvoiceId();
        _savedInvoice = null;
        _savedItems = [];
        ErrorMessage = string.Empty;
        Notes = string.Empty;
        SupplierSearchText = string.Empty;
        SelectedSupplier = null;
        InvoiceDate = DateTime.Now;
        _isManualGrandTotal = false;

        foreach (var item in Items.ToList())
            UnwireItemRow(item);
        Items.Clear();
        AddRow();

        RecalculateTotals();
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Purchase);
        ApplyDefaultSupplierIfAny();
    }

    // ══════════════════════════════════════════════════════
    // QUICK ADD SUPPLIER
    // ══════════════════════════════════════════════════════
    [ObservableProperty]
    private bool _isQuickAddSupplierOpen;

    [ObservableProperty]
    private string _quickSupplierName = string.Empty;

    [ObservableProperty]
    private string _quickSupplierPhone = string.Empty;

    [ObservableProperty]
    private string _quickSupplierAddress = string.Empty;

    [ObservableProperty]
    private string _quickSupplierError = string.Empty;

    [RelayCommand]
    private void OpenQuickAddSupplier()
    {
        QuickSupplierName = string.Empty;
        QuickSupplierPhone = string.Empty;
        QuickSupplierAddress = string.Empty;
        QuickSupplierError = string.Empty;
        IsQuickAddSupplierOpen = true;
    }

    [RelayCommand]
    private void CancelQuickAddSupplier() => IsQuickAddSupplierOpen = false;

    [RelayCommand]
    private async Task SaveQuickSupplier()
    {
        if (string.IsNullOrWhiteSpace(QuickSupplierName))
        {
            QuickSupplierError = "اسم المورد مطلوب";
            return;
        }

        try
        {
            var newSupplier = new Supplier
            {
                Name = QuickSupplierName.Trim(),
                Phone = string.IsNullOrWhiteSpace(QuickSupplierPhone) ? null : QuickSupplierPhone.Trim(),
                Address = string.IsNullOrWhiteSpace(QuickSupplierAddress) ? null : QuickSupplierAddress.Trim(),
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Suppliers.AddAsync(newSupplier);
            await _unitOfWork.SaveChangesAsync();

            // Reload suppliers
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            Suppliers.Clear();
            FilteredSuppliers.Clear();
            foreach (var s in suppliers)
            {
                Suppliers.Add(s);
                FilteredSuppliers.Add(s);
            }

            // Select the newly created supplier
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == newSupplier.Id);
            IsQuickAddSupplierOpen = false;

            BeautifulMessageDialog.ShowSuccess($"تم إضافة المورد '{newSupplier.Name}' بنجاح");
        }
        catch (Exception ex)
        {
            QuickSupplierError = $"خطأ: {ex.Message}";
        }
    }
}
