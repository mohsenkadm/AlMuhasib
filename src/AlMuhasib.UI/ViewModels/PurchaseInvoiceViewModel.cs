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

public partial class PurchaseInvoiceViewModel : ViewModelBase, IProductQuickSearchHost
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExportService _exportService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IProductPriceService _productPriceService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly bool _updateProductPriceOnPurchase;
    private readonly IPartyQuickDetailService _partyQuickDetail;
    private readonly IProductQuickDetailService _productQuickDetail;

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
    public ProductQuickSearchCatalog QuickSearchCatalog { get; }

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

    partial void OnIsSavedChanged(bool value)
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
        SendInvoiceWhatsAppCommand.NotifyCanExecuteChanged();
    }

    public PurchaseInvoiceViewModel(
        IInvoiceService invoiceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IExportService exportService,
        IWhatsAppShareService whatsAppShare,
        IInvoiceTemplateService templateService,
        IInvoiceDraftService draftService,
        IInvoiceQueueService queueService,
        IProductPriceService productPriceService,
        IPricingTypeService pricingTypeService,
        IUserPreferencesService userPreferences,
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService,
        IPartyQuickDetailService partyQuickDetail,
        IProductQuickDetailService productQuickDetail)
    {
        _invoiceService = invoiceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _exportService = exportService;
        _whatsAppShare = whatsAppShare;
        _templateService = templateService;
        _draftService = draftService;
        _queueService = queueService;
        _productPriceService = productPriceService;
        _pricingTypeService = pricingTypeService;
        _userPreferences = userPreferences;
        _updateProductPriceOnPurchase = userPreferences.Current.FeatureFlags.UpdateProductPriceOnPurchase
            && userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        _partyQuickDetail = partyQuickDetail;
        _productQuickDetail = productQuickDetail;

        PageTitle = "فاتورة مشتريات";

        ProductPicker = new ProductPickerViewModel(
            _unitOfWork,
            productPriceService,
            userPreferences.Current.FeatureFlags.ProductPricingEnabled);
        ProductPicker.Confirmed += OnProductPickerConfirmed;
        ProductPicker.Cancelled += () => IsProductPickerOpen = false;
        QuickSearchCatalog = new ProductQuickSearchCatalog(_unitOfWork, productPriceService);

        Items.CollectionChanged += OnItemsCollectionChanged;
        ConfigureFeatureServices(
            featureFlags, productUnitService, productBatchService, productSerialService,
            productSizeService, productColorService);
    }

    public override bool HasUnsavedChanges =>
        !IsSaved && Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity != 0);

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

            await QuickSearchCatalog.LoadAsync(
                Products,
                InvoicePickerMode.Purchase,
                ShowProductPricing);

            if (ShowProductPricing)
                await InvoiceBulkPricingHelper.LoadBulkPricingTypesAsync(_pricingTypeService, BulkPricingTypes);

            // Start with one empty row
            AddRow();

            if (InvoiceNavigationBridge.PendingPurchaseReturnFromInvoiceId is int pendingReturnId)
            {
                InvoiceNavigationBridge.PendingPurchaseReturnFromInvoiceId = null;
                InvoiceNavigationBridge.PendingPurchaseReturnMode = false;
                await LoadAsReturnFromInvoiceAsync(pendingReturnId);
            }
            else if (InvoiceNavigationBridge.PendingPurchaseReturnMode)
            {
                InvoiceNavigationBridge.PendingPurchaseReturnMode = false;
                EnterReturnMode();
                InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.PurchaseReturn);
                BeautifulMessageDialog.ShowInfo("وضع مرتجع المشتريات: ابحث عن فاتورة مشتريات من حقل البحث وانسخها كمرتجع، أو أدخل البنود يدوياً بكميات سالبة.");
            }
            else if (InvoiceNavigationBridge.PendingPurchaseEditInvoiceId is int pendingEditId)
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
            InvoiceCustomFieldsHelper.ApplyFromJson(row, item.CustomFieldsJson);
            WireItemRow(row);
            Items.Add(row);
            _ = LoadPurchaseRowFeatureDataAsync(row);
        }

        if (!Items.Any())
            AddRow();

        RecalculateTotals();
        BeautifulMessageDialog.ShowSuccess($"تم نسخ {invoice.Items.Count} بند من الفاتورة {invoice.InvoiceNumber}");
    }

    public async Task LoadAsReturnFromInvoiceAsync(int invoiceId)
    {
        if (!_userPreferences.Current.FeatureFlags.PurchaseReturns)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «مرتجع مشتريات» من إعدادات الميزات أولاً");
            return;
        }

        var source = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
        var refNumber = source?.InvoiceNumber ?? invoiceId.ToString();
        await CopyFromInvoiceAsync(invoiceId);
        EnterReturnMode(refNumber);
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.PurchaseReturn);
        IsCashPayment = true;

        foreach (var row in Items.Where(i => i.Quantity != 0).ToList())
            row.Quantity = -Math.Abs(row.Quantity);

        RecalculateTotals();
        BeautifulMessageDialog.ShowInfo("وضع المرتجع: الكميات سالبة لإرجاع البضاعة للمورد. راجع ثم احفظ.");
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

    private async void OnProductPickerConfirmed()
    {
        var picks = ProductPicker.BuildResults();
        IsProductPickerOpen = false;

        foreach (var pick in picks.Where(p => p.Quantity > 0))
        {
            var handled = await TryPromptClothingSizesAsync(pick.Product, pick.SuggestedUnitPrice);
            if (handled)
                continue;

            InvoiceProductMergeHelper.Merge(
                [pick],
                Items,
                WireItemRow,
                UnwireItemRow);
        }

        foreach (var row in Items.Where(i => i.ProductId is > 0).ToList())
            await LoadPurchaseRowFeatureDataAsync(row);

        RecalculateTotals();
    }

    private void WireItemRow(InvoiceItemRow row)
    {
        row.TotalChanged += RecalculateTotals;
        row.ProductChanged += OnPurchaseProductChanged;
    }

    private void UnwireItemRow(InvoiceItemRow row)
    {
        row.TotalChanged -= RecalculateTotals;
        row.ProductChanged -= OnPurchaseProductChanged;
    }

    private async void OnPurchaseProductChanged(InvoiceItemRow row)
    {
        try
        {
            if (ShowClothingSizes
                && row.SelectedProduct is Product product
                && row.ProductSizeId is null
                && string.IsNullOrWhiteSpace(row.SizeName)
                && _productSizeService is not null
                && await _productSizeService.HasSizesAsync(product.Id))
            {
                await TryPromptClothingSizesAsync(product, row.UnitPrice, replaceRow: row);
                return;
            }

            await LoadPurchaseRowFeatureDataAsync(row);
        }
        catch { /* ignore lookup failures */ }
    }

    [RelayCommand]
    private async Task ProcessBarcode()
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
                replaceRow: updatedRow.ProductSizeId is null ? updatedRow : null);
            if (handled)
            {
                RecalculateTotals();
                return;
            }
        }

        if (updatedRow is not null)
            await LoadPurchaseRowFeatureDataAsync(updatedRow);

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

    private bool _isRecalculating;

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

        var (_, _, rounding, grand) = InvoiceTotalsCalculator.Compute(
            Items.Select(i => i.TotalPrice),
            _invoiceService,
            InvoiceType.Purchase,
            invoiceDiscountAmount: 0m,
            transportFeeAmount: ShowTransportFee ? TransportFeeAmount : 0m);

        RoundingAmount = rounding;
        _isRecalculating = true;
        GrandTotal = grand;
        _isRecalculating = false;
    }

    partial void OnTransportFeeAmountChanged(decimal value) => RecalculateTotals();

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

        var validItems = Items.Where(i =>
                !string.IsNullOrWhiteSpace(i.ItemName)
                && (IsReturnMode ? i.Quantity != 0 : i.Quantity > 0)
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

        if (IsReturnMode && !_userPreferences.Current.FeatureFlags.PurchaseReturns)
        {
            ErrorMessage = "فعّل «مرتجع مشتريات» من إعدادات الميزات أولاً";
            return;
        }

        if (_userPreferences.Current.FeatureFlags.AddMissingProductsOnPurchase)
        {
            var missingNames = validItems
                .Where(row =>
                {
                    if (row.ProductId is > 0) return false;
                    var name = row.ItemName.Trim();
                    return !Products.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                })
                .Select(row => row.ItemName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingNames.Count > 0)
            {
                var preview = string.Join("\n", missingNames.Take(12).Select(n => $"• {n}"));
                if (missingNames.Count > 12)
                    preview += $"\n… و{missingNames.Count - 12} أخرى";

                var confirmed = BeautifulMessageDialog.ShowConfirm(
                    $"الأسماء التالية غير موجودة في المنتجات:\n{preview}\n\nهل تريد إضافتها إلى المنتجات ثم حفظ الفاتورة؟",
                    "منتجات ناقصة");
                if (!confirmed)
                {
                    ErrorMessage = "تم إلغاء الحفظ — لم تُضف المنتجات الناقصة";
                    return;
                }

                IsBusy = true;
                try
                {
                    await EnsureMissingProductsCreatedAsync(missingNames, validItems);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"تعذّر إنشاء المنتجات الناقصة: {ex.Message}";
                    IsBusy = false;
                    return;
                }
            }
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

            var invoiceType = IsReturnMode ? InvoiceType.PurchaseReturn : InvoiceType.Purchase;

            var invoice = new Invoice
            {
                InvoiceNumber = InvoiceNumber,
                InvoiceType = invoiceType,
                SupplierId = supplierId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = IsCashPayment ? PaymentMethod.Cash : PaymentMethod.Credit,
                CashBoxId = IsCashPayment && SelectedCashBox is not null ? SelectedCashBox.Id : null,
                Date = InvoiceDate,
                TransportFeeAmount = ShowTransportFee ? Math.Max(0m, TransportFeeAmount) : 0m,
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
                    if (productId is > 0)
                    {
                        row.ProductId = productId;
                        row.SelectedProduct = matchedProduct;
                    }
                }

                if (!string.IsNullOrWhiteSpace(row.SizeName))
                {
                    row.CustomField1 = row.SizeName;
                    if (string.IsNullOrWhiteSpace(row.CustomField1Label))
                        row.CustomField1Label = ClothingSizeInvoiceHelper.SizeLabel;
                }

                var stockQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(row));
                var lineTotal = Math.Abs(row.Quantity) * row.UnitPrice;
                var unitPriceForStorage = stockQty == 0 ? row.UnitPrice : lineTotal / stockQty;

                invoiceItems.Add(new InvoiceItem
                {
                    ProductId = productId,
                    PricingTypeId = row.PricingTypeId,
                    ItemName = row.ItemName.Trim(),
                    Quantity = stockQty,
                    UnitPrice = unitPriceForStorage,
                    TotalPrice = lineTotal,
                    CustomFieldsJson = InvoiceCustomFieldsHelper.ToJson(row, [ClothingSizeInvoiceHelper.SizeLabel])
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

                if (_updateProductPriceOnPurchase && !IsReturnMode)
                {
                    foreach (var item in invoiceItems.Where(i => i.ProductId is > 0 && i.PricingTypeId is > 0))
                    {
                        await _productPriceService.UpdatePurchasePriceAsync(
                            item.ProductId!.Value,
                            item.PricingTypeId!.Value,
                            item.UnitPrice);
                    }
                }
            }

            try
            {
                await ApplyPurchaseFeatureSideEffectsAsync(validItems);
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

            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ {(IsReturnMode ? "مرتجع المشتريات" : "الفاتورة")} بنجاح\nرقم الفاتورة: {saved.InvoiceNumber}\nالمبلغ الكلي: {saved.NetAmount:N0} د.ع");

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

    private async Task EnsureMissingProductsCreatedAsync(
        IReadOnlyList<string> missingNames,
        IReadOnlyList<InvoiceItemRow> validItems)
    {
        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var defaultCategory = categories.FirstOrDefault(c =>
                c.Name.Equals("عام", StringComparison.OrdinalIgnoreCase))
            ?? categories.FirstOrDefault();

        if (defaultCategory is null)
        {
            defaultCategory = new Category
            {
                Name = "عام",
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Categories.AddAsync(defaultCategory);
            await _unitOfWork.SaveChangesAsync();
        }

        foreach (var name in missingNames)
        {
            if (Products.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var product = new Product
            {
                Name = name,
                CategoryId = defaultCategory.Id,
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            Products.Add(product);

            foreach (var row in validItems.Where(r =>
                         r.ProductId is null or 0
                         && r.ItemName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                row.ProductId = product.Id;
                row.SelectedProduct = product;
            }
        }
    }

    // ── Print / WhatsApp ───────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanPrint))]
    private void PrintInvoice()
    {
        if (_savedInvoice is null) return;
        _exportService.PrintInvoice(BuildSavedInvoicePrintModel());
    }

    [RelayCommand(CanExecute = nameof(CanPrint))]
    private void SendInvoiceWhatsApp()
    {
        if (_savedInvoice is null) return;
        _whatsAppShare.ShareInvoice(
            BuildSavedInvoicePrintModel(),
            SelectedSupplier?.Phone,
            SelectedSupplier?.Name ?? SupplierSearchText);
    }

    private InvoicePrintModel BuildSavedInvoicePrintModel()
    {
        if (_savedInvoice is null)
            throw new InvalidOperationException("لا توجد فاتورة محفوظة");

        return new InvoicePrintModel
        {
            Title = IsReturnMode ? "مرتجع مشتريات" : "فاتورة مشتريات",
            InvoiceNumber = _savedInvoice.InvoiceNumber,
            Date = _savedInvoice.Date,
            PartyLabel = "المورد",
            PartyName = SelectedSupplier?.Name ?? SupplierSearchText,
            PartyPhone = SelectedSupplier?.Phone,
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            PaymentMethod = IsCashPayment ? "نقدي" : "آجل",
            Notes = _savedInvoice.Notes,
            Subtotal = Subtotal,
            RoundingAmount = RoundingAmount,
            TransportFeeAmount = ShowTransportFee ? TransportFeeAmount : 0m,
            GrandTotal = GrandTotal,
            Items = _savedItems.Select((item, i) => new InvoicePrintItem
            {
                Number = i + 1,
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(
                    item.ItemName,
                    item.CustomFieldsJson),
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
        IsReturnMode = false;
        PageTitle = "فاتورة مشتريات";
        ClearEditingInvoiceId();
        _savedInvoice = null;
        _savedItems = [];
        ErrorMessage = string.Empty;
        Notes = string.Empty;
        TransportFeeAmount = 0m;
        SupplierSearchText = string.Empty;
        SelectedSupplier = null;
        InvoiceDate = DateTime.Now;

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
    private void ShowSelectedPartyDetails()
    {
        if (SelectedSupplier is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر مورداً أولاً لعرض تفاصيله");
            return;
        }

        PartyQuickDetailDialog.ShowSupplier(_partyQuickDetail, SelectedSupplier.Id);
    }

    [RelayCommand]
    private void ShowProductDetails(InvoiceItemRow? row)
    {
        if (row?.ProductId is not > 0)
        {
            BeautifulMessageDialog.ShowWarning("اختر منتجاً مسجلاً لعرض تفاصيله");
            return;
        }

        ProductQuickDetailDialog.Show(_productQuickDetail, row.ProductId.Value);
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
