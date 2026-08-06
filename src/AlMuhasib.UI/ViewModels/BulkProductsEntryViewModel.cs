using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class BulkProductsEntryViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductPriceService _productPriceService;
    private readonly ICustomFieldSettingsService _customFieldSettings;
    private readonly IFeatureFlagService _featureFlags;
    private readonly MainWindowViewModel _mainWindow;
    private HashSet<string> _existingNames = new(StringComparer.OrdinalIgnoreCase);
    private int? _defaultPricingTypeId;

    public ObservableCollection<BulkProductEntryRow> Rows { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<string> CategoryNameOptions { get; } = [];
    public IReadOnlyList<string> WeightUnitOptions { get; } = ["كغ", "غرام", "لتر", "مل", "متر", "سم"];
    public IReadOnlyList<string> DiscountTypeOptions { get; } = ["بدون", "نسبة مئوية", "قيمة ثابتة"];

    [ObservableProperty] private bool _showPharmacyFields;
    [ObservableProperty] private bool _showWeightFields;
    [ObservableProperty] private bool _showDiscountFields;
    [ObservableProperty] private bool _showPricingFields;

    [ObservableProperty] private bool _showCustomField1;
    [ObservableProperty] private bool _showCustomField2;
    [ObservableProperty] private bool _showCustomField3;
    [ObservableProperty] private bool _showCustomField4;
    [ObservableProperty] private bool _showCustomField5;
    [ObservableProperty] private bool _showCustomField6;
    [ObservableProperty] private bool _showCustomField7;
    [ObservableProperty] private bool _showCustomField8;

    [ObservableProperty] private string _customField1Header = "حقل 1";
    [ObservableProperty] private string _customField2Header = "حقل 2";
    [ObservableProperty] private string _customField3Header = "حقل 3";
    [ObservableProperty] private string _customField4Header = "حقل 4";
    [ObservableProperty] private string _customField5Header = "حقل 5";
    [ObservableProperty] private string _customField6Header = "حقل 6";
    [ObservableProperty] private string _customField7Header = "حقل 7";
    [ObservableProperty] private string _customField8Header = "حقل 8";

    [ObservableProperty] private int _totalRows;
    [ObservableProperty] private int _readyRows;
    [ObservableProperty] private int _incompleteRows;
    [ObservableProperty] private int _duplicateRows;

    [ObservableProperty] private string _statusMessage = string.Empty;

    private List<(int Slot, string Label)> _enabledCustomSlots = [];

    public BulkProductsEntryViewModel(
        IProductService productService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IProductPriceService productPriceService,
        ICustomFieldSettingsService customFieldSettings,
        IFeatureFlagService featureFlags,
        MainWindowViewModel mainWindow)
    {
        _productService = productService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _productPriceService = productPriceService;
        _customFieldSettings = customFieldSettings;
        _featureFlags = featureFlags;
        _mainWindow = mainWindow;
        PageTitle = "إضافة منتجات متعددة";
        Rows.CollectionChanged += OnRowsCollectionChanged;
        EnsureBlankRows(8);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Products");
        RefreshFeatureVisibility();
        await LoadLookupsAsync();
        RecalculateStats();
    }

    private void RefreshFeatureVisibility()
    {
        var flags = _featureFlags.Current;
        ShowPharmacyFields = flags.TemplatePharmacy;
        ShowWeightFields = flags.MenuWeight;
        ShowDiscountFields = flags.ProductDiscountEnabled;
        ShowPricingFields = flags.ProductPricingEnabled;
    }

    private async Task LoadLookupsAsync()
    {
        Categories.Clear();
        CategoryNameOptions.Clear();
        var categories = (await _unitOfWork.Categories.GetAllAsync()).OrderBy(c => c.Name).ToList();
        foreach (var c in categories)
        {
            Categories.Add(c);
            CategoryNameOptions.Add(c.Name);
        }

        var products = await _unitOfWork.Products.GetAllAsync();
        _existingNames = products
            .Select(p => p.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (ShowPricingFields)
        {
            var pricingTypes = await _unitOfWork.PricingTypes.GetAllAsync();
            _defaultPricingTypeId = pricingTypes
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefault()?.Id;
        }

        var enabled = await _customFieldSettings.GetEnabledDefinitionsAsync(CustomFieldEntityKind.Products);
        _enabledCustomSlots = enabled.Select(d => (d.Slot, d.DisplayLabel)).ToList();
        ApplyCustomFieldVisibility();
    }

    private void ApplyCustomFieldVisibility()
    {
        ShowCustomField1 = ShowCustomField2 = ShowCustomField3 = ShowCustomField4 =
            ShowCustomField5 = ShowCustomField6 = ShowCustomField7 = ShowCustomField8 = false;

        foreach (var (slot, label) in _enabledCustomSlots)
        {
            switch (slot)
            {
                case 1: ShowCustomField1 = true; CustomField1Header = label; break;
                case 2: ShowCustomField2 = true; CustomField2Header = label; break;
                case 3: ShowCustomField3 = true; CustomField3Header = label; break;
                case 4: ShowCustomField4 = true; CustomField4Header = label; break;
                case 5: ShowCustomField5 = true; CustomField5Header = label; break;
                case 6: ShowCustomField6 = true; CustomField6Header = label; break;
                case 7: ShowCustomField7 = true; CustomField7Header = label; break;
                case 8: ShowCustomField8 = true; CustomField8Header = label; break;
            }
        }
    }

    private void EnsureBlankRows(int count)
    {
        while (Rows.Count < count)
            AddRowInternal(new BulkProductEntryRow());
    }

    private void AddRowInternal(BulkProductEntryRow row)
    {
        row.RowChanged += OnRowChanged;
        Rows.Add(row);
    }

    private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (BulkProductEntryRow row in e.NewItems)
                row.RowChanged += OnRowChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (BulkProductEntryRow row in e.OldItems)
                row.RowChanged -= OnRowChanged;
        }

        RecalculateStats();
    }

    private void OnRowChanged(BulkProductEntryRow row) => RecalculateStats();

    private void RecalculateStats()
    {
        var named = Rows.Where(r => r.HasName).ToList();
        TotalRows = named.Count;
        ReadyRows = named.Count(r => r.IsReadyToSave);
        IncompleteRows = Rows.Count(r => !r.HasName && HasAnyOtherData(r));

        var nameGroups = named
            .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();
        var inTableDupes = nameGroups.Where(g => g.Count() > 1).Sum(g => g.Count());
        var dbDupes = named.Count(r => _existingNames.Contains(r.Name.Trim()));
        DuplicateRows = inTableDupes + dbDupes;

        foreach (var row in Rows)
        {
            if (!row.HasName)
            {
                row.RowStatus = HasAnyOtherData(row) ? "ناقص الاسم" : "";
                continue;
            }

            var name = row.Name.Trim();
            if (_existingNames.Contains(name))
                row.RowStatus = "موجود مسبقاً";
            else if (named.Count(r => r.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) > 1)
                row.RowStatus = "مكرر في الجدول";
            else
                row.RowStatus = "جاهز";
        }
    }

    private static bool HasAnyOtherData(BulkProductEntryRow row) =>
        !string.IsNullOrWhiteSpace(row.Barcode)
        || !string.IsNullOrWhiteSpace(row.CategoryName)
        || !string.IsNullOrWhiteSpace(row.Description)
        || !string.IsNullOrWhiteSpace(row.ScientificName)
        || row.Weight != 0
        || row.SalePrice != 0
        || row.PurchasePrice != 0;

    [RelayCommand]
    private void AddEmptyRows()
    {
        for (var i = 0; i < 5; i++)
            AddRowInternal(new BulkProductEntryRow());
        RecalculateStats();
    }

    [RelayCommand]
    private void ClearRows()
    {
        if (!BeautifulMessageDialog.ShowConfirm("مسح جميع الصفوف؟", "تأكيد"))
            return;

        Rows.Clear();
        EnsureBlankRows(8);
        RecalculateStats();
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة منتجات");
            return;
        }

        RecalculateStats();
        var candidates = Rows
            .Where(r => r.HasName)
            .Where(r => !_existingNames.Contains(r.Name.Trim()))
            .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (candidates.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد صفوف جاهزة للحفظ (تحقق من الأسماء والمكررات)");
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"سيتم حفظ {candidates.Count} منتج.\nهل تريد المتابعة؟",
                "تأكيد الحفظ"))
            return;

        IsBusy = true;
        StatusMessage = "جاري الحفظ...";
        var saved = 0;
        var errors = new List<string>();

        try
        {
            var defaultCategory = Categories.FirstOrDefault(c =>
                    c.Name.Equals("عام", StringComparison.OrdinalIgnoreCase))
                ?? Categories.FirstOrDefault();

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
                Categories.Add(defaultCategory);
                CategoryNameOptions.Add(defaultCategory.Name);
            }

            foreach (var row in candidates)
            {
                try
                {
                    var catName = row.CategoryName?.Trim();
                    Category? category;
                    if (string.IsNullOrEmpty(catName))
                    {
                        category = defaultCategory;
                    }
                    else
                    {
                        category = Categories.FirstOrDefault(c =>
                            c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase));
                        if (category is null)
                        {
                            category = new Category
                            {
                                Name = catName,
                                CreatedBy = _currentUserService.Username,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _unitOfWork.Categories.AddAsync(category);
                            await _unitOfWork.SaveChangesAsync();
                            Categories.Add(category);
                            if (!CategoryNameOptions.Contains(category.Name))
                                CategoryNameOptions.Add(category.Name);
                        }
                    }

                    var product = new Product
                    {
                        Name = row.Name.Trim(),
                        Barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode.Trim(),
                        Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
                        CategoryId = category.Id,
                        CustomFieldsJson = row.BuildCustomFieldsJson(_enabledCustomSlots)
                    };

                    if (ShowPharmacyFields)
                    {
                        product.ScientificName = string.IsNullOrWhiteSpace(row.ScientificName)
                            ? null : row.ScientificName.Trim();
                        product.UsageInstructions = string.IsNullOrWhiteSpace(row.UsageInstructions)
                            ? null : row.UsageInstructions.Trim();
                    }

                    if (ShowWeightFields)
                    {
                        product.Weight = row.Weight;
                        product.WeightUnit = string.IsNullOrWhiteSpace(row.WeightUnit)
                            ? null : row.WeightUnit.Trim();
                    }

                    if (ShowDiscountFields)
                    {
                        product.DiscountType = row.ParseDiscountType();
                        product.DiscountValue = row.DiscountValue;
                        product.DiscountExpiresAt = row.ParseDiscountExpiry();
                    }

                    await _productService.CreateAsync(product);
                    _existingNames.Add(product.Name);

                    if (ShowPricingFields
                        && _defaultPricingTypeId is int pricingTypeId
                        && (row.SalePrice > 0 || row.PurchasePrice > 0))
                    {
                        await _productPriceService.UpsertAsync(new ProductPrice
                        {
                            ProductId = product.Id,
                            PricingTypeId = pricingTypeId,
                            SalePrice = row.SalePrice,
                            PurchasePrice = row.PurchasePrice
                        });
                    }

                    saved++;
                    row.RowStatus = "تم الحفظ";
                }
                catch (Exception ex)
                {
                    errors.Add($"{row.Name}: {ex.Message}");
                    row.RowStatus = "خطأ";
                }
            }

            var msg = $"تم حفظ {saved} منتج.";
            if (errors.Count > 0)
                msg += $"\nأخطاء: {errors.Count}\n" + string.Join("\n", errors.Take(6));

            StatusMessage = msg;
            if (errors.Count > 0 && saved == 0)
                BeautifulMessageDialog.ShowError(msg);
            else
                BeautifulMessageDialog.ShowSuccess(msg);

            // أزل الصفوف المحفوظة وأبقِ الباقي
            var remaining = Rows.Where(r => r.RowStatus != "تم الحفظ").ToList();
            Rows.Clear();
            foreach (var r in remaining)
                AddRowInternal(r);
            EnsureBlankRows(5);
            RecalculateStats();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الحفظ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Close() => _mainWindow.CloseTabForViewModel(this);
}
