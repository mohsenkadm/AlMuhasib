using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBarcodeLabelService _barcodeLabelService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IProductPriceService _productPriceService;
    private readonly bool _pricingEnabled;

    // ── Collections ────────────────────────────────────────
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<ProductCardDisplay> ProductCards { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<PricingType> PricingTypes { get; } = [];

    // ── Filter / Search ────────────────────────────────────
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory;

    // ── Pagination ─────────────────────────────────────────
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private string _paginationText = string.Empty;

    // ── Selected item ──────────────────────────────────────
    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private bool _isCardView;

    // ── Dialog state ───────────────────────────────────────
    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _dialogTitle = string.Empty;

    // ── Dialog form fields ─────────────────────────────────
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editBarcode = string.Empty;

    [ObservableProperty]
    private string _editScientificName = string.Empty;

    [ObservableProperty]
    private Category? _editCategory;

    [ObservableProperty]
    private decimal _editWeight;

    [ObservableProperty]
    private string _editWeightUnit = "كغ";

    [ObservableProperty]
    private DiscountType _editDiscountType = DiscountType.None;

    [ObservableProperty]
    private decimal _editDiscountValue;

    [ObservableProperty]
    private bool _editDiscountHasExpiry;

    [ObservableProperty]
    private DateTime? _editDiscountExpiresAt;

    [ObservableProperty]
    private string _dialogError = string.Empty;

    // ── Bulk discount selection ────────────────────────────
    [ObservableProperty]
    private bool _hasBulkDiscountSelection;

    [ObservableProperty]
    private int _bulkDiscountSelectionCount;

    // ── Delete confirmation ────────────────────────────────
    [ObservableProperty]
    private bool _isDeleteDialogOpen;

    [ObservableProperty]
    private Product? _productToDelete;

    // ── Price edit dialog ──────────────────────────────────
    [ObservableProperty]
    private bool _isPriceEditDialogOpen;

    [ObservableProperty]
    private string _priceEditProductName = string.Empty;

    [ObservableProperty]
    private PricingType? _editPricingType;

    [ObservableProperty]
    private decimal _editSalePrice;

    [ObservableProperty]
    private decimal _editPurchasePrice;

    [ObservableProperty]
    private string _priceEditError = string.Empty;

    [ObservableProperty]
    private bool _showPricingOnCards;

    private int? _editingProductId;
    private int? _editingPriceProductId;
    private int? _editingProductPriceId;
    private System.Timers.Timer? _debounceTimer;
    private bool _isInitializing;
    private int _loadRequestId;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public ProductsViewModel(
        IProductService productService,
        IUnitOfWork unitOfWork,
        IAuthService authService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IBarcodeLabelService barcodeLabelService,
        IUserPreferencesService userPreferences,
        IProductPriceService productPriceService,
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IPackagingTypeService packagingTypeService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService)
    {
        _productService = productService;
        _unitOfWork = unitOfWork;
        _authService = authService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _barcodeLabelService = barcodeLabelService;
        _userPreferences = userPreferences;
        _productPriceService = productPriceService;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        ShowPricingOnCards = _pricingEnabled;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.Products);

        PageTitle = "المنتجات";
        ConfigureFeatureServices(
            featureFlags, productUnitService, packagingTypeService, productBatchService,
            productSerialService, productSizeService, productColorService);
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        _isInitializing = true;

        try
        {
            LoadPermissions(_currentUserService, "Products");
            await LoadCategoriesAsync();
            await RefreshFeatureFilterOptionsAsync();
            await LoadProductsAsync();
        }
        finally
        {
            _isInitializing = false;
            IsBusy = false;
        }
    }

    // ── Category loading ───────────────────────────────────
    private async Task LoadCategoriesAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        Categories.Clear();
        foreach (var c in categories)
            Categories.Add(c);
    }

    // ── Product loading ────────────────────────────────────
    private async Task LoadProductsAsync()
    {
        var requestId = ++_loadRequestId;
        await _loadLock.WaitAsync();
        try
        {
            // تجاهل الطلبات القديمة إذا وُجد طلب أحدث في الانتظار.
            if (requestId != _loadRequestId)
                return;

            var sizeFilter = ShowSizesSection
                             && !string.IsNullOrWhiteSpace(SelectedSizeFilter)
                             && SelectedSizeFilter != ProductsViewModel.AllFilterLabel
                ? SelectedSizeFilter.Trim()
                : null;
            var colorFilter = ShowColorsSection
                              && !string.IsNullOrWhiteSpace(SelectedColorFilter)
                              && SelectedColorFilter != ProductsViewModel.AllFilterLabel
                ? SelectedColorFilter.Trim()
                : null;
            var hasBatchesOnly = ShowBatchesSection && FilterHasBatchesOnly;

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _productService.GetPagedAsync(
                    1, int.MaxValue,
                    SelectedCategory?.Id,
                    string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                    sizeFilter,
                    colorFilter,
                    hasBatchesOnly);

                if (requestId != _loadRequestId) return;

                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Products, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                await RebuildProductCardsAsync(Products);
                return;
            }

            var (items, totalCount) = await _productService.GetPagedAsync(
                CurrentPage,
                PageSize,
                SelectedCategory?.Id,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                sizeFilter,
                colorFilter,
                hasBatchesOnly);

            if (requestId != _loadRequestId) return;

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Products.Clear();
            foreach (var p in items)
                Products.Add(p);

            await RebuildProductCardsAsync(items);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task RebuildProductCardsAsync(IEnumerable<Product> items)
    {
        // بطاقات العرض فقط — لا نحمّل أسعار البطاقات في وضع الجدول لتفادي بطء/تجمّد الفتح.
        if (!IsCardView)
        {
            ProductCards.Clear();
            return;
        }

        ProductCards.Clear();
        var list = items.ToList();
        Dictionary<int, List<ProductPrice>> pricesByProduct = new();

        if (_pricingEnabled && list.Count > 0)
        {
            try
            {
                if (PricingTypes.Count == 0)
                {
                    foreach (var t in await _unitOfWork.PricingTypes.GetAllAsync())
                        PricingTypes.Add(t);
                }

                var prices = await _productPriceService.GetByProductIdsAsync(list.Select(p => p.Id));
                pricesByProduct = prices.GroupBy(p => p.ProductId).ToDictionary(g => g.Key, g => g.ToList());
            }
            catch
            {
                // لا نُفشل قائمة المنتجات إذا تعذّر تحميل الأسعار للبطاقات.
                pricesByProduct = new();
            }
        }

        foreach (var product in list)
        {
            var categoryName = product.Category?.Name
                ?? Categories.FirstOrDefault(c => c.Id == product.CategoryId)?.Name
                ?? "—";

            var card = new ProductCardDisplay
            {
                Product = product,
                Name = product.Name,
                ScientificName = product.ScientificName,
                Barcode = product.Barcode,
                Description = product.Description,
                CategoryName = categoryName
            };
            if (pricesByProduct.TryGetValue(product.Id, out var productPrices))
            {
                foreach (var price in productPrices)
                {
                    card.Prices.Add(new ProductPriceCardLine
                    {
                        ProductPriceId = price.Id,
                        ProductId = product.Id,
                        PricingTypeId = price.PricingTypeId,
                        PricingTypeName = price.PricingType?.Name ?? "",
                        SalePrice = price.SalePrice,
                        PurchasePrice = price.PurchasePrice
                    });
                }
            }

            ProductCards.Add(card);
        }
    }

    protected override void OnColumnFiltersChanged()
    {
        if (_isInitializing) return;
        CurrentPage = 1;
        _ = ReloadAsync();
    }

    // ── Search with debounce ───────────────────────────────
    partial void OnSearchTextChanged(string value)
    {
        if (_isInitializing) return;

        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadProductsAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (_isInitializing) return;
        CurrentPage = 1;
        _ = ReloadAsync();
    }

    // ── Pagination commands ────────────────────────────────
    [RelayCommand]
    private async Task FirstPage()
    {
        CurrentPage = 1;
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task LastPage()
    {
        CurrentPage = TotalPages;
        await LoadProductsAsync();
    }

    // ── Refresh ────────────────────────────────────────────
    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        SelectedCategory = null;
        await LoadProductsAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadProductsAsync();
    }

    // ── Add / Edit Dialog ──────────────────────────────────
    [RelayCommand]
    private async Task OpenAddDialog()
    {
        _editingProductId = null;
        IsEditMode = false;
        DialogTitle = "إضافة منتج جديد";
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditBarcode = string.Empty;
        EditScientificName = string.Empty;
        EditCategory = null;
        EditWeight = 0m;
        EditWeightUnit = "كغ";
        EditDiscountType = DiscountType.None;
        EditDiscountTypeOption = ProductDiscountTypeOptions[0];
        EditDiscountValue = 0m;
        EditDiscountHasExpiry = false;
        EditDiscountExpiresAt = null;
        DialogError = string.Empty;
        ClearFeatureEditCollections();
        await LoadMinQuantitiesForProductAsync(null);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(Product product)
    {
        if (product is null) return;

        _editingProductId = product.Id;
        IsEditMode = true;
        DialogTitle = "تعديل المنتج";
        EditName = product.Name;
        EditDescription = product.Description ?? string.Empty;
        EditBarcode = product.Barcode ?? string.Empty;
        EditScientificName = product.ScientificName ?? string.Empty;
        EditCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        EditWeight = product.Weight;
        EditWeightUnit = string.IsNullOrWhiteSpace(product.WeightUnit) ? "كغ" : product.WeightUnit;
        EditDiscountType = product.DiscountType;
        EditDiscountTypeOption = ProductDiscountTypeOptions.FirstOrDefault(o => o.Type == product.DiscountType)
            ?? ProductDiscountTypeOptions[0];
        EditDiscountValue = product.DiscountValue;
        EditDiscountHasExpiry = product.DiscountExpiresAt.HasValue;
        EditDiscountExpiresAt = product.DiscountExpiresAt?.ToLocalTime().Date;
        DialogError = string.Empty;
        await LoadFeatureDataForProductAsync(product.Id);
        await LoadMinQuantitiesForProductAsync(product.Id);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveProduct()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم المنتج مطلوب";
            return;
        }
        if (EditCategory is null)
        {
            DialogError = "يرجى اختيار الصنف";
            return;
        }

        if (ShowDiscountSection && EditDiscountType != DiscountType.None)
        {
            if (EditDiscountValue <= 0)
            {
                DialogError = "أدخل قيمة خصم أكبر من صفر أو اختر بدون خصم";
                return;
            }
            if (EditDiscountType == DiscountType.Percentage && EditDiscountValue > 100m)
            {
                DialogError = "نسبة الخصم لا تتجاوز 100%";
                return;
            }
            if (EditDiscountHasExpiry && EditDiscountExpiresAt is null)
            {
                DialogError = "حدد تاريخ انتهاء الخصم أو ألغِ خيار الانتهاء";
                return;
            }
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingProductId.HasValue)
            {
                var product = await _productService.GetByIdAsync(_editingProductId.Value);
                if (product is null) return;

                product.Name = EditName.Trim();
                product.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
                product.Barcode = string.IsNullOrWhiteSpace(EditBarcode) ? null : EditBarcode.Trim();
                product.ScientificName = string.IsNullOrWhiteSpace(EditScientificName) ? null : EditScientificName.Trim();
                product.CategoryId = EditCategory.Id;
                product.Weight = EditWeight < 0 ? 0m : EditWeight;
                product.WeightUnit = string.IsNullOrWhiteSpace(EditWeightUnit) ? null : EditWeightUnit.Trim();
                ApplyEditDiscountToProduct(product);

                await _productService.UpdateAsync(product);
                await SaveMinQuantitiesAsync(product.Id);
            }
            else
            {
                var product = new Product
                {
                    Name = EditName.Trim(),
                    Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                    Barcode = string.IsNullOrWhiteSpace(EditBarcode) ? null : EditBarcode.Trim(),
                    ScientificName = string.IsNullOrWhiteSpace(EditScientificName) ? null : EditScientificName.Trim(),
                    CategoryId = EditCategory.Id,
                    Weight = EditWeight < 0 ? 0m : EditWeight,
                    WeightUnit = string.IsNullOrWhiteSpace(EditWeightUnit) ? null : EditWeightUnit.Trim()
                };
                ApplyEditDiscountToProduct(product);

                var created = await _productService.CreateAsync(product);
                _editingProductId = created.Id;
                IsEditMode = true;
                DialogTitle = "تعديل المنتج";
                await SaveMinQuantitiesAsync(created.Id);
                await LoadFeatureDataForProductAsync(created.Id);
                await LoadMinQuantitiesForProductAsync(created.Id);
                BeautifulMessageDialog.ShowSuccess("تم حفظ المنتج — يمكنك الآن إضافة الوحدات/الدفعات/السيريالات إن كانت مفعّلة");
                await LoadProductsAsync();
                return;
            }

            IsDialogOpen = false;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    private void ApplyEditDiscountToProduct(Product product)
    {
        if (!ShowDiscountSection || EditDiscountType == DiscountType.None || EditDiscountValue <= 0)
        {
            product.DiscountType = DiscountType.None;
            product.DiscountValue = 0m;
            product.DiscountExpiresAt = null;
            return;
        }

        product.DiscountType = EditDiscountType;
        product.DiscountValue = Math.Max(0m, EditDiscountValue);
        product.DiscountExpiresAt = EditDiscountHasExpiry && EditDiscountExpiresAt is DateTime d
            ? DateTime.SpecifyKind(d.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime()
            : null;
    }

    public void UpdateBulkDiscountSelectionFromGrid(System.Windows.Controls.DataGrid grid)
    {
        var count = grid.SelectedItems.OfType<Product>().Count();
        BulkDiscountSelectionCount = count;
        HasBulkDiscountSelection = ShowDiscountSection && count > 0;
    }

    [RelayCommand]
    private void ClearBulkDiscountSelection(System.Windows.Controls.DataGrid? grid)
    {
        grid?.UnselectAll();
        BulkDiscountSelectionCount = 0;
        HasBulkDiscountSelection = false;
    }

    [RelayCommand]
    private async Task OpenBulkDiscountDialog(System.Windows.Controls.DataGrid? grid)
    {
        if (!ShowDiscountSection || grid is null)
            return;

        var selected = grid.SelectedItems.OfType<Product>().ToList();
        if (selected.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("حدّد منتجاً واحداً أو أكثر من الجدول أولاً");
            return;
        }

        var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current?.MainWindow;
        var result = ProductBulkDiscountDialog.Show(owner, selected.Count);
        if (result is null)
            return;

        var (_, cleared, type, value, expiresAt) = result.Value;
        try
        {
            if (cleared)
            {
                await _productService.ApplyDiscountToProductsAsync(
                    selected.Select(p => p.Id), DiscountType.None, 0m, null);
                BeautifulMessageDialog.ShowSuccess($"تم إلغاء الخصم عن {selected.Count} منتج");
            }
            else
            {
                await _productService.ApplyDiscountToProductsAsync(
                    selected.Select(p => p.Id), type, value, expiresAt);
                BeautifulMessageDialog.ShowSuccess($"تم تطبيق الخصم على {selected.Count} منتج");
            }

            ClearBulkDiscountSelection(grid);
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر تحديث الخصم: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDialog()
    {
        IsDialogOpen = false;
    }

    // ── Delete ─────────────────────────────────────────────
    [RelayCommand]
    private void ConfirmDelete(Product product)
    {
        if (product is null) return;
        ProductToDelete = product;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (ProductToDelete is null) return;

        try
        {
            await _productService.DeleteAsync(ProductToDelete.Id);
            IsDeleteDialogOpen = false;
            ProductToDelete = null;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الحذف: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        ProductToDelete = null;
    }

    // ── Export to Excel ────────────────────────────────────
    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            // Load all matching products (not just current page)
            var sizeFilter = ShowSizesSection
                             && !string.IsNullOrWhiteSpace(SelectedSizeFilter)
                             && SelectedSizeFilter != AllFilterLabel
                ? SelectedSizeFilter.Trim()
                : null;
            var colorFilter = ShowColorsSection
                              && !string.IsNullOrWhiteSpace(SelectedColorFilter)
                              && SelectedColorFilter != AllFilterLabel
                ? SelectedColorFilter.Trim()
                : null;
            var (allItems, _) = await _productService.GetPagedAsync(
                1, int.MaxValue,
                SelectedCategory?.Id,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                sizeFilter,
                colorFilter,
                ShowBatchesSection && FilterHasBatchesOnly);

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"المنتجات_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            if (ShowScientificName)
            {
                var exportData = allItems.Select(p => new
                {
                    الاسم = p.Name,
                    الاسم_العلمي = p.ScientificName ?? "",
                    الصنف = p.Category?.Name ?? "",
                    الباركود = p.Barcode ?? "",
                    الوصف = p.Description ?? "",
                    تاريخ_الإنشاء = p.CreatedAt.ToString("yyyy/MM/dd")
                });
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المنتجات");
            }
            else
            {
                var exportData = allItems.Select(p => new
                {
                    الاسم = p.Name,
                    الصنف = p.Category?.Name ?? "",
                    الباركود = p.Barcode ?? "",
                    الوصف = p.Description ?? "",
                    تاريخ_الإنشاء = p.CreatedAt.ToString("yyyy/MM/dd")
                });
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المنتجات");
            }

            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    // ── Print ──────────────────────────────────────────────
    [RelayCommand]
    private async Task Print()
    {
        try
        {
            var sizeFilter = ShowSizesSection
                             && !string.IsNullOrWhiteSpace(SelectedSizeFilter)
                             && SelectedSizeFilter != AllFilterLabel
                ? SelectedSizeFilter.Trim()
                : null;
            var colorFilter = ShowColorsSection
                              && !string.IsNullOrWhiteSpace(SelectedColorFilter)
                              && SelectedColorFilter != AllFilterLabel
                ? SelectedColorFilter.Trim()
                : null;
            var (allItems, _) = await _productService.GetPagedAsync(
                1, int.MaxValue,
                SelectedCategory?.Id,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                sizeFilter,
                colorFilter,
                ShowBatchesSection && FilterHasBatchesOnly);

            var printData = allItems.ToList();

            // Build FlowDocument
            var document = new System.Windows.Documents.FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FlowDirection = FlowDirection.RightToLeft,
                PageWidth = 793,
                PagePadding = new Thickness(50),
                ColumnWidth = double.MaxValue
            };

            PrintBrandingFlowDocumentHelper.PrependBrandingHeader(document);

            // Title
            var title = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run("قائمة المنتجات"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(title);

            // Date
            var dateParagraph = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd hh:mm tt}"))
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 0, 0, 15)
            };
            document.Blocks.Add(dateParagraph);

            // Table
            var table = new System.Windows.Documents.Table();
            table.CellSpacing = 0;

            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(50) });   // #
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(180) });  // Name
            if (ShowScientificName)
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(160) }); // Scientific
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(110) });  // Category
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(110) });  // Barcode
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(160) });  // Description

            // Header row
            var headerGroup = new System.Windows.Documents.TableRowGroup();
            var headerRow = new System.Windows.Documents.TableRow();
            headerRow.Background = System.Windows.Media.Brushes.DarkSlateBlue;
            headerRow.Foreground = System.Windows.Media.Brushes.White;

            var headers = ShowScientificName
                ? new[] { "#", "الاسم", "الاسم العلمي", "الصنف", "الباركود", "الوصف" }
                : new[] { "#", "الاسم", "الصنف", "الباركود", "الوصف" };
            foreach (var h in headers)
            {
                var cell = new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(h))
                    { FontWeight = FontWeights.Bold, FontSize = 12 });
                cell.Padding = new Thickness(6, 4, 6, 4);
                cell.BorderThickness = new Thickness(0.5);
                cell.BorderBrush = System.Windows.Media.Brushes.Gray;
                headerRow.Cells.Add(cell);
            }
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // Data rows
            var dataGroup = new System.Windows.Documents.TableRowGroup();
            int index = 1;
            foreach (var p in printData)
            {
                var row = new System.Windows.Documents.TableRow();
                if (index % 2 == 0)
                    row.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(245, 245, 245));

                var values = ShowScientificName
                    ? new[] { index.ToString(), p.Name, p.ScientificName ?? "", p.Category?.Name ?? "", p.Barcode ?? "", p.Description ?? "" }
                    : new[] { index.ToString(), p.Name, p.Category?.Name ?? "", p.Barcode ?? "", p.Description ?? "" };
                foreach (var v in values)
                {
                    var cell = new System.Windows.Documents.TableCell(
                        new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(v))
                        { FontSize = 11 });
                    cell.Padding = new Thickness(6, 3, 6, 3);
                    cell.BorderThickness = new Thickness(0.5);
                    cell.BorderBrush = System.Windows.Media.Brushes.LightGray;
                    row.Cells.Add(cell);
                }
                dataGroup.Rows.Add(row);
                index++;
            }
            table.RowGroups.Add(dataGroup);
            document.Blocks.Add(table);

            // Total count
            var totalParagraph = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"إجمالي المنتجات: {printData.Count}"))
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 0)
            };
            document.Blocks.Add(totalParagraph);

            PrintBrandingFlowDocumentHelper.AppendBrandingFooter(document, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

            DocumentPrintHelper.PrintWithPreview(document, "طباعة المنتجات");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value)
    {
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.Products, value);
        if (_isInitializing) return;
        if (value)
            _ = RebuildProductCardsAsync(Products);
        else
            ProductCards.Clear();
    }

    [RelayCommand]
    private void OpenEditPriceDialog(ProductPriceCardLine? line)
    {
        if (line is null || !_pricingEnabled) return;
        _editingPriceProductId = line.ProductId;
        _editingProductPriceId = line.ProductPriceId;
        PriceEditProductName = Products.FirstOrDefault(p => p.Id == line.ProductId)?.Name
                               ?? ProductCards.FirstOrDefault(c => c.Product.Id == line.ProductId)?.Name
                               ?? "";
        EditPricingType = PricingTypes.FirstOrDefault(t => t.Id == line.PricingTypeId);
        EditSalePrice = line.SalePrice;
        EditPurchasePrice = line.PurchasePrice;
        PriceEditError = string.Empty;
        IsPriceEditDialogOpen = true;
    }

    [RelayCommand]
    private void OpenAddPriceDialog(ProductCardDisplay? card)
    {
        if (card is null || !_pricingEnabled) return;
        _editingPriceProductId = card.Product.Id;
        _editingProductPriceId = null;
        PriceEditProductName = card.Name;
        EditPricingType = PricingTypes.FirstOrDefault(t => t.IsDefault) ?? PricingTypes.FirstOrDefault();
        EditSalePrice = 0;
        EditPurchasePrice = 0;
        PriceEditError = string.Empty;
        IsPriceEditDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveProductPrice()
    {
        if (_editingPriceProductId is null || EditPricingType is null)
        {
            PriceEditError = "اختر نوع التسعير";
            return;
        }

        try
        {
            await _productPriceService.UpsertAsync(new ProductPrice
            {
                Id = _editingProductPriceId ?? 0,
                ProductId = _editingPriceProductId.Value,
                PricingTypeId = EditPricingType.Id,
                SalePrice = EditSalePrice,
                PurchasePrice = EditPurchasePrice
            });
            IsPriceEditDialogOpen = false;
            await LoadProductsAsync();
            BeautifulMessageDialog.ShowSuccess("تم حفظ السعر");
        }
        catch (Exception ex)
        {
            PriceEditError = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelPriceEdit() => IsPriceEditDialogOpen = false;

    [RelayCommand]
    private async Task OpenEditProductFromCard(ProductCardDisplay? card)
    {
        if (card?.Product is not null)
            await OpenEditDialog(card.Product);
    }

    [RelayCommand]
    private void ConfirmDeleteFromCard(ProductCardDisplay? card)
    {
        if (card?.Product is not null)
            ConfirmDelete(card.Product);
    }
}
