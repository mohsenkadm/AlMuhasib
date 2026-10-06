using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.CustomFields;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductFormViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly IProductPriceService _productPriceService;
    private readonly IProductUnitService _productUnitService;
    private readonly IProductSizeService _productSizeService;
    private readonly IProductColorService _productColorService;
    private readonly IPackagingTypeService _packagingTypeService;
    private readonly IPricingTypeService _pricingTypeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBranchService _branchService;
    private readonly IBranchContext _branchContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ICustomFieldSettingsService _customFieldSettings;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IToastNotificationService _toast;
    private readonly MainWindowViewModel _mainWindow;

    private int? _editingProductId;
    private bool _pricingEnabled;

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<ProductFormPriceRow> PriceRows { get; } = [];
    public ObservableCollection<ProductFormStockRow> StockRows { get; } = [];
    public ObservableCollection<ProductFormUnitRow> UnitRows { get; } = [];
    public ObservableCollection<PackagingType> AvailablePackagingTypes { get; } = [];
    public ObservableCollection<ProductBranchOption> BranchOptions { get; } = [];
    public ObservableCollection<CustomFieldEditItem> CustomFields { get; } = [];
    public ObservableCollection<string> PendingSizes { get; } = [];
    public ObservableCollection<string> PendingColors { get; } = [];

    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _formTitle = "إضافة منتج جديد";
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private bool _isSaving;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _barcode = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private string _scientificName = string.Empty;
    [ObservableProperty] private string _usageInstructions = string.Empty;
    [ObservableProperty] private string _vehicleType = string.Empty;
    [ObservableProperty] private string _chassisNumber = string.Empty;
    [ObservableProperty] private string _carModel = string.Empty;
    [ObservableProperty] private string _vehicleColor = string.Empty;
    [ObservableProperty] private string _passengerCountText = string.Empty;
    [ObservableProperty] private string _plateNumber = string.Empty;
    [ObservableProperty] private VehiclePlateType _plateType = VehiclePlateType.None;
    [ObservableProperty] private VehiclePlateTypeOption? _plateTypeOption;
    [ObservableProperty] private decimal _weight;
    [ObservableProperty] private string _weightUnit = "كغ";
    [ObservableProperty] private DiscountType _discountType = DiscountType.None;
    [ObservableProperty] private DiscountTypeOption? _discountTypeOption;
    [ObservableProperty] private decimal _discountValue;
    [ObservableProperty] private bool _discountHasExpiry;
    [ObservableProperty] private DateTime? _discountExpiresAt;

    [ObservableProperty] private bool _showPricingSection;
    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private bool _showUnitsSection;
    [ObservableProperty] private bool _showWeightSection;
    [ObservableProperty] private bool _showDiscountSection;
    [ObservableProperty] private bool _showSizesSection;
    [ObservableProperty] private bool _showColorsSection;
    [ObservableProperty] private bool _showScientificName;
    [ObservableProperty] private bool _showUsageInstructions;
    [ObservableProperty] private bool _showCarShowroomFields;
    [ObservableProperty] private bool _hasCustomFields;

    [ObservableProperty] private string _selectedBranchesSummary = "الفرع الرئيسي";
    [ObservableProperty] private bool _isBranchPickerOpen;
    [ObservableProperty] private bool _isQuickCategoryOpen;
    [ObservableProperty] private string _quickCategoryName = string.Empty;
    [ObservableProperty] private string _quickCategoryError = string.Empty;
    [ObservableProperty] private bool _isQuickPricingTypeOpen;
    [ObservableProperty] private string _quickPricingTypeName = string.Empty;
    [ObservableProperty] private string _quickPricingTypeError = string.Empty;
    [ObservableProperty] private bool _isQuickWarehouseOpen;
    [ObservableProperty] private string _quickWarehouseName = string.Empty;
    [ObservableProperty] private string _quickWarehouseLocation = string.Empty;
    [ObservableProperty] private ProductBranchOption? _quickWarehouseBranch;
    [ObservableProperty] private string _quickWarehouseError = string.Empty;
    [ObservableProperty] private bool _isQuickPackagingOpen;
    [ObservableProperty] private string _quickPackagingName = string.Empty;
    [ObservableProperty] private decimal _quickPackagingFactor = 1m;
    [ObservableProperty] private string _quickPackagingError = string.Empty;
    [ObservableProperty] private PackagingType? _selectedPackagingToAdd;
    [ObservableProperty] private decimal _newUnitFactor = 1m;
    [ObservableProperty] private string _newSizeName = string.Empty;
    [ObservableProperty] private string _newColorName = string.Empty;

    public IReadOnlyList<string> WeightUnitOptions { get; } =
        ["كغ", "غرام", "لتر", "مل", "متر", "سم"];

    public IReadOnlyList<DiscountTypeOption> DiscountTypeOptions { get; } =
    [
        new(DiscountType.None, "بدون خصم"),
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة (د.ع لكل وحدة)")
    ];

    public IReadOnlyList<VehiclePlateTypeOption> PlateTypeOptions { get; } =
    [
        new(VehiclePlateType.None, "بدون"),
        new(VehiclePlateType.Inspection, "فحص"),
        new(VehiclePlateType.Official, "رسمي")
    ];

    public ProductFormViewModel(
        IProductService productService,
        IProductPriceService productPriceService,
        IProductUnitService productUnitService,
        IProductSizeService productSizeService,
        IProductColorService productColorService,
        IPackagingTypeService packagingTypeService,
        IPricingTypeService pricingTypeService,
        IUnitOfWork unitOfWork,
        IBranchService branchService,
        IBranchContext branchContext,
        ICurrentUserService currentUserService,
        IFeatureFlagService featureFlags,
        ICustomFieldSettingsService customFieldSettings,
        IUserPreferencesService userPreferences,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _productService = productService;
        _productPriceService = productPriceService;
        _productUnitService = productUnitService;
        _productSizeService = productSizeService;
        _productColorService = productColorService;
        _packagingTypeService = packagingTypeService;
        _pricingTypeService = pricingTypeService;
        _unitOfWork = unitOfWork;
        _branchService = branchService;
        _branchContext = branchContext;
        _currentUserService = currentUserService;
        _featureFlags = featureFlags;
        _customFieldSettings = customFieldSettings;
        _userPreferences = userPreferences;
        _toast = toast;
        _mainWindow = mainWindow;

        PageTitle = "إضافة منتج";
        DiscountTypeOption = DiscountTypeOptions[0];
        PlateTypeOption = PlateTypeOptions[0];
        RefreshFeatureVisibility();
        _featureFlags.FlagsChanged += (_, _) =>
            FeatureUiRefresh.Invoke(RefreshFeatureVisibility);
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Products");
            RefreshFeatureVisibility();
            await LoadCategoriesAsync();
            await LoadBranchesAsync();
            await ResetCustomFieldsAsync(null);

            if (ProductFormNavigationBridge.PendingEditProductId is int editId)
            {
                ProductFormNavigationBridge.PendingEditProductId = null;
                await LoadProductForEditAsync(editId);
            }
            else
            {
                await PrepareNewProductAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshFeatureVisibility()
    {
        _pricingEnabled = _userPreferences.Current.FeatureFlags.ProductPricingEnabled
                          || _featureFlags.ProductPricingEnabled;
        ShowPricingSection = _pricingEnabled;
        ShowMultiCurrency = _featureFlags.MultiCurrency && _pricingEnabled;
        ShowUnitsSection = _featureFlags.UnitsOfMeasure;
        ShowWeightSection = _featureFlags.MenuWeight;
        ShowDiscountSection = _featureFlags.ProductDiscountEnabled;
        ShowSizesSection = _featureFlags.TemplateClothing;
        ShowColorsSection = _featureFlags.TemplateClothing;
        ShowScientificName = _featureFlags.TemplatePharmacy;
        ShowUsageInstructions = _featureFlags.TemplatePharmacy;
        ShowCarShowroomFields = _featureFlags.CarShowroom;

        foreach (var row in PriceRows)
            row.ShowUsd = ShowMultiCurrency;
    }

    private async Task LoadCategoriesAsync()
    {
        Categories.Clear();
        foreach (var c in (await _unitOfWork.Categories.GetAllAsync()).OrderBy(c => c.Name))
            Categories.Add(c);
    }

    private async Task LoadBranchesAsync()
    {
        BranchOptions.Clear();
        var branches = await _branchService.GetActiveAsync();
        foreach (var b in branches.OrderByDescending(x => x.IsMain).ThenBy(x => x.Name))
        {
            var opt = new ProductBranchOption
            {
                BranchId = b.Id,
                Name = b.Name,
                Code = b.Code,
                IsMain = b.IsMain
            };
            opt.SelectionChanged += RefreshSelectedBranchesSummary;
            BranchOptions.Add(opt);
        }
    }

    private async Task PrepareNewProductAsync()
    {
        _editingProductId = null;
        IsEditMode = false;
        FormTitle = "إضافة منتج جديد";
        PageTitle = "إضافة منتج";
        ClearBasicFields();

        foreach (var opt in BranchOptions)
            opt.SetSelectedSilent(opt.IsMain || BranchOptions.Count == 1);
        if (!BranchOptions.Any(o => o.IsSelected) && BranchOptions.Count > 0)
            BranchOptions[0].SetSelectedSilent(true);
        RefreshSelectedBranchesSummary();

        await LoadPriceRowsAsync(null);
        await ReloadStockRowsAsync();
        UnitRows.Clear();
        PendingSizes.Clear();
        PendingColors.Clear();
        if (ShowUnitsSection)
            await EnsurePackagingTypesAsync();
        await ResetCustomFieldsAsync(null);
    }

    private async Task LoadProductForEditAsync(int productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product is null)
        {
            BeautifulMessageDialog.ShowError("المنتج غير موجود");
            await PrepareNewProductAsync();
            return;
        }

        _editingProductId = product.Id;
        IsEditMode = true;
        FormTitle = $"تعديل المنتج — {product.Name}";
        PageTitle = "تعديل منتج";

        Name = product.Name;
        Description = product.Description ?? string.Empty;
        Barcode = product.Barcode ?? string.Empty;
        ScientificName = product.ScientificName ?? string.Empty;
        UsageInstructions = product.UsageInstructions ?? string.Empty;
        VehicleType = product.VehicleType ?? string.Empty;
        ChassisNumber = product.ChassisNumber ?? string.Empty;
        CarModel = product.CarModel ?? string.Empty;
        VehicleColor = product.VehicleColor ?? string.Empty;
        PassengerCountText = product.PassengerCount?.ToString() ?? string.Empty;
        PlateNumber = product.PlateNumber ?? string.Empty;
        PlateType = product.PlateType;
        PlateTypeOption = PlateTypeOptions.FirstOrDefault(o => o.Type == product.PlateType) ?? PlateTypeOptions[0];
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        Weight = product.Weight;
        WeightUnit = string.IsNullOrWhiteSpace(product.WeightUnit) ? "كغ" : product.WeightUnit!;
        DiscountType = product.DiscountType;
        DiscountTypeOption = DiscountTypeOptions.FirstOrDefault(o => o.Type == product.DiscountType) ?? DiscountTypeOptions[0];
        DiscountValue = product.DiscountValue;
        DiscountHasExpiry = product.DiscountExpiresAt.HasValue;
        DiscountExpiresAt = product.DiscountExpiresAt?.ToLocalTime().Date;

        var branchIds = await _productService.GetBranchIdsForProductAsync(product.Id);
        foreach (var opt in BranchOptions)
            opt.SetSelectedSilent(branchIds.Contains(opt.BranchId));
        if (!BranchOptions.Any(o => o.IsSelected))
        {
            foreach (var opt in BranchOptions)
                opt.SetSelectedSilent(opt.IsMain || BranchOptions.Count == 1);
        }
        RefreshSelectedBranchesSummary();

        await LoadPriceRowsAsync(product.Id);
        await ReloadStockRowsAsync();
        await LoadUnitsAsync(product.Id);
        await LoadSizesColorsAsync(product.Id);
        await ResetCustomFieldsAsync(product.CustomFieldsJson);
    }

    private void ClearBasicFields()
    {
        Name = string.Empty;
        Description = string.Empty;
        Barcode = string.Empty;
        ScientificName = string.Empty;
        UsageInstructions = string.Empty;
        VehicleType = string.Empty;
        ChassisNumber = string.Empty;
        CarModel = string.Empty;
        VehicleColor = string.Empty;
        PassengerCountText = string.Empty;
        PlateNumber = string.Empty;
        PlateType = VehiclePlateType.None;
        PlateTypeOption = PlateTypeOptions[0];
        SelectedCategory = null;
        Weight = 0;
        WeightUnit = "كغ";
        DiscountType = DiscountType.None;
        DiscountTypeOption = DiscountTypeOptions[0];
        DiscountValue = 0;
        DiscountHasExpiry = false;
        DiscountExpiresAt = null;
        FormError = string.Empty;
    }

    private async Task ResetCustomFieldsAsync(string? json)
    {
        CustomFields.Clear();
        var enabled = await _customFieldSettings.GetEnabledDefinitionsAsync(CustomFieldEntityKind.Products);
        foreach (var item in CustomFieldEditFactory.CreateEditItems(enabled, json))
            CustomFields.Add(item);
        HasCustomFields = CustomFields.Count > 0;
    }

    private void RefreshSelectedBranchesSummary()
    {
        var selected = BranchOptions.Where(o => o.IsSelected).ToList();
        SelectedBranchesSummary = selected.Count == 0
            ? "يجب اختيار فرع واحد على الأقل"
            : string.Join(" · ", selected.Select(o => o.Name));
        _ = ReloadStockRowsAsync();
    }

    private async Task LoadPriceRowsAsync(int? productId)
    {
        PriceRows.Clear();
        if (!ShowPricingSection) return;

        await _pricingTypeService.EnsureDefaultExistsAsync();
        var types = await _pricingTypeService.GetActiveAsync();
        var existing = productId is int pid
            ? (await _productPriceService.GetByProductIdAsync(pid)).ToDictionary(p => p.PricingTypeId)
            : new Dictionary<int, ProductPrice>();

        foreach (var t in types.OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name))
        {
            existing.TryGetValue(t.Id, out var price);
            PriceRows.Add(new ProductFormPriceRow
            {
                ProductPriceId = price?.Id,
                PricingTypeId = t.Id,
                PricingTypeName = t.Name,
                IsDefault = t.IsDefault,
                SalePrice = price?.SalePrice ?? 0,
                PurchasePrice = price?.PurchasePrice ?? 0,
                SalePriceUsd = price?.SalePriceUsd ?? 0,
                PurchasePriceUsd = price?.PurchasePriceUsd ?? 0,
                ShowUsd = ShowMultiCurrency
            });
        }
    }

    private async Task ReloadStockRowsAsync()
    {
        var branchIds = BranchOptions.Where(o => o.IsSelected).Select(o => o.BranchId).ToList();
        if (branchIds.Count == 0 && _branchContext.CurrentBranchId is int current)
            branchIds.Add(current);

        StockRows.Clear();
        var rows = await _productService.GetWarehouseStockRowsForBranchesAsync(_editingProductId, branchIds);
        foreach (var r in rows)
        {
            StockRows.Add(new ProductFormStockRow
            {
                WarehouseId = r.WarehouseId,
                BranchId = r.BranchId,
                WarehouseStockId = r.StockId,
                WarehouseName = r.WarehouseName,
                BranchName = r.BranchName,
                Quantity = r.Quantity,
                MinQuantity = r.MinQuantity
            });
        }
    }

    private async Task EnsurePackagingTypesAsync()
    {
        await _packagingTypeService.EnsureDefaultExistsAsync();
        AvailablePackagingTypes.Clear();
        foreach (var t in await _packagingTypeService.GetActiveAsync())
            AvailablePackagingTypes.Add(t);
        SelectedPackagingToAdd ??= AvailablePackagingTypes.FirstOrDefault(t => t.IsDefault)
            ?? AvailablePackagingTypes.FirstOrDefault();
    }

    private async Task LoadUnitsAsync(int productId)
    {
        UnitRows.Clear();
        if (!ShowUnitsSection) return;
        await EnsurePackagingTypesAsync();
        foreach (var u in await _productUnitService.GetByProductAsync(productId))
        {
            UnitRows.Add(new ProductFormUnitRow
            {
                ProductUnitId = u.Id,
                PackagingTypeId = u.PackagingTypeId,
                UnitName = u.UnitName,
                ConversionFactor = u.ConversionFactor,
                IsDefault = u.IsDefault
            });
        }
    }

    private async Task LoadSizesColorsAsync(int productId)
    {
        PendingSizes.Clear();
        PendingColors.Clear();
        if (ShowSizesSection)
        {
            foreach (var s in (await _productSizeService.GetByProductAsync(productId)).OrderBy(x => x.SizeName))
                PendingSizes.Add(s.SizeName);
        }
        if (ShowColorsSection)
        {
            foreach (var c in (await _productColorService.GetByProductAsync(productId)).OrderBy(x => x.ColorName))
                PendingColors.Add(c.ColorName);
        }
    }

    partial void OnDiscountTypeOptionChanged(DiscountTypeOption? value)
    {
        if (value is not null)
            DiscountType = value.Type;
    }

    partial void OnPlateTypeOptionChanged(VehiclePlateTypeOption? value)
    {
        if (value is not null)
            PlateType = value.Type;
    }

    [RelayCommand]
    private void OpenBranchPicker() => IsBranchPickerOpen = true;

    [RelayCommand]
    private void CloseBranchPicker()
    {
        if (!BranchOptions.Any(o => o.IsSelected))
        {
            BeautifulMessageDialog.ShowWarning("اختر فرعاً واحداً على الأقل");
            return;
        }
        IsBranchPickerOpen = false;
        RefreshSelectedBranchesSummary();
    }

    [RelayCommand]
    private void OpenQuickCategory()
    {
        QuickCategoryName = string.Empty;
        QuickCategoryError = string.Empty;
        IsQuickCategoryOpen = true;
    }

    [RelayCommand]
    private void CancelQuickCategory() => IsQuickCategoryOpen = false;

    [RelayCommand]
    private async Task SaveQuickCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickCategoryName))
        {
            QuickCategoryError = "اسم الصنف مطلوب";
            return;
        }

        try
        {
            var name = QuickCategoryName.Trim();
            if (await _unitOfWork.Categories.AnyAsync(c => c.Name == name))
            {
                QuickCategoryError = "الصنف موجود مسبقاً";
                return;
            }

            var category = new Category
            {
                Name = name,
                CreatedBy = _currentUserService.Username
            };
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            await LoadCategoriesAsync();
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == category.Id)
                               ?? Categories.FirstOrDefault(c => c.Name == name);
            IsQuickCategoryOpen = false;
            _toast.ShowSuccess($"تمت إضافة الصنف «{name}»");
        }
        catch (Exception ex)
        {
            QuickCategoryError = ex.Message;
        }
    }

    [RelayCommand]
    private void OpenQuickPricingType()
    {
        QuickPricingTypeName = string.Empty;
        QuickPricingTypeError = string.Empty;
        IsQuickPricingTypeOpen = true;
    }

    [RelayCommand]
    private void CancelQuickPricingType() => IsQuickPricingTypeOpen = false;

    [RelayCommand]
    private async Task SaveQuickPricingTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickPricingTypeName))
        {
            QuickPricingTypeError = "اسم نوع التسعير مطلوب";
            return;
        }

        try
        {
            var created = await _pricingTypeService.CreateAsync(new PricingType
            {
                Name = QuickPricingTypeName.Trim(),
                IsActive = true,
                IsDefault = false
            });
            IsQuickPricingTypeOpen = false;
            await LoadPriceRowsAsync(_editingProductId);
            var row = PriceRows.FirstOrDefault(r => r.PricingTypeId == created.Id);
            if (row is not null)
                row.ShowUsd = ShowMultiCurrency;
            _toast.ShowSuccess($"تمت إضافة نوع التسعير «{created.Name}»");
        }
        catch (Exception ex)
        {
            QuickPricingTypeError = ex.Message;
        }
    }

    [RelayCommand]
    private void OpenQuickWarehouse()
    {
        QuickWarehouseName = string.Empty;
        QuickWarehouseLocation = string.Empty;
        QuickWarehouseError = string.Empty;
        QuickWarehouseBranch = BranchOptions.FirstOrDefault(o => o.IsSelected && o.IsMain)
            ?? BranchOptions.FirstOrDefault(o => o.IsSelected)
            ?? BranchOptions.FirstOrDefault();
        IsQuickWarehouseOpen = true;
    }

    [RelayCommand]
    private void CancelQuickWarehouse() => IsQuickWarehouseOpen = false;

    [RelayCommand]
    private async Task SaveQuickWarehouseAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickWarehouseName))
        {
            QuickWarehouseError = "اسم المخزن مطلوب";
            return;
        }
        if (QuickWarehouseBranch is null)
        {
            QuickWarehouseError = "اختر الفرع للمخزن";
            return;
        }

        try
        {
            var name = QuickWarehouseName.Trim();
            // إنشاء المخزن في سياق الفرع المختار عبر تجاوز مؤقت إن لزم.
            var warehouse = new Warehouse
            {
                Name = name,
                Location = string.IsNullOrWhiteSpace(QuickWarehouseLocation) ? null : QuickWarehouseLocation.Trim(),
                BranchId = QuickWarehouseBranch.BranchId,
                CreatedBy = _currentUserService.Username
            };

            // إن كان الفرع الحالي هو نفسه نستخدم UnitOfWork العادي؛ وإلا نضيف عبر منتج الخدمة لاحقاً.
            if (_branchContext.CurrentBranchId == QuickWarehouseBranch.BranchId)
            {
                if (await _unitOfWork.Warehouses.AnyAsync(w => w.Name == name))
                {
                    QuickWarehouseError = "اسم المخزن موجود مسبقاً في هذا الفرع";
                    return;
                }
                await _unitOfWork.Warehouses.AddAsync(warehouse);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                // حفظ عبر Upsert بتجاوز العزل — أضف المخزن عبر Db عبر product service pattern
                await AddWarehouseForBranchAsync(warehouse);
            }

            if (!QuickWarehouseBranch.IsSelected)
                QuickWarehouseBranch.IsSelected = true;

            IsQuickWarehouseOpen = false;
            await ReloadStockRowsAsync();
            _toast.ShowSuccess($"تمت إضافة المخزن «{name}»");
        }
        catch (Exception ex)
        {
            QuickWarehouseError = ex.Message;
        }
    }

    private async Task AddWarehouseForBranchAsync(Warehouse warehouse)
    {
        // استخدام ProductService's context factory عبر Upsert بحيلة: نخزّن عبر UnitOfWork مع ضبط BranchId
        // إن رُفضت الكتابة عبر الفروع نبلغ المستخدم.
        try
        {
            warehouse.BranchId = warehouse.BranchId;
            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                "لا يمكن إضافة مخزن لفرع آخر من الجلسة الحالية. اختر الفرع أولاً من شريط الفروع أو أضف المخزن من شاشة المخازن.");
        }
    }

    [RelayCommand]
    private void OpenQuickPackaging()
    {
        QuickPackagingName = string.Empty;
        QuickPackagingFactor = 1m;
        QuickPackagingError = string.Empty;
        IsQuickPackagingOpen = true;
    }

    [RelayCommand]
    private void CancelQuickPackaging() => IsQuickPackagingOpen = false;

    [RelayCommand]
    private async Task SaveQuickPackagingAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickPackagingName))
        {
            QuickPackagingError = "اسم نوع التعبئة مطلوب";
            return;
        }
        if (QuickPackagingFactor <= 0)
        {
            QuickPackagingError = "كمية التعبئة يجب أن تكون أكبر من صفر";
            return;
        }

        try
        {
            var created = await _packagingTypeService.CreateAsync(new PackagingType
            {
                Name = QuickPackagingName.Trim(),
                IsActive = true,
                IsDefault = false
            });
            await EnsurePackagingTypesAsync();
            SelectedPackagingToAdd = AvailablePackagingTypes.FirstOrDefault(t => t.Id == created.Id);
            NewUnitFactor = QuickPackagingFactor;
            AddPendingUnit();
            IsQuickPackagingOpen = false;
            _toast.ShowSuccess($"تمت إضافة نوع التعبئة «{created.Name}»");
        }
        catch (Exception ex)
        {
            QuickPackagingError = ex.Message;
        }
    }

    [RelayCommand]
    private void AddPendingUnit()
    {
        if (!ShowUnitsSection) return;
        if (SelectedPackagingToAdd is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع التعبئة");
            return;
        }

        if (UnitRows.Any(u => u.PackagingTypeId == SelectedPackagingToAdd.Id
                              || string.Equals(u.UnitName, SelectedPackagingToAdd.Name, StringComparison.OrdinalIgnoreCase)))
        {
            BeautifulMessageDialog.ShowWarning("نوع التعبئة مضاف مسبقاً");
            return;
        }

        UnitRows.Add(new ProductFormUnitRow
        {
            PackagingTypeId = SelectedPackagingToAdd.Id,
            UnitName = SelectedPackagingToAdd.Name,
            ConversionFactor = NewUnitFactor <= 0 ? 1m : NewUnitFactor,
            IsDefault = UnitRows.Count == 0
        });
        NewUnitFactor = 1m;
        SelectedPackagingToAdd = AvailablePackagingTypes.FirstOrDefault(t =>
            UnitRows.All(u => u.PackagingTypeId != t.Id));
    }

    [RelayCommand]
    private void RemovePendingUnit(ProductFormUnitRow? row)
    {
        if (row is null) return;
        UnitRows.Remove(row);
        if (UnitRows.Count > 0 && UnitRows.All(u => !u.IsDefault))
            UnitRows[0].IsDefault = true;
    }

    [RelayCommand]
    private void SetDefaultUnit(ProductFormUnitRow? row)
    {
        if (row is null) return;
        foreach (var u in UnitRows)
            u.IsDefault = ReferenceEquals(u, row);
    }

    [RelayCommand]
    private void AddPendingSize()
    {
        if (string.IsNullOrWhiteSpace(NewSizeName)) return;
        var name = NewSizeName.Trim();
        if (PendingSizes.Any(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase)))
            return;
        PendingSizes.Add(name);
        NewSizeName = string.Empty;
    }

    [RelayCommand]
    private void RemovePendingSize(string? size)
    {
        if (size is null) return;
        PendingSizes.Remove(size);
    }

    [RelayCommand]
    private void AddPendingColor()
    {
        if (string.IsNullOrWhiteSpace(NewColorName)) return;
        var name = NewColorName.Trim();
        if (PendingColors.Any(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase)))
            return;
        PendingColors.Add(name);
        NewColorName = string.Empty;
    }

    [RelayCommand]
    private void RemovePendingColor(string? color)
    {
        if (color is null) return;
        PendingColors.Remove(color);
    }

    [RelayCommand]
    private async Task GenerateBarcodeAsync()
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var stamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
            var candidate = $"2{stamp}{Random.Shared.Next(100, 999)}";
            if (candidate.Length > 50) candidate = candidate[..50];
            var existing = await _productService.GetByBarcodeAsync(candidate);
            if (existing is null || existing.Id == _editingProductId)
            {
                Barcode = candidate;
                return;
            }
        }
        BeautifulMessageDialog.ShowWarning("تعذّر إنشاء باركود فريد — حاول مجدداً");
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainWindow.CloseTabForViewModel(this);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;

        if (string.IsNullOrWhiteSpace(Name))
        {
            FormError = "اسم المنتج مطلوب";
            return;
        }
        if (SelectedCategory is null)
        {
            FormError = "يرجى اختيار الصنف";
            return;
        }
        var selectedBranchIds = BranchOptions.Where(o => o.IsSelected).Select(o => o.BranchId).ToList();
        if (selectedBranchIds.Count == 0)
        {
            FormError = "اختر فرعاً واحداً على الأقل لظهور المنتج";
            return;
        }

        if (ShowDiscountSection && DiscountType != DiscountType.None)
        {
            if (DiscountValue <= 0)
            {
                FormError = "أدخل قيمة خصم أكبر من صفر أو اختر بدون خصم";
                return;
            }
            if (DiscountType == DiscountType.Percentage && DiscountValue > 100m)
            {
                FormError = "نسبة الخصم لا تتجاوز 100%";
                return;
            }
            if (DiscountHasExpiry && DiscountExpiresAt is null)
            {
                FormError = "حدد تاريخ انتهاء الخصم أو ألغِ خيار الانتهاء";
                return;
            }
        }

        FormError = string.Empty;
        IsSaving = true;
        try
        {
            var product = IsEditMode && _editingProductId.HasValue
                ? await _productService.GetByIdAsync(_editingProductId.Value)
                  ?? throw new InvalidOperationException("المنتج غير موجود")
                : new Product();

            ApplyFieldsToProduct(product);

            if (IsEditMode)
            {
                await _productService.UpdateAsync(product);
            }
            else
            {
                product = await _productService.CreateAsync(product);
                _editingProductId = product.Id;
            }

            var productId = product.Id;
            await _productService.SetProductBranchesAsync(productId, selectedBranchIds);

            if (ShowPricingSection && PriceRows.Count > 0)
            {
                var prices = PriceRows.Select(r => new ProductPrice
                {
                    Id = r.ProductPriceId ?? 0,
                    ProductId = productId,
                    PricingTypeId = r.PricingTypeId,
                    SalePrice = r.SalePrice,
                    PurchasePrice = r.PurchasePrice,
                    SalePriceUsd = ShowMultiCurrency ? r.SalePriceUsd : 0m,
                    PurchasePriceUsd = ShowMultiCurrency ? r.PurchasePriceUsd : 0m
                }).ToList();
                await _productPriceService.UpsertManyAsync(prices);
            }

            if (StockRows.Count > 0)
            {
                await _productService.UpsertWarehouseStocksAsync(
                    productId,
                    StockRows.Select(s => (s.WarehouseId, s.Quantity, s.MinQuantity)));
            }

            if (ShowUnitsSection)
                await SaveUnitsAsync(productId);

            if (ShowSizesSection || ShowColorsSection)
                await SaveSizesColorsAsync(productId);

            _toast.ShowSuccess(IsEditMode ? "تم تحديث المنتج بنجاح" : "تم حفظ المنتج بنجاح");
            _mainWindow.CloseTabForViewModel(this);

            // حدّث قائمة المنتجات إن كانت مفتوحة
            var productsTab = _mainWindow.OpenTabs.FirstOrDefault(t => t.ViewModel is ProductsViewModel);
            if (productsTab?.ViewModel is ProductsViewModel productsVm)
                await productsVm.ReloadAfterExternalChangeAsync();
        }
        catch (Exception ex)
        {
            FormError = $"حدث خطأ: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ApplyFieldsToProduct(Product product)
    {
        product.Name = Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        product.Barcode = string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim();
        product.CategoryId = SelectedCategory!.Id;
        product.ScientificName = ShowScientificName && !string.IsNullOrWhiteSpace(ScientificName)
            ? ScientificName.Trim() : null;
        product.UsageInstructions = ShowUsageInstructions && !string.IsNullOrWhiteSpace(UsageInstructions)
            ? UsageInstructions.Trim() : null;
        product.Weight = ShowWeightSection ? (Weight < 0 ? 0 : Weight) : 0;
        product.WeightUnit = ShowWeightSection
            ? (string.IsNullOrWhiteSpace(WeightUnit) ? null : WeightUnit.Trim())
            : null;
        product.CustomFieldsJson = CustomFieldEditFactory.SerializeEditItems(CustomFields);

        if (ShowCarShowroomFields)
        {
            product.VehicleType = string.IsNullOrWhiteSpace(VehicleType) ? null : VehicleType.Trim();
            product.ChassisNumber = string.IsNullOrWhiteSpace(ChassisNumber) ? null : ChassisNumber.Trim();
            product.CarModel = string.IsNullOrWhiteSpace(CarModel) ? null : CarModel.Trim();
            product.VehicleColor = string.IsNullOrWhiteSpace(VehicleColor) ? null : VehicleColor.Trim();
            product.PlateNumber = string.IsNullOrWhiteSpace(PlateNumber) ? null : PlateNumber.Trim();
            product.PlateType = PlateType;
            product.PassengerCount = int.TryParse(PassengerCountText, out var pc) ? pc : null;
        }

        if (!ShowDiscountSection || DiscountType == DiscountType.None || DiscountValue <= 0)
        {
            product.DiscountType = DiscountType.None;
            product.DiscountValue = 0;
            product.DiscountExpiresAt = null;
        }
        else
        {
            product.DiscountType = DiscountType;
            product.DiscountValue = Math.Max(0m, DiscountValue);
            product.DiscountExpiresAt = DiscountHasExpiry && DiscountExpiresAt is DateTime d
                ? DateTime.SpecifyKind(d.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime()
                : null;
        }
    }

    private async Task SaveUnitsAsync(int productId)
    {
        var existing = (await _productUnitService.GetByProductAsync(productId)).ToList();
        var keepIds = new HashSet<int>();

        foreach (var row in UnitRows)
        {
            var saved = await _productUnitService.SaveAsync(new ProductUnit
            {
                Id = row.ProductUnitId ?? 0,
                ProductId = productId,
                PackagingTypeId = row.PackagingTypeId,
                UnitName = row.UnitName,
                ConversionFactor = row.ConversionFactor <= 0 ? 1m : row.ConversionFactor,
                IsDefault = row.IsDefault
            });
            keepIds.Add(saved.Id);
            row.ProductUnitId = saved.Id;
        }

        foreach (var old in existing.Where(e => !keepIds.Contains(e.Id)))
            await _productUnitService.DeleteAsync(old.Id);

        var defaultRow = UnitRows.FirstOrDefault(u => u.IsDefault) ?? UnitRows.FirstOrDefault();
        if (defaultRow?.ProductUnitId is int defId)
            await _productUnitService.SetDefaultAsync(productId, defId);
    }

    private async Task SaveSizesColorsAsync(int productId)
    {
        if (ShowSizesSection)
        {
            var existing = (await _productSizeService.GetByProductAsync(productId)).ToList();
            foreach (var old in existing.Where(e => PendingSizes.All(n =>
                         !string.Equals(n, e.SizeName, StringComparison.OrdinalIgnoreCase))))
            {
                await _productSizeService.DeleteAsync(old.Id);
            }
            foreach (var name in PendingSizes)
            {
                if (existing.Any(e => string.Equals(e.SizeName, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                await _productSizeService.SaveAsync(new ProductSize
                {
                    ProductId = productId,
                    SizeName = name
                });
            }
        }

        if (ShowColorsSection)
        {
            var existing = (await _productColorService.GetByProductAsync(productId)).ToList();
            foreach (var old in existing.Where(e => PendingColors.All(n =>
                         !string.Equals(n, e.ColorName, StringComparison.OrdinalIgnoreCase))))
            {
                await _productColorService.DeleteAsync(old.Id);
            }
            foreach (var name in PendingColors)
            {
                if (existing.Any(e => string.Equals(e.ColorName, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                await _productColorService.SaveAsync(new ProductColor
                {
                    ProductId = productId,
                    ColorName = name
                });
            }
        }
    }
}
