using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core;
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

public partial class SalesInvoiceViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IInvoiceDraftService _draftService;
    private readonly IRecentActivityService _recentActivity;
    private DispatcherTimer? _draftSaveTimer;
    private const string DraftKey = "sales-invoice";

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

    public ProductPickerViewModel ProductPicker { get; }

    [ObservableProperty]
    private bool _isProductPickerOpen;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    // ── Footer / Totals ────────────────────────────────────
    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _invoiceDiscountAmount;

    [ObservableProperty]
    private DiscountType _invoiceDiscountType = DiscountType.None;

    [ObservableProperty]
    private decimal _invoiceDiscountValue;

    public IReadOnlyList<DiscountTypeOption> InvoiceDiscountTypeOptions { get; } =
    [
        new(DiscountType.None, "بدون خصم كلي"),
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة (د.ع)")
    ];

    [ObservableProperty]
    private DiscountTypeOption? _selectedInvoiceDiscountOption;

    partial void OnSelectedInvoiceDiscountOptionChanged(DiscountTypeOption? value)
    {
        if (value is not null && InvoiceDiscountType != value.Type)
            InvoiceDiscountType = value.Type;
    }

    partial void OnInvoiceDiscountTypeChanged(DiscountType value)
    {
        var match = InvoiceDiscountTypeOptions.FirstOrDefault(o => o.Type == value);
        if (!Equals(SelectedInvoiceDiscountOption, match))
            SelectedInvoiceDiscountOption = match;
        RecalculateTotals();
    }

    [ObservableProperty]
    private decimal _roundingAmount;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private int _totalItemCount;

    [ObservableProperty]
    private decimal _totalQuantity;

    [ObservableProperty]
    private string _invoiceWeightSummaryText = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    // ── State ──────────────────────────────────────────────
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isReturnMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(CanPrintSavedInvoice))]
    private bool _isSaved;

    public bool CanSave => !IsSaved;

    /// <summary>فاتورة محفوظة وجاهزة للطباعة/واتساب (منفصل عن صلاحية CanPrint في ViewModelBase).</summary>
    public bool CanPrintSavedInvoice => IsSaved;

    partial void OnIsSavedChanged(bool value)
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
        SendInvoiceWhatsAppCommand.NotifyCanExecuteChanged();
    }

    // Helpers for payment visibility
    public bool IsCashPayment => SelectedPaymentMethod == PaymentMethod.Cash;
    public bool IsCreditPayment => SelectedPaymentMethod == PaymentMethod.Credit;
    public bool IsInstallmentPayment => SelectedPaymentMethod == PaymentMethod.Installment;

    public SalesInvoiceViewModel(
        IInvoiceService invoiceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INavigationService navigationService,
        IExportService exportService,
        IWhatsAppShareService whatsAppShare,
        IInvoiceDraftService draftService,
        IRecentActivityService recentActivity,
        IInvoiceTemplateService templateService,
        IInvoiceQueueService queueService,
        IProductPriceService productPriceService,
        IUserPreferencesService userPreferences,
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService)
    {
        _invoiceService = invoiceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _navigationService = navigationService;
        _exportService = exportService;
        _whatsAppShare = whatsAppShare;
        _draftService = draftService;
        _recentActivity = recentActivity;
        _templateService = templateService;
        _queueService = queueService;
        _productPriceService = productPriceService;

        PageTitle = "فاتورة مبيعات";

        ProductPicker = new ProductPickerViewModel(
            _unitOfWork,
            productPriceService,
            userPreferences.Current.FeatureFlags.ProductPricingEnabled);
        ProductPicker.Confirmed += OnProductPickerConfirmed;
        ProductPicker.Cancelled += () => IsProductPickerOpen = false;

        Items.CollectionChanged += OnItemsCollectionChanged;
        ConfigureFeatureServices(featureFlags, productUnitService, productBatchService, productSerialService, productSizeService);
        SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions[0];
    }

    private void ScheduleDraftSave()
    {
        if (IsSaved) return;
        _draftSaveTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _draftSaveTimer.Stop();
        _draftSaveTimer.Tick -= OnDraftSaveTick;
        _draftSaveTimer.Tick += OnDraftSaveTick;
        _draftSaveTimer.Start();
    }

    private void OnDraftSaveTick(object? sender, EventArgs e)
    {
        _draftSaveTimer?.Stop();
        if (IsSaved || !Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName))) return;
        _draftService.SaveDraft(DraftKey, BuildDraft());
    }

    private SalesInvoiceDraft BuildDraft() => new()
    {
        InvoiceNumber = InvoiceNumber,
        InvoiceDate = InvoiceDate,
        CustomerId = SelectedCustomer?.Id,
        WarehouseId = SelectedWarehouse?.Id,
        PaymentMethod = SelectedPaymentMethod,
        CreditDueDate = CreditDueDate,
        CashBoxId = SelectedCashBox?.Id,
        Notes = Notes,
        Lines = Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName)).Select(i => new SalesInvoiceDraftLine
        {
            ProductId = i.ProductId ?? 0,
            ProductName = i.ItemName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    private void ApplyDraft(SalesInvoiceDraft draft)
    {
        InvoiceDate = draft.InvoiceDate;
        SelectedPaymentMethod = draft.PaymentMethod;
        CreditDueDate = draft.CreditDueDate;
        Notes = draft.Notes ?? string.Empty;
        if (draft.CustomerId.HasValue)
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == draft.CustomerId);
        if (draft.WarehouseId.HasValue)
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == draft.WarehouseId);
        if (draft.CashBoxId.HasValue)
            SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Id == draft.CashBoxId);
        Items.Clear();
        foreach (var line in draft.Lines)
        {
            Items.Add(new InvoiceItemRow
            {
                ProductId = line.ProductId > 0 ? line.ProductId : null,
                ItemName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            });
        }
        RecalculateTotals();
    }

    public override bool HasUnsavedChanges =>
        !IsSaved && Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0);

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

            if (InvoiceNavigationBridge.PendingSalesReturnFromInvoiceId is int pendingReturnId)
            {
                InvoiceNavigationBridge.PendingSalesReturnFromInvoiceId = null;
                await LoadAsReturnFromInvoiceAsync(pendingReturnId);
            }
            else if (InvoiceNavigationBridge.PendingSalesCopyInvoiceId is int pendingCopyId)
            {
                InvoiceNavigationBridge.PendingSalesCopyInvoiceId = null;
                await CopyFromInvoiceAsync(pendingCopyId);
            }
            else if (InvoiceNavigationBridge.PendingSalesEditInvoiceId is int pendingEditId)
            {
                InvoiceNavigationBridge.PendingSalesEditInvoiceId = null;
                await LoadInvoiceForEditAsync(pendingEditId);
            }
            else if (_draftService.HasDraft(DraftKey))
            {
                var savedAt = _draftService.GetDraftSavedAt(DraftKey);
                var when = savedAt.HasValue ? savedAt.Value.ToString("yyyy/MM/dd HH:mm") : "";
                if (BeautifulMessageDialog.ShowConfirm(
                        $"يوجد مسودة فاتورة مبيعات محفوظة ({when}).\nهل تريد استعادتها؟"))
                {
                    var draft = _draftService.LoadDraft<SalesInvoiceDraft>(DraftKey);
                    if (draft is not null)
                        ApplyDraft(draft);
                }
            }

            ApplyDefaultCustomerIfAny();
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
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Sale);
        InvoiceDate = DateTime.Now;
        Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? string.Empty : $"{invoice.Notes} (نسخة)";

        if (invoice.CustomerId.HasValue)
        {
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == invoice.CustomerId);
            if (SelectedCustomer is not null)
                CustomerSearchText = SelectedCustomer.Name;
        }

        if (invoice.WarehouseId > 0)
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == invoice.WarehouseId);

        SelectedPaymentMethod = invoice.PaymentMethod == PaymentMethod.Installment
            ? PaymentMethod.Cash
            : invoice.PaymentMethod;

        if (SelectedPaymentMethod == PaymentMethod.Credit)
            CreditDueDate = invoice.CreditDueDate ?? DateTime.Today.AddMonths(1);

        foreach (var row in Items.ToList())
            UnwireItemRow(row);
        Items.Clear();

        foreach (var item in invoice.Items)
        {
            var row = new InvoiceItemRow
            {
                ProductId = item.ProductId,
                PricingTypeId = item.PricingTypeId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            ApplyActiveLabelsToRow(row);
            InvoiceCustomFieldsHelper.ApplyFromJson(row, item.CustomFieldsJson, ActiveCustomFieldLabels);
            WireItemRow(row);
            Items.Add(row);
            _ = LoadRowFeatureDataAsync(row);
        }

        if (!Items.Any())
            AddRow();

        RecalculateTotals();
        BeautifulMessageDialog.ShowSuccess($"تم نسخ {invoice.Items.Count} بند من الفاتورة {invoice.InvoiceNumber}");
    }

    public async Task LoadAsReturnFromInvoiceAsync(int invoiceId)
    {
        var source = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
        var refNumber = source?.InvoiceNumber ?? invoiceId.ToString();
        await CopyFromInvoiceAsync(invoiceId);
        IsReturnMode = true;
        SelectedPaymentMethod = PaymentMethod.Cash;
        Notes = $"مرتجع مبيعات — مرجع {refNumber}";

        foreach (var row in Items.Where(i => i.Quantity != 0).ToList())
            row.Quantity = -Math.Abs(row.Quantity);

        RecalculateTotals();
        BeautifulMessageDialog.ShowInfo("وضع المرتجع: الكميات سالبة لإرجاع البضاعة للمخزن. راجع ثم احفظ.");
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
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;

        SelectedCustomer = null;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, value);
    }

    // ── Items management ───────────────────────────────────
    [RelayCommand]
    private void AddRow()
    {
        var row = new InvoiceItemRow();
        ApplyActiveLabelsToRow(row);
        WireItemRow(row);
        Items.Add(row);
    }

    [RelayCommand]
    private async Task OpenProductPickerAsync()
    {
        try
        {
            await ProductPicker.InitializeAsync(SelectedWarehouse?.Id, InvoicePickerMode.Sale);
            ProductPicker.SeedFromInvoiceItems(Items);
            IsProductPickerOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر فتح اختيار المنتجات:\n{ex.Message}");
        }
    }

    private async void OnProductPickerConfirmed()
    {
        var picks = ProductPicker.BuildResults();
        IsProductPickerOpen = false;

        foreach (var pick in picks.Where(p => p.Quantity > 0))
        {
            var handled = await TryPromptClothingSizesAsync(
                pick.Product,
                pick.SuggestedUnitPrice,
                pick.PricingTypeId,
                pick.PricingTypeName,
                seedQuantities: null);
            if (handled)
                continue;

            InvoiceProductMergeHelper.Merge(
                [pick],
                Items,
                WireItemRow,
                UnwireItemRow);
        }

        foreach (var row in Items.Where(i => i.ProductId is > 0).ToList())
            await LoadRowFeatureDataAsync(row);

        RecalculateTotals();
    }

    private void WireItemRow(InvoiceItemRow row)
    {
        row.ProductDiscountFeatureEnabled = ShowProductDiscount;
        row.RefreshProductDiscount();
        row.TotalChanged += RecalculateTotals;
        row.ProductChanged += OnProductChanged;
    }

    private void UnwireItemRow(InvoiceItemRow row)
    {
        row.TotalChanged -= RecalculateTotals;
        row.ProductChanged -= OnProductChanged;
    }

    [RelayCommand]
    private async Task ProcessBarcodeAsync()
    {
        InvoiceItemRow? updatedRow = null;
        if (!InvoiceBarcodeHelper.TryAddByBarcode(
                BarcodeInput,
                Products,
                Items,
                WireItemRow,
                UnwireItemRow,
                row => updatedRow = row,
                out var error))
        {
            BeautifulMessageDialog.ShowWarning(error);
            return;
        }

        BarcodeInput = string.Empty;

        if (updatedRow?.SelectedProduct is Product product)
        {
            var handled = await TryPromptClothingSizesAsync(
                product,
                updatedRow.UnitPrice,
                updatedRow.PricingTypeId,
                updatedRow.PricingTypeName,
                replaceRow: updatedRow.ProductSizeId is null ? updatedRow : null);
            if (handled)
            {
                RecalculateTotals();
                return;
            }
        }

        if (updatedRow is not null)
            await RefreshProductRowAsync(updatedRow);

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

    private async void OnProductChanged(InvoiceItemRow row) =>
        await RefreshProductRowAsync(row);

    private async Task RefreshProductRowAsync(InvoiceItemRow row)
    {
        if (row.ProductId is null)
        {
            row.StockInfo = string.Empty;
            row.AvailableStock = 0;
            row.AvailablePricingOptions.Clear();
            row.SetSelectedPricingOptionWithoutPrice(null);
            return;
        }

        try
        {
            if (ShowClothingSizes
                && row.SelectedProduct is Product product
                && row.ProductSizeId is null
                && string.IsNullOrWhiteSpace(row.SizeName)
                && _productSizeService is not null
                && await _productSizeService.HasSizesAsync(product.Id))
            {
                await TryPromptClothingSizesAsync(
                    product,
                    row.UnitPrice,
                    row.PricingTypeId,
                    row.PricingTypeName,
                    replaceRow: row);
                return;
            }

            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(s => s.ProductId == row.ProductId.Value);
            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            var warehouseDict = warehouses.ToDictionary(w => w.Id, w => w.Name);

            var lines = stocks
                .Where(s => s.Quantity != 0)
                .Select(s => $"{warehouseDict.GetValueOrDefault(s.WarehouseId, "مخزن")}: {s.Quantity:N0}")
                .ToList();

            row.StockInfo = lines.Count > 0 ? string.Join(" | ", lines) : "لا يوجد رصيد";

            if (SelectedWarehouse is not null)
                row.AvailableStock = stocks.FirstOrDefault(s => s.WarehouseId == SelectedWarehouse.Id)?.Quantity ?? 0;
            else
                row.AvailableStock = stocks.Sum(s => s.Quantity);

            await LoadRowFeatureDataAsync(row);
        }
        catch { row.StockInfo = string.Empty; }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculateTotals();
        ScheduleDraftSave();
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
        InvoiceWeightSummaryText = InvoiceWeightHelper.BuildSummaryText(Items);

        if (ShowProductDiscount)
            InvoiceDiscountAmount = ProductDiscountHelper.CalculateInvoiceDiscount(
                InvoiceDiscountType, InvoiceDiscountValue, sub);
        else
            InvoiceDiscountAmount = 0m;

        var (computedSub, discount, rounding, grand) = InvoiceTotalsCalculator.Compute(
            Items.Select(i => i.TotalPrice),
            _invoiceService,
            InvoiceType.Sale,
            InvoiceDiscountAmount);
        _ = computedSub;
        _ = discount;

        RoundingAmount = rounding;
        GrandTotal = grand;
    }

    partial void OnInvoiceDiscountValueChanged(decimal value) => RecalculateTotals();

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

        if (IsInstallmentPayment)
        {
            ErrorMessage = "لفواتير الأقساط استخدم شاشة «فاتورة أقساط» من القائمة الجانبية";
            return;
        }

        var validItems = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity != 0
                        && (i.UnitPrice > 0 || i.TotalPrice != 0))
            .ToList();

        if (validItems.Count == 0)
        {
            ErrorMessage = "يجب إضافة عنصر واحد على الأقل بالكمية والسعر";
            return;
        }

        if (ShowClothingSizes && _productSizeService is not null)
        {
            foreach (var row in validItems.Where(r => r.ProductId is > 0))
            {
                if (row.ProductSizeId is not null) continue;
                if (!await _productSizeService.HasSizesAsync(row.ProductId!.Value)) continue;
                ErrorMessage = $"المنتج «{row.ItemName}» يتطلب اختيار القياس. افتح اختيار المنتجات أو أعد تحديد المنتج.";
                return;
            }
        }

        // Stock validation for items with known products
        if (SelectedWarehouse is not null)
        {
            foreach (var item in validItems.Where(i => i.ProductId.HasValue))
            {
                var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                    s => s.WarehouseId == SelectedWarehouse.Id && s.ProductId == item.ProductId!.Value);
                var available = stocks.FirstOrDefault()?.Quantity ?? 0;
                var qty = Math.Abs(item.Quantity);
                if (!IsReturnMode && item.Quantity > available)
                {
                    ErrorMessage = $"الكمية المطلوبة من '{item.ItemName}' ({item.Quantity:N0}) تتجاوز الرصيد المتاح ({available:N0}) في المخزن '{SelectedWarehouse.Name}'";
                    return;
                }

                if (IsReturnMode && qty > available)
                {
                    ErrorMessage = $"كمية المرتجع من '{item.ItemName}' ({qty:N0}) تتجاوز ما يمكن إرجاعه للمخزن ({available:N0})";
                    return;
                }
            }

            if (!IsReturnMode && _featureFlags?.ExpiryTracking == true && _productBatchService is not null)
            {
                foreach (var item in validItems.Where(i => i.ProductId.HasValue))
                {
                    var stockQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(item));
                    if (stockQty <= 0) continue;

                    try
                    {
                        if (item.BatchId is int batchId)
                        {
                            var selected = item.AvailableBatches.FirstOrDefault(b => b.Id == batchId);
                            if (selected is not null && selected.Quantity >= stockQty)
                                continue;
                        }

                        await _productBatchService.AllocateFefoAsync(
                            item.ProductId!.Value, SelectedWarehouse.Id, stockQty);
                    }
                    catch (InvalidOperationException ex)
                    {
                        ErrorMessage = $"«{item.ItemName}»: {ex.Message}";
                        return;
                    }
                }
            }
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
                InvoiceNumber = InvoiceNumber,
                InvoiceType = invoiceType,
                CustomerId = customerId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = SelectedPaymentMethod,
                CashBoxId = IsCashPayment && SelectedCashBox is not null ? SelectedCashBox.Id : null,
                Date = InvoiceDate,
                CreditDueDate = IsCreditPayment ? CreditDueDate : null,
                DiscountAmount = ShowProductDiscount ? InvoiceDiscountAmount : 0m,
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

                if (!string.IsNullOrWhiteSpace(row.SizeName))
                {
                    row.CustomField1 = row.SizeName;
                    if (string.IsNullOrWhiteSpace(row.CustomField1Label))
                        row.CustomField1Label = ClothingSizeInvoiceHelper.SizeLabel;
                }

                var displayQty = row.Quantity;
                var stockQty = InvoiceCustomFieldsHelper.ToStockQuantity(row);
                var lineGross = displayQty * row.UnitPrice;
                var lineDiscount = ShowProductDiscount ? row.DiscountAmount : 0m;
                if (lineDiscount > Math.Abs(lineGross))
                    lineDiscount = Math.Abs(lineGross);
                var lineTotal = ProductDiscountHelper.CalculateLineTotal(displayQty, row.UnitPrice, lineDiscount);
                var unitPriceForStorage = stockQty == 0 ? row.UnitPrice : lineGross / stockQty;
                var discountForStorage = stockQty == 0 || displayQty == 0
                    ? lineDiscount
                    : lineDiscount * (stockQty / displayQty);
                invoiceItems.Add(new InvoiceItem
                {
                    ProductId = productId,
                    PricingTypeId = row.PricingTypeId,
                    ItemName = row.ItemName.Trim(),
                    Quantity = stockQty,
                    UnitPrice = unitPriceForStorage,
                    DiscountAmount = discountForStorage,
                    TotalPrice = lineTotal,
                    CustomFieldsJson = InvoiceCustomFieldsHelper.ToJson(row, ActiveCustomFieldLabels)
                });
            }

            // اقرأ العلم الحي — لا تعتمد على حالة قديمة إن تغيّرت الميزات أثناء فتح الشاشة
            if (_featureFlags?.SerialNumbers == true)
            {
                foreach (var row in validItems.Where(r => r.ProductId.HasValue && Math.Abs(r.Quantity) >= 1))
                {
                    if (string.IsNullOrWhiteSpace(row.SerialNumber) && !IsReturnMode)
                    {
                        ErrorMessage = $"أدخل الرقم التسلسلي للصنف «{row.ItemName}»";
                        return;
                    }
                }
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

            try
            {
                await ApplyFeatureSideEffectsOnSaveAsync(validItems, invoiceItems);
            }
            catch (Exception sideEx)
            {
                BeautifulMessageDialog.ShowWarning($"حُفظت الفاتورة مع تحذير الميزات: {sideEx.Message}");
            }

            _savedInvoice = saved;
            _savedItems = invoiceItems;
            IsSaved = true;
            InvoiceNumber = saved.InvoiceNumber;

            _draftService.ClearDraft(DraftKey);
            _recentActivity.Record(
                "فاتورة مبيعات",
                $"{saved.InvoiceNumber} — {saved.NetAmount:N0} د.ع",
                "SaleInvoice",
                typeof(SalesInvoiceViewModel));

            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ الفاتورة بنجاح\nرقم الفاتورة: {saved.InvoiceNumber}\nالمبلغ الكلي: {saved.NetAmount:N0} د.ع\n\nيمكنك الطباعة أو الإرسال عبر واتساب.");

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
    [RelayCommand(CanExecute = nameof(CanPrintSavedInvoice))]
    private void PrintInvoice()
    {
        if (_savedInvoice is null) return;
        _exportService.PrintInvoice(BuildSavedInvoicePrintModel());
    }

    [RelayCommand(CanExecute = nameof(CanPrintSavedInvoice))]
    private void SendInvoiceWhatsApp()
    {
        if (_savedInvoice is null) return;
        _whatsAppShare.ShareInvoice(
            BuildSavedInvoicePrintModel(),
            SelectedCustomer?.Phone,
            SelectedCustomer?.Name ?? CustomerSearchText);
    }

    private InvoicePrintModel BuildSavedInvoicePrintModel()
    {
        if (_savedInvoice is null)
            throw new InvalidOperationException("لا توجد فاتورة محفوظة");

        return new InvoicePrintModel
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
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(
                    item.ItemName,
                    InvoiceCustomFieldsHelper.ExtractSizeName(item.CustomFieldsJson)),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
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
        CustomerSearchText = string.Empty;
        SelectedCustomer = null;
        SelectedPaymentMethod = PaymentMethod.Cash;
        CreditDueDate = null;
        InvoiceDate = DateTime.Now;
        foreach (var item in Items.ToList())
            UnwireItemRow(item);
        Items.Clear();
        AddRow();

        RecalculateTotals();
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Sale);
        ApplyDefaultCustomerIfAny();
    }

    // ══════════════════════════════════════════════════════
    // QUICK ADD CUSTOMER
    // ══════════════════════════════════════════════════════
    [ObservableProperty]
    private bool _isQuickAddCustomerOpen;

    [ObservableProperty]
    private string _quickCustomerName = string.Empty;

    [ObservableProperty]
    private string _quickCustomerPhone = string.Empty;

    [ObservableProperty]
    private string _quickCustomerAddress = string.Empty;

    [ObservableProperty]
    private string _quickCustomerError = string.Empty;

    [RelayCommand]
    private void OpenQuickAddCustomer()
    {
        QuickCustomerName = string.Empty;
        QuickCustomerPhone = string.Empty;
        QuickCustomerAddress = string.Empty;
        QuickCustomerError = string.Empty;
        IsQuickAddCustomerOpen = true;
    }

    [RelayCommand]
    private void CancelQuickAddCustomer() => IsQuickAddCustomerOpen = false;

    [RelayCommand]
    private async Task SaveQuickCustomer()
    {
        if (string.IsNullOrWhiteSpace(QuickCustomerName))
        {
            QuickCustomerError = "اسم العميل مطلوب";
            return;
        }

        try
        {
            var newCustomer = new Customer
            {
                Name = QuickCustomerName.Trim(),
                Phone = string.IsNullOrWhiteSpace(QuickCustomerPhone) ? null : QuickCustomerPhone.Trim(),
                Address = string.IsNullOrWhiteSpace(QuickCustomerAddress) ? null : QuickCustomerAddress.Trim(),
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Customers.AddAsync(newCustomer);
            await _unitOfWork.SaveChangesAsync();

            var customers = await _unitOfWork.Customers.GetAllAsync();
            Customers.Clear();
            FilteredCustomers.Clear();
            foreach (var c in customers)
            {
                Customers.Add(c);
                FilteredCustomers.Add(c);
            }

            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == newCustomer.Id);
            IsQuickAddCustomerOpen = false;

            BeautifulMessageDialog.ShowSuccess($"تم إضافة العميل '{newCustomer.Name}' بنجاح");
        }
        catch (Exception ex)
        {
            QuickCustomerError = $"خطأ: {ex.Message}";
        }
    }
}
