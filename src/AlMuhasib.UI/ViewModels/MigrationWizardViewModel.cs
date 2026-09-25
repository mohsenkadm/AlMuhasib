using System.Collections.ObjectModel;
using System.IO;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class MigrationWizardViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInvestorService _investorService;
    private readonly IExpenseService _expenseService;
    private readonly IPricingTypeService _pricingTypeService;
    private readonly IProductPriceService _productPriceService;
    private readonly IOpeningPartyBalanceService _openingPartyBalance;
    private readonly IOpeningCustomerBalanceExcelService _customerBalanceExcel;
    private readonly IOpeningSupplierBalanceExcelService _supplierBalanceExcel;
    private readonly IOpeningInstallmentExcelService _openingInstallmentExcel;
    private readonly IInstallmentService _installmentService;
    private readonly HashSet<MigrationStepKind> _savedSteps = [];
    private bool _needsCapital = true;
    private bool _needsPricingTypes = true;
    private bool _needsProductPricing = true;

    [ObservableProperty] private int _currentStepIndex;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string? _selectedFilePath;
    [ObservableProperty] private int _lastImportedCount;
    [ObservableProperty] private int _lastSkippedCount;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private int _stepTransitionToken;
    [ObservableProperty] private bool _isManualEntryMode = true;
    [ObservableProperty] private bool _isExcelMode;
    [ObservableProperty] private decimal _capitalAmount;
    [ObservableProperty] private DateTime _capitalDate = DateTime.Today;
    [ObservableProperty] private string _capitalNotes = string.Empty;
    [ObservableProperty] private decimal _profitOpeningBalance;
    [ObservableProperty] private string _newRowName = string.Empty;
    [ObservableProperty] private string? _newRowPhone;
    [ObservableProperty] private string? _newRowExtra;
    [ObservableProperty] private decimal _newRowAmount;
    [ObservableProperty] private decimal _newRowAmount2;
    [ObservableProperty] private decimal _newRowAmount3;
    [ObservableProperty] private int _newRowInt = 1;
    [ObservableProperty] private int _newRowInt2;
    [ObservableProperty] private DateTime _newRowDate = DateTime.Today;
    [ObservableProperty] private string _newRowKind = "Cash";
    [ObservableProperty] private string? _selectedWarehouseName;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private double _progressPercent;

    public ObservableCollection<MigrationStepInfo> Steps { get; } = [];
    public ObservableCollection<MigrationNamedBalanceRow> CurrentRows { get; } = [];
    public ObservableCollection<string> AvailableCategories { get; } = [];
    public ObservableCollection<string> AvailableProducts { get; } = [];
    public ObservableCollection<string> AvailablePricingTypes { get; } = [];
    public ObservableCollection<string> AvailableWarehouses { get; } = [];
    public ObservableCollection<string> AccountKinds { get; } = ["قاصة", "مصرف"];

    public int TotalSteps => Steps.Count;
    public MigrationStepInfo? CurrentStepInfo =>
        CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count ? Steps[CurrentStepIndex] : null;
    public MigrationStepKind? CurrentKind => CurrentStepInfo?.Kind;
    public string StepTitle => CurrentStepInfo?.Title ?? "اكتمل";
    public string StepDescription => CurrentStepInfo?.Description ?? "تم إكمال جميع خطوات النقل.";
    public bool CanGoBack => CurrentStepIndex > 0 && !IsCompleted;
    public bool IsLastStep => CurrentStepIndex >= TotalSteps - 1 && TotalSteps > 0;
    public bool CanGoNext => !IsCompleted && !IsBusy;
    public bool CanSkip
    {
        get
        {
            if (IsCompleted || IsBusy || CurrentStepInfo is null) return false;
            if (CurrentKind is MigrationStepKind.Capital or MigrationStepKind.Warehouses) return false;
            if (CurrentKind == MigrationStepKind.PricingTypes && _needsPricingTypes) return false;
            if (CurrentKind == MigrationStepKind.ProductPricing && _needsProductPricing) return false;
            return CurrentStepInfo.IsOptional;
        }
    }
    public bool HasLastResult => LastImportedCount > 0 || LastSkippedCount > 0;
    public bool HasRows => CurrentRows.Count > 0;
    public bool IsCapitalStep => CurrentKind == MigrationStepKind.Capital;
    public bool IsCashStep => CurrentKind == MigrationStepKind.CashAndBank;
    public bool IsInvestorsStep => CurrentKind == MigrationStepKind.Investors;
    public bool IsWarehousesStep => CurrentKind == MigrationStepKind.Warehouses;
    public bool IsCategoriesStep => CurrentKind == MigrationStepKind.Categories;
    public bool IsProductsStep => CurrentKind == MigrationStepKind.Products;
    public bool IsPricingTypesStep => CurrentKind == MigrationStepKind.PricingTypes;
    public bool IsProductPricingStep => CurrentKind == MigrationStepKind.ProductPricing;
    public bool IsCustomersStep => CurrentKind == MigrationStepKind.Customers;
    public bool IsSuppliersStep => CurrentKind == MigrationStepKind.Suppliers;
    public bool IsExpenseTypesStep => CurrentKind == MigrationStepKind.ExpenseTypes;
    public bool IsInstallmentsStep => CurrentKind == MigrationStepKind.Installments;
    public bool IsSimpleNameStep => IsCategoriesStep || IsPricingTypesStep || IsExpenseTypesStep;
    public bool ShowManualEntryForm => IsManualEntryMode && !IsCapitalStep && !IsCompleted;
    public bool ShowExcelPanel => IsExcelMode && !IsCapitalStep && !IsCompleted;
    public bool ShowDataGrid => !IsCapitalStep && HasRows && !IsCompleted;
    public string EntryHint => CurrentKind switch
    {
        MigrationStepKind.CashAndBank => "أدخل اسم القاصة/المصرف والرصيد ثم Enter أو إضافة",
        MigrationStepKind.Investors => "الاسم + الهاتف + نسبة الربح + الرصيد الافتتاحي",
        MigrationStepKind.Warehouses => "اسم المخزن والموقع (اختياري)",
        MigrationStepKind.Categories => "اسم الصنف فقط — يمكن إضافة عدة أصناف بسرعة",
        MigrationStepKind.Products => "المنتج + الصنف + السعر + الكمية + التكلفة",
        MigrationStepKind.PricingTypes => "اسم نوع التسعير (مفرد، جملة، وكيل...)",
        MigrationStepKind.ProductPricing => "اختر المنتج ونوع التسعير وأدخل سعر البيع/الشراء",
        MigrationStepKind.Customers => "اسم العميل + الهاتف + رقم الملف + الرصيد الآجل",
        MigrationStepKind.Suppliers => "اسم المورد + الهاتف + الرصيد الآجل",
        MigrationStepKind.ExpenseTypes => "اسم نوع المصروف",
        MigrationStepKind.Installments => "الزبون + المبلغ الكلي + عدد الأقساط + المسدد",
        _ => "أدخل البيانات ثم احفظ قبل الانتقال"
    };

    public MigrationWizardViewModel(
        IUnitOfWork unitOfWork,
        IInvestorService investorService,
        IExpenseService expenseService,
        IPricingTypeService pricingTypeService,
        IProductPriceService productPriceService,
        IOpeningPartyBalanceService openingPartyBalance,
        IOpeningCustomerBalanceExcelService customerBalanceExcel,
        IOpeningSupplierBalanceExcelService supplierBalanceExcel,
        IOpeningInstallmentExcelService openingInstallmentExcel,
        IInstallmentService installmentService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _investorService = investorService;
        _expenseService = expenseService;
        _pricingTypeService = pricingTypeService;
        _productPriceService = productPriceService;
        _openingPartyBalance = openingPartyBalance;
        _customerBalanceExcel = customerBalanceExcel;
        _supplierBalanceExcel = supplierBalanceExcel;
        _openingInstallmentExcel = openingInstallmentExcel;
        _installmentService = installmentService;
        PageTitle = "معالج النقل من نظام قديم";
        LoadPermissions(currentUserService, "DataImport");
    }

    public override async Task InitializeAsync()
    {
        _needsCapital = !await _unitOfWork.CapitalEntries.AnyAsync();
        await RefreshPricingNeedFlagsAsync();
        BuildSteps();
        await RefreshLookupsAsync();
        UpdateProgress();
        NotifyStepProps();
        await PrepareCurrentStepAsync();
    }

    private async Task RefreshPricingNeedFlagsAsync()
    {
        var types = (await _pricingTypeService.GetActiveAsync()).ToList();
        _needsPricingTypes = types.Count == 0;

        var productIds = (await _unitOfWork.Products.GetAllAsync()).Select(p => p.Id).ToHashSet();
        if (productIds.Count == 0)
        {
            // ستُضاف المنتجات لاحقاً في المعالج — نُبقي خطوة التسعير ظاهرة ومرنة
            _needsProductPricing = true;
            return;
        }

        var pricedProductIds = (await _unitOfWork.ProductPrices.GetAllAsync())
            .Select(p => p.ProductId)
            .ToHashSet();
        _needsProductPricing = productIds.Any(id => !pricedProductIds.Contains(id));
    }

    private void BuildSteps()
    {
        Steps.Clear();
        void Add(MigrationStepKind kind, string shortTitle, string desc, PackIconKind icon, bool optional = true)
            => Steps.Add(new MigrationStepInfo
            {
                Kind = kind,
                ShortTitle = shortTitle,
                Description = desc,
                Icon = icon,
                IsOptional = optional
            });

        if (_needsCapital)
        {
            Add(MigrationStepKind.Capital, "رأس المال",
                "أدخل رأس المال والأرباح الافتتاحية مرة واحدة لبدء النظام.",
                PackIconKind.Cash, optional: false);
        }

        Add(MigrationStepKind.CashAndBank, "القاصات والمصرف",
            "أنشئ القاصات وحسابات المصرف مع أرصدتها الافتتاحية.",
            PackIconKind.SafeSquareOutline);
        Add(MigrationStepKind.Investors, "المستثمرون",
            "أضف المستثمرين مع الرصيد الافتتاحي — يُنشأ الاسم إن لم يكن موجوداً.",
            PackIconKind.AccountCash);
        Add(MigrationStepKind.Warehouses, "المخازن",
            "المخزن مطلوب قبل المنتجات والأرصدة والأقساط.",
            PackIconKind.Warehouse, optional: false);
        Add(MigrationStepKind.Categories, "الأصناف",
            "تصنيفات المنتجات (اختياري — يُنشأ «عام» تلقائياً عند الحاجة).",
            PackIconKind.Shape);
        Add(MigrationStepKind.Products, "المنتجات",
            "المنتجات مع الصنف والسعر المفرد والكمية الافتتاحية والتكلفة.",
            PackIconKind.PackageVariant);

        // أنواع التسعير: إلزامية إن لم تكن موجودة، وإلا اختيارية لإضافة المزيد
        Add(MigrationStepKind.PricingTypes, "أنواع التسعير",
            _needsPricingTypes
                ? "لا توجد أنواع تسعير بعد — أضفها الآن (مثل: سعر مفرد، جملة، وكيل)."
                : "أضف أنواع تسعير إضافية أو تخطَّ إن كانت مكتملة.",
            PackIconKind.TagMultiple,
            optional: !_needsPricingTypes);

        // تسعير المنتجات: إلزامية إن وُجدت منتجات بلا أسعار
        Add(MigrationStepKind.ProductPricing, "تسعير المنتجات",
            _needsProductPricing
                ? "سعّر المنتجات المضافة — يُنشأ النوع الافتراضي تلقائياً إن لم يوجد."
                : "حدّث أسعار المنتجات أو تخطَّ إن كانت مسعّرة مسبقاً.",
            PackIconKind.TagOutline,
            optional: !_needsProductPricing);

        Add(MigrationStepKind.Customers, "العملاء",
            "العملاء مع أرصدة آجلة افتتاحية — يُنشأ العميل تلقائياً.",
            PackIconKind.AccountGroup);
        Add(MigrationStepKind.Suppliers, "الموردون",
            "الموردون مع أرصدة آجلة افتتاحية — يُنشأ المورد تلقائياً.",
            PackIconKind.TruckDelivery);
        Add(MigrationStepKind.ExpenseTypes, "أنواع المصاريف",
            "أنواع المصاريف الافتتاحية للنظام.",
            PackIconKind.CashMinus);
        Add(MigrationStepKind.Installments, "الأقساط",
            "أرصدة الأقساط الافتتاحية بنفس منطق شاشة الأرصدة الافتتاحية.",
            PackIconKind.CashClock);

        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].Title = $"{ToArabicNumeral(i + 1)} — {Steps[i].ShortTitle}";
            Steps[i].IsActive = i == 0;
        }
    }

    private static string ToArabicNumeral(int n) => n switch
    {
        1 => "١", 2 => "٢", 3 => "٣", 4 => "٤", 5 => "٥", 6 => "٦",
        7 => "٧", 8 => "٨", 9 => "٩", 10 => "١٠", 11 => "١١", 12 => "١٢",
        _ => n.ToString()
    };

    partial void OnCurrentStepIndexChanged(int value)
    {
        SyncStepFlags();
        CurrentRows.Clear();
        SelectedFilePath = null;
        LastImportedCount = 0;
        LastSkippedCount = 0;
        StatusMessage = string.Empty;
        ResetNewRowFields();
        StepTransitionToken++;
        UpdateProgress();
        NotifyStepProps();
        _ = PrepareStepAfterNavigationAsync();
    }

    private async Task PrepareStepAfterNavigationAsync()
    {
        await RefreshLookupsAsync();
        await PrepareCurrentStepAsync();
        NotifyStepProps();
    }

    /// <summary>
    /// يجهّز محتوى الخطوة: يقترح أنواع تسعير إن لم توجد، ويملأ جدول تسعير المنتجات للمنتجات بلا أسعار.
    /// </summary>
    private async Task PrepareCurrentStepAsync()
    {
        if (CurrentKind is null || IsCompleted) return;

        try
        {
            if (CurrentKind == MigrationStepKind.PricingTypes)
            {
                await RefreshPricingNeedFlagsAsync();
                UpdatePricingStepOptionality();
                if (CurrentRows.Count == 0 && _needsPricingTypes)
                {
                    foreach (var name in new[] { "سعر مفرد", "جملة", "وكيل" })
                        CurrentRows.Add(new MigrationNamedBalanceRow { Name = name });
                    StatusMessage = "تم اقتراح أنواع تسعير افتراضية — عدّلها ثم احفظ";
                }
            }
            else if (CurrentKind == MigrationStepKind.ProductPricing)
            {
                await _pricingTypeService.EnsureDefaultExistsAsync();
                await RefreshLookupsAsync();
                await RefreshPricingNeedFlagsAsync();
                UpdatePricingStepOptionality();

                if (CurrentRows.Count == 0)
                    await PrefillProductPricingRowsAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذّر تجهيز الخطوة: {ex.Message}";
        }

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ShowDataGrid));
        OnPropertyChanged(nameof(CanSkip));
        OnPropertyChanged(nameof(StepDescription));
    }

    private void UpdatePricingStepOptionality()
    {
        foreach (var step in Steps)
        {
            if (step.Kind == MigrationStepKind.PricingTypes)
            {
                step.IsOptional = !_needsPricingTypes;
                step.Description = _needsPricingTypes
                    ? "لا توجد أنواع تسعير بعد — أضفها الآن (مثل: سعر مفرد، جملة، وكيل)."
                    : "أضف أنواع تسعير إضافية أو تخطَّ إن كانت مكتملة.";
            }
            else if (step.Kind == MigrationStepKind.ProductPricing)
            {
                step.IsOptional = !_needsProductPricing;
                step.Description = _needsProductPricing
                    ? "سعّر المنتجات المضافة — يُنشأ النوع الافتراضي تلقائياً إن لم يوجد."
                    : "حدّث أسعار المنتجات أو تخطَّ إن كانت مسعّرة مسبقاً.";
            }
        }
    }

    private async Task PrefillProductPricingRowsAsync()
    {
        var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
        if (products.Count == 0)
        {
            StatusMessage = "لا توجد منتجات بعد — أضف منتجات في الخطوة السابقة أو تخطَّ";
            return;
        }

        var types = (await _pricingTypeService.GetActiveAsync()).ToList();
        if (types.Count == 0)
        {
            await _pricingTypeService.EnsureDefaultExistsAsync();
            types = (await _pricingTypeService.GetActiveAsync()).ToList();
        }

        var defaultType = types.FirstOrDefault(t => t.IsDefault) ?? types.FirstOrDefault();
        var existingPrices = (await _unitOfWork.ProductPrices.GetAllAsync()).ToList();
        var pricedKeys = existingPrices
            .Select(p => (p.ProductId, p.PricingTypeId))
            .ToHashSet();

        var added = 0;
        foreach (var product in products)
        {
            if (defaultType is null) break;
            if (pricedKeys.Contains((product.Id, defaultType.Id))) continue;

            var existingUnit = existingPrices.FirstOrDefault(p => p.ProductId == product.Id);
            CurrentRows.Add(new MigrationNamedBalanceRow
            {
                Name = product.Name,
                ProductName = product.Name,
                PricingTypeName = defaultType.Name,
                SalePrice = existingUnit?.SalePrice ?? 0,
                Amount = existingUnit?.SalePrice ?? 0,
                PurchasePrice = existingUnit?.PurchasePrice ?? 0,
                UnitCost = existingUnit?.PurchasePrice ?? 0
            });
            added++;
        }

        // إن كانت كل المنتجات مسعّرة للنوع الافتراضي، اعرض صفوفاً فارغة للأنواع الأخرى عند الحاجة
        if (added == 0 && _needsProductPricing == false)
        {
            StatusMessage = "كل المنتجات لديها أسعار للنوع الافتراضي — يمكنك إضافة تسعير لأنواع أخرى";
        }
        else if (added > 0)
        {
            StatusMessage = $"تم تجهيز {added} منتج بدون سعر — أكمل الأسعار ثم احفظ";
            _needsProductPricing = true;
            UpdatePricingStepOptionality();
        }
    }

    private void SyncStepFlags()
    {
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsActive = i == CurrentStepIndex;
            Steps[i].IsCompleted = i < CurrentStepIndex || _savedSteps.Contains(Steps[i].Kind);
            Steps[i].IsSaved = _savedSteps.Contains(Steps[i].Kind);
        }
    }

    partial void OnIsManualEntryModeChanged(bool value)
    {
        if (value) IsExcelMode = false;
        NotifyStepProps();
    }

    partial void OnIsExcelModeChanged(bool value)
    {
        if (value) IsManualEntryMode = false;
        NotifyStepProps();
    }

    private void UpdateProgress()
    {
        if (TotalSteps == 0)
        {
            ProgressText = string.Empty;
            ProgressPercent = 0;
            return;
        }

        var done = _savedSteps.Count;
        ProgressPercent = Math.Min(100, done * 100.0 / TotalSteps);
        ProgressText = $"التقدم: {done} من {TotalSteps} · الخطوة {CurrentStepIndex + 1}";
    }

    private void NotifyStepProps()
    {
        OnPropertyChanged(nameof(TotalSteps));
        OnPropertyChanged(nameof(CurrentStepInfo));
        OnPropertyChanged(nameof(CurrentKind));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepDescription));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanSkip));
        OnPropertyChanged(nameof(HasLastResult));
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(IsCapitalStep));
        OnPropertyChanged(nameof(IsCashStep));
        OnPropertyChanged(nameof(IsInvestorsStep));
        OnPropertyChanged(nameof(IsWarehousesStep));
        OnPropertyChanged(nameof(IsCategoriesStep));
        OnPropertyChanged(nameof(IsProductsStep));
        OnPropertyChanged(nameof(IsPricingTypesStep));
        OnPropertyChanged(nameof(IsProductPricingStep));
        OnPropertyChanged(nameof(IsCustomersStep));
        OnPropertyChanged(nameof(IsSuppliersStep));
        OnPropertyChanged(nameof(IsExpenseTypesStep));
        OnPropertyChanged(nameof(IsInstallmentsStep));
        OnPropertyChanged(nameof(IsSimpleNameStep));
        OnPropertyChanged(nameof(ShowManualEntryForm));
        OnPropertyChanged(nameof(ShowExcelPanel));
        OnPropertyChanged(nameof(ShowDataGrid));
        OnPropertyChanged(nameof(EntryHint));
    }

    private void ResetNewRowFields()
    {
        NewRowName = string.Empty;
        NewRowPhone = null;
        NewRowExtra = null;
        NewRowAmount = 0;
        NewRowAmount2 = 0;
        NewRowAmount3 = 0;
        NewRowInt = 1;
        NewRowInt2 = 0;
        NewRowDate = DateTime.Today;
        NewRowKind = "قاصة";
    }

    private async Task RefreshLookupsAsync()
    {
        try
        {
            AvailableCategories.Clear();
            foreach (var c in await _unitOfWork.Categories.GetAllAsync())
                AvailableCategories.Add(c.Name);

            AvailableProducts.Clear();
            foreach (var p in await _unitOfWork.Products.GetAllAsync())
                AvailableProducts.Add(p.Name);

            AvailablePricingTypes.Clear();
            foreach (var t in await _pricingTypeService.GetActiveAsync())
                AvailablePricingTypes.Add(t.Name);

            AvailableWarehouses.Clear();
            foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
                AvailableWarehouses.Add(w.Name);

            if (string.IsNullOrWhiteSpace(SelectedWarehouseName) && AvailableWarehouses.Count > 0)
                SelectedWarehouseName = AvailableWarehouses[0];
        }
        catch
        {
            // best-effort
        }
    }

    [RelayCommand]
    private void SetManualMode()
    {
        IsManualEntryMode = true;
        IsExcelMode = false;
    }

    [RelayCommand]
    private void SetExcelMode()
    {
        IsExcelMode = true;
        IsManualEntryMode = false;
    }

    [RelayCommand]
    private void GoToStep(MigrationStepInfo? step)
    {
        if (step is null || IsCompleted || IsBusy) return;
        var index = Steps.IndexOf(step);
        if (index < 0) return;

        // فقط الرجوع لخطوات سابقة أو محفوظة — منع القفز للأمام
        if (index > CurrentStepIndex && !_savedSteps.Contains(Steps[CurrentStepIndex].Kind))
        {
            Warn("احفظ الخطوة الحالية أو تخطَّها قبل الانتقال لخطوة لاحقة");
            return;
        }

        if (index <= CurrentStepIndex || _savedSteps.Contains(Steps[Math.Max(0, index - 1)].Kind))
            CurrentStepIndex = index;
    }

    [RelayCommand]
    private void AddManualRow()
    {
        if (CurrentKind is null || IsCapitalStep) return;
        var kind = CurrentKind.Value;
        var row = new MigrationNamedBalanceRow();

        switch (kind)
        {
            case MigrationStepKind.CashAndBank:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم القاصة أو المصرف"); return; }
                row.Name = NewRowName.Trim();
                row.Amount = NewRowAmount;
                row.Kind = NewRowKind.Contains("مصرف", StringComparison.Ordinal) ? "Bank" : "Cash";
                row.AccountNumber = NewRowExtra;
                row.Notes = NewRowKind;
                break;
            case MigrationStepKind.Investors:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم المستثمر"); return; }
                row.Name = NewRowName.Trim();
                row.Phone = NewRowPhone;
                row.ProfitPercentage = NewRowAmount2;
                row.Amount = NewRowAmount;
                break;
            case MigrationStepKind.Warehouses:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم المخزن"); return; }
                row.Name = NewRowName.Trim();
                row.Notes = NewRowExtra;
                break;
            case MigrationStepKind.Categories:
            case MigrationStepKind.ExpenseTypes:
            case MigrationStepKind.PricingTypes:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل الاسم"); return; }
                row.Name = NewRowName.Trim();
                break;
            case MigrationStepKind.Products:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم المنتج"); return; }
                row.Name = NewRowName.Trim();
                row.CategoryName = string.IsNullOrWhiteSpace(NewRowExtra) ? "عام" : NewRowExtra.Trim();
                row.Barcode = string.IsNullOrWhiteSpace(NewRowPhone) ? null : NewRowPhone.Trim();
                row.UnitPrice = NewRowAmount;
                row.Amount = NewRowAmount;
                row.Quantity = NewRowAmount2;
                row.UnitCost = NewRowAmount3;
                break;
            case MigrationStepKind.ProductPricing:
                if (string.IsNullOrWhiteSpace(NewRowName) || string.IsNullOrWhiteSpace(NewRowExtra))
                { Warn("اختر المنتج ونوع التسعير"); return; }
                row.ProductName = NewRowName.Trim();
                row.Name = NewRowName.Trim();
                row.PricingTypeName = NewRowExtra.Trim();
                row.SalePrice = NewRowAmount;
                row.Amount = NewRowAmount;
                row.PurchasePrice = NewRowAmount2;
                row.UnitCost = NewRowAmount2;
                break;
            case MigrationStepKind.Customers:
            case MigrationStepKind.Suppliers:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل الاسم"); return; }
                if (NewRowAmount <= 0) { Warn("أدخل مبلغ الرصيد الافتتاحي"); return; }
                row.Name = NewRowName.Trim();
                row.Phone = NewRowPhone;
                row.FileNumber = NewRowExtra;
                row.Amount = NewRowAmount;
                row.Date = NewRowDate;
                break;
            case MigrationStepKind.Installments:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم الزبون"); return; }
                if (NewRowAmount <= 0 || NewRowInt <= 0) { Warn("أدخل المبلغ وعدد الأقساط"); return; }
                row.Name = NewRowName.Trim();
                row.FileNumber = NewRowExtra;
                row.Amount = NewRowAmount;
                row.NumberOfInstallments = NewRowInt;
                row.PaidInstallmentsCount = NewRowInt2;
                row.Date = NewRowDate;
                row.Notes = NewRowPhone;
                break;
            default:
                return;
        }

        CurrentRows.Add(row);
        ResetNewRowFields();
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ShowDataGrid));
        StatusMessage = $"صفوف جاهزة: {CurrentRows.Count}";
    }

    [RelayCommand]
    private void RemoveRow(MigrationNamedBalanceRow? row)
    {
        if (row is null) return;
        CurrentRows.Remove(row);
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ShowDataGrid));
    }

    [RelayCommand]
    private void ClearRows()
    {
        if (CurrentRows.Count == 0) return;
        if (!BeautifulMessageDialog.ShowConfirm("مسح جميع صفوف هذه الخطوة؟", "تأكيد"))
            return;
        CurrentRows.Clear();
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ShowDataGrid));
        StatusMessage = "تم مسح الصفوف";
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog { Filter = "Excel|*.xlsx;*.xls", Title = "اختر ملف Excel" };
        if (dlg.ShowDialog() != true) return;
        SelectedFilePath = dlg.FileName;
        _ = LoadPreviewAsync();
    }

    [RelayCommand]
    private async Task LoadPreviewAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath) || CurrentKind is null) return;
        try
        {
            IsBusy = true;
            CurrentRows.Clear();
            await LoadExcelIntoRowsAsync(CurrentKind.Value, SelectedFilePath);
            StatusMessage = CurrentRows.Count == 0
                ? "الملف لا يحتوي على بيانات صالحة"
                : $"تم تحميل {CurrentRows.Count} صف — راجع الجدول ثم احفظ";
            OnPropertyChanged(nameof(HasRows));
            OnPropertyChanged(nameof(ShowDataGrid));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
            NotifyStepProps();
        }
    }

    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        if (CurrentKind is null) return;
        var dlg = new SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = $"قالب_{CurrentStepInfo?.ShortTitle ?? "نقل"}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var bytes = await BuildTemplateBytesAsync(CurrentKind.Value);
            await File.WriteAllBytesAsync(dlg.FileName, bytes);
            BeautifulMessageDialog.ShowSuccess("تم حفظ القالب بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private Task<byte[]> BuildTemplateBytesAsync(MigrationStepKind kind) => kind switch
    {
        MigrationStepKind.CashAndBank => Task.FromResult(MigrationExcelHelper.BuildTemplate(
            "النوع", "الاسم", "الرصيد", "رقم_الحساب")),
        MigrationStepKind.Investors => Task.FromResult(MigrationExcelHelper.BuildTemplate(
            "الاسم", "الهاتف", "نسبة_الربح", "الرصيد_الافتتاحي")),
        MigrationStepKind.Warehouses => Task.FromResult(MigrationExcelHelper.BuildTemplate(
            "الاسم", "الموقع")),
        MigrationStepKind.Categories => Task.FromResult(MigrationExcelHelper.BuildTemplate("الاسم")),
        MigrationStepKind.Products => Task.FromResult(MigrationExcelHelper.BuildTemplate(
            "اسم_المنتج", "الباركود", "الصنف", "السعر_المفرد", "الكمية", "تكلفة_الوحدة")),
        MigrationStepKind.PricingTypes => Task.FromResult(MigrationExcelHelper.BuildTemplate("الاسم")),
        MigrationStepKind.ProductPricing => Task.FromResult(MigrationExcelHelper.BuildTemplate(
            "اسم_المنتج", "نوع_التسعير", "سعر_البيع", "سعر_الشراء")),
        MigrationStepKind.Customers => Task.FromResult(_customerBalanceExcel.GenerateTemplate()),
        MigrationStepKind.Suppliers => Task.FromResult(_supplierBalanceExcel.GenerateTemplate()),
        MigrationStepKind.ExpenseTypes => Task.FromResult(MigrationExcelHelper.BuildTemplate("الاسم")),
        MigrationStepKind.Installments => Task.FromResult(_openingInstallmentExcel.GenerateTemplate()),
        _ => Task.FromResult(MigrationExcelHelper.BuildTemplate("الاسم"))
    };

    private async Task LoadExcelIntoRowsAsync(MigrationStepKind kind, string path)
    {
        switch (kind)
        {
            case MigrationStepKind.CashAndBank:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 4))
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Kind = r[0].Contains("مصرف", StringComparison.OrdinalIgnoreCase) ||
                               r[0].Contains("Bank", StringComparison.OrdinalIgnoreCase) ? "Bank" : "Cash",
                        Name = r[1],
                        Amount = MigrationExcelHelper.ParseDecimal(r[2]),
                        AccountNumber = r[3],
                        Notes = r[0]
                    });
                break;
            case MigrationStepKind.Investors:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 4))
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Name = r[0], Phone = r[1],
                        ProfitPercentage = MigrationExcelHelper.ParseDecimal(r[2]),
                        Amount = MigrationExcelHelper.ParseDecimal(r[3])
                    });
                break;
            case MigrationStepKind.Warehouses:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 2))
                    CurrentRows.Add(new MigrationNamedBalanceRow { Name = r[0], Notes = r[1] });
                break;
            case MigrationStepKind.Categories:
            case MigrationStepKind.ExpenseTypes:
            case MigrationStepKind.PricingTypes:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 1))
                    CurrentRows.Add(new MigrationNamedBalanceRow { Name = r[0] });
                break;
            case MigrationStepKind.Products:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 6))
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Name = r[0], Barcode = r[1],
                        CategoryName = string.IsNullOrWhiteSpace(r[2]) ? "عام" : r[2],
                        UnitPrice = MigrationExcelHelper.ParseDecimal(r[3]),
                        Amount = MigrationExcelHelper.ParseDecimal(r[3]),
                        Quantity = MigrationExcelHelper.ParseDecimal(r[4]),
                        UnitCost = MigrationExcelHelper.ParseDecimal(r[5])
                    });
                break;
            case MigrationStepKind.ProductPricing:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 4))
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        ProductName = r[0], Name = r[0], PricingTypeName = r[1],
                        SalePrice = MigrationExcelHelper.ParseDecimal(r[2]),
                        Amount = MigrationExcelHelper.ParseDecimal(r[2]),
                        PurchasePrice = MigrationExcelHelper.ParseDecimal(r[3]),
                        UnitCost = MigrationExcelHelper.ParseDecimal(r[3])
                    });
                break;
            case MigrationStepKind.Customers:
                foreach (var r in _customerBalanceExcel.ParseImportFile(path))
                    CurrentRows.Add(FromPartyImport(r));
                break;
            case MigrationStepKind.Suppliers:
                foreach (var r in _supplierBalanceExcel.ParseImportFile(path))
                    CurrentRows.Add(FromPartyImport(r));
                break;
            case MigrationStepKind.Installments:
                foreach (var r in _openingInstallmentExcel.ParseImportFile(path))
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Name = r.CustomerName,
                        FileNumber = r.FileNumber,
                        Amount = r.TotalAmount,
                        NumberOfInstallments = r.NumberOfInstallments,
                        PaidInstallmentsCount = r.PaidInstallmentsCount,
                        Date = r.StartDate,
                        Notes = r.Notes,
                        IsValid = r.IsValid,
                        ErrorText = r.IsValid ? null : r.ErrorsText
                    });
                break;
            default:
                await Task.CompletedTask;
                break;
        }
    }

    private static MigrationNamedBalanceRow FromPartyImport(OpeningPartyBalanceImportRow r) => new()
    {
        Name = r.PartyName,
        Phone = r.Phone,
        FileNumber = r.FileNumber,
        Amount = r.Amount,
        Date = r.Date,
        Notes = r.Notes,
        IsValid = r.IsValid,
        ErrorText = r.IsValid ? null : r.ErrorsText
    };

    [RelayCommand]
    private async Task SaveCurrentStepAsync()
    {
        if (CurrentKind is null || IsCompleted || IsBusy) return;
        try
        {
            IsBusy = true;
            NotifyStepProps();
            var ok = await PersistStepAsync(CurrentKind.Value);
            if (!ok) return;

            MarkSaved(CurrentKind.Value);
            StatusMessage = "تم حفظ الخطوة بنجاح — يمكنك الانتقال للتالي";
            BeautifulMessageDialog.ShowSuccess(StatusMessage);
            await RefreshLookupsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر الحفظ:\n{ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            IsBusy = false;
            NotifyStepProps();
        }
    }

    [RelayCommand]
    private async Task SkipStepAsync()
    {
        if (!CanSkip || CurrentKind is null) return;

        if (CurrentRows.Count > 0)
        {
            if (!BeautifulMessageDialog.ShowConfirm(
                    "توجد صفوف غير محفوظة في هذه الخطوة. هل تريد تخطيها بدون حفظ؟",
                    "تخطي الخطوة"))
                return;
            CurrentRows.Clear();
        }

        MarkSaved(CurrentKind.Value);
        StatusMessage = "تم تخطي الخطوة";
        await AdvanceAsync();
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (IsCompleted || CurrentKind is null || IsBusy) return;

        try
        {
            IsBusy = true;
            NotifyStepProps();

            if (!_savedSteps.Contains(CurrentKind.Value))
            {
                var hasData = IsCapitalStep
                    ? CapitalAmount > 0 || ProfitOpeningBalance != 0
                    : CurrentRows.Any(r => !string.IsNullOrWhiteSpace(r.Name) || !string.IsNullOrWhiteSpace(r.ProductName));

                if (!hasData && CurrentStepInfo?.IsOptional == true)
                {
                    MarkSaved(CurrentKind.Value);
                }
                else if (!hasData && CurrentKind == MigrationStepKind.Warehouses
                         && await _unitOfWork.Warehouses.AnyAsync())
                {
                    MarkSaved(CurrentKind.Value);
                }
                else
                {
                    var ok = await PersistStepAsync(CurrentKind.Value);
                    if (!ok) return;
                    MarkSaved(CurrentKind.Value);
                }
            }

            await AdvanceAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر الانتقال:\n{ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            IsBusy = false;
            NotifyStepProps();
        }
    }

    private async Task AdvanceAsync()
    {
        if (IsLastStep)
        {
            Finish();
            return;
        }

        CurrentStepIndex++;
        await Task.CompletedTask;
    }

    private void MarkSaved(MigrationStepKind kind)
    {
        _savedSteps.Add(kind);
        if (CurrentStepInfo is not null) CurrentStepInfo.IsSaved = true;
        LastImportedCount = Math.Max(LastImportedCount, CurrentRows.Count);
        UpdateProgress();
        SyncStepFlags();
        OnPropertyChanged(nameof(HasLastResult));
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0 && !IsCompleted && !IsBusy)
            CurrentStepIndex--;
    }

    [RelayCommand]
    private void Finish()
    {
        IsCompleted = true;
        UpdateProgress();
        NotifyStepProps();
        BeautifulMessageDialog.ShowSuccess("اكتمل معالج النقل — يمكنك متابعة العمل بشكل طبيعي");
    }

    private async Task<bool> PersistStepAsync(MigrationStepKind kind) => kind switch
    {
        MigrationStepKind.Capital => await SaveCapitalAsync(),
        MigrationStepKind.CashAndBank => await SaveCashAndBankAsync(),
        MigrationStepKind.Investors => await SaveInvestorsAsync(),
        MigrationStepKind.Warehouses => await SaveWarehousesAsync(),
        MigrationStepKind.Categories => await SaveCategoriesAsync(),
        MigrationStepKind.Products => await SaveProductsAsync(),
        MigrationStepKind.PricingTypes => await SavePricingTypesAsync(),
        MigrationStepKind.ProductPricing => await SaveProductPricingAsync(),
        MigrationStepKind.Customers => await SaveCustomersAsync(),
        MigrationStepKind.Suppliers => await SaveSuppliersAsync(),
        MigrationStepKind.ExpenseTypes => await SaveExpenseTypesAsync(),
        MigrationStepKind.Installments => await SaveInstallmentsAsync(),
        _ => false
    };

    private async Task<bool> SaveCapitalAsync()
    {
        if (await _unitOfWork.CapitalEntries.AnyAsync())
        {
            _needsCapital = false;
            return true;
        }

        if (CapitalAmount <= 0 && ProfitOpeningBalance == 0)
        {
            Warn("أدخل رأس المال أو الأرباح الافتتاحية");
            return false;
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (CapitalAmount > 0)
            {
                await _unitOfWork.CapitalEntries.AddAsync(new CapitalEntry
                {
                    Amount = CapitalAmount,
                    Date = CapitalDate,
                    Type = CapitalEntryType.Initial,
                    Notes = string.IsNullOrWhiteSpace(CapitalNotes) ? "رأس المال — معالج النقل" : CapitalNotes
                });
            }

            if (ProfitOpeningBalance != 0)
            {
                await _unitOfWork.CapitalEntries.AddAsync(new CapitalEntry
                {
                    Amount = ProfitOpeningBalance,
                    Date = CapitalDate,
                    Type = CapitalEntryType.ProfitOpeningBalance,
                    Notes = "الأرباح الافتتاحية — معالج النقل"
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            _needsCapital = false;
            LastImportedCount = 1;
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<bool> SaveCashAndBankAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        if (rows.Count == 0)
        {
            if (await _unitOfWork.CashBoxes.AnyAsync() || await _unitOfWork.BankAccounts.AnyAsync())
                return true;
            Warn("أضف قاصة أو مصرفاً، أو تخطَّ الخطوة إن كانت موجودة مسبقاً");
            return false;
        }

        var existingCash = (await _unitOfWork.CashBoxes.GetAllAsync()).ToList();
        var existingBank = (await _unitOfWork.BankAccounts.GetAllAsync()).ToList();
        var saved = 0;
        foreach (var row in rows)
        {
            var name = row.Name.Trim();
            if (string.Equals(row.Kind, "Bank", StringComparison.OrdinalIgnoreCase))
            {
                if (existingBank.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var bank = new BankAccount { Name = name, AccountNumber = row.AccountNumber, Balance = row.Amount };
                await _unitOfWork.BankAccounts.AddAsync(bank);
                existingBank.Add(bank);
            }
            else
            {
                if (existingCash.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var cash = new CashBox { Name = name, Balance = row.Amount };
                await _unitOfWork.CashBoxes.AddAsync(cash);
                existingCash.Add(cash);
            }
            saved++;
        }
        await _unitOfWork.SaveChangesAsync();
        LastImportedCount = saved;
        LastSkippedCount = rows.Count - saved;
        return true;
    }

    private async Task<bool> SaveInvestorsAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        if (rows.Count == 0) { Warn("أضف مستثمراً أو اضغط تخطي"); return false; }

        var existing = (await _investorService.GetAllInvestorsAsync()).ToList();
        var openingItems = new List<InvestorOpeningBalanceItem>();
        foreach (var row in rows)
        {
            var match = existing.FirstOrDefault(i =>
                string.Equals(i.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                match = await _investorService.AddInvestorAsync(row.Name.Trim(), row.Phone, row.ProfitPercentage);
                existing.Add(match);
            }

            openingItems.Add(new InvestorOpeningBalanceItem
            {
                InvestorId = match.Id,
                Name = match.Name,
                Phone = match.Phone,
                ProfitPercentage = row.ProfitPercentage > 0 ? row.ProfitPercentage : match.ProfitPercentage,
                OpeningBalance = row.Amount
            });
        }

        await _investorService.SaveOpeningBalancesAsync(openingItems);
        LastImportedCount = openingItems.Count;
        return true;
    }

    private async Task<bool> SaveWarehousesAsync()
    {
        var existing = (await _unitOfWork.Warehouses.GetAllAsync()).ToList();
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        if (rows.Count == 0 && existing.Count > 0) return true;
        if (rows.Count == 0)
        {
            Warn("أدخل مخزناً واحداً على الأقل — مطلوب قبل المنتجات");
            return false;
        }

        var saved = 0;
        foreach (var row in rows)
        {
            if (existing.Any(w => string.Equals(w.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue;
            var wh = new Warehouse
            {
                Name = row.Name.Trim(),
                Location = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes
            };
            await _unitOfWork.Warehouses.AddAsync(wh);
            existing.Add(wh);
            saved++;
        }
        await _unitOfWork.SaveChangesAsync();
        LastImportedCount = saved;
        return true;
    }

    private async Task<bool> SaveCategoriesAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        if (rows.Count == 0) { Warn("أضف أصنافاً أو اضغط تخطي"); return false; }
        var existing = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var saved = 0;
        foreach (var row in rows.GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
        {
            if (existing.Any(c => string.Equals(c.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue;
            var cat = new Category { Name = row.Name.Trim() };
            await _unitOfWork.Categories.AddAsync(cat);
            existing.Add(cat);
            saved++;
        }
        await _unitOfWork.SaveChangesAsync();
        LastImportedCount = saved;
        return true;
    }

    private async Task<bool> SaveProductsAsync()
    {
        // دمج الصفوف المكررة بالاسم (آخر قيمة تفوز) لتفادي فهرس المخزون الفريد
        var rows = CurrentRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        if (rows.Count == 0) { Warn("أضف منتجات أو اضغط تخطي"); return false; }

        var warehouses = (await _unitOfWork.Warehouses.GetAllAsync()).ToList();
        if (warehouses.Count == 0)
        {
            Warn("لا يوجد مخزن — ارجع لخطوة المخازن أولاً");
            return false;
        }

        var warehouse = warehouses.FirstOrDefault(w =>
            string.Equals(w.Name, SelectedWarehouseName, StringComparison.OrdinalIgnoreCase))
            ?? warehouses[0];

        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
        var stocks = (await _unitOfWork.WarehouseStocks.FindAsync(s => s.WarehouseId == warehouse.Id)).ToList();
        var prices = (await _unitOfWork.ProductPrices.GetAllAsync()).ToList();

        var pricingTypes = (await _pricingTypeService.GetActiveAsync()).ToList();
        if (pricingTypes.Count == 0)
        {
            await _pricingTypeService.EnsureDefaultExistsAsync();
            pricingTypes = (await _pricingTypeService.GetActiveAsync()).ToList();
        }
        var defaultPricing = pricingTypes.FirstOrDefault(t => t.IsDefault) ?? pricingTypes.FirstOrDefault();

        var saved = 0;
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var row in rows)
            {
                var catName = string.IsNullOrWhiteSpace(row.CategoryName) ? "عام" : row.CategoryName.Trim();
                var category = categories.FirstOrDefault(c =>
                    string.Equals(c.Name, catName, StringComparison.OrdinalIgnoreCase));
                if (category is null)
                {
                    category = new Category { Name = catName };
                    await _unitOfWork.Categories.AddAsync(category);
                    await _unitOfWork.SaveChangesAsync();
                    categories.Add(category);
                }

                var product = products.FirstOrDefault(p =>
                    string.Equals(p.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (product is null)
                {
                    product = new Product
                    {
                        Name = row.Name.Trim(),
                        Barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode.Trim(),
                        CategoryId = category.Id
                    };
                    await _unitOfWork.Products.AddAsync(product);
                    await _unitOfWork.SaveChangesAsync();
                    products.Add(product);
                }
                else if (!string.IsNullOrWhiteSpace(row.Barcode) && string.IsNullOrWhiteSpace(product.Barcode))
                {
                    product.Barcode = row.Barcode.Trim();
                    _unitOfWork.Products.Update(product);
                }

                if (row.Quantity > 0)
                {
                    var stock = stocks.FirstOrDefault(s => s.ProductId == product.Id);
                    if (stock is null)
                    {
                        stock = new WarehouseStock
                        {
                            WarehouseId = warehouse.Id,
                            ProductId = product.Id,
                            Quantity = row.Quantity,
                            OpeningQuantity = row.Quantity,
                            UnitCost = row.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.WarehouseStocks.AddAsync(stock);
                        stocks.Add(stock);
                    }
                    else
                    {
                        stock.OpeningQuantity = row.Quantity;
                        stock.Quantity = Math.Max(stock.Quantity, row.Quantity);
                        if (row.UnitCost > 0) stock.UnitCost = row.UnitCost;
                        _unitOfWork.WarehouseStocks.Update(stock);
                    }
                }

                if (defaultPricing is not null && row.UnitPrice > 0)
                {
                    var existingPrice = prices.FirstOrDefault(p =>
                        p.ProductId == product.Id && p.PricingTypeId == defaultPricing.Id);
                    if (existingPrice is null)
                    {
                        existingPrice = new ProductPrice
                        {
                            ProductId = product.Id,
                            PricingTypeId = defaultPricing.Id,
                            SalePrice = row.UnitPrice,
                            PurchasePrice = row.UnitCost
                        };
                        await _unitOfWork.ProductPrices.AddAsync(existingPrice);
                        prices.Add(existingPrice);
                    }
                    else
                    {
                        existingPrice.SalePrice = row.UnitPrice;
                        if (row.UnitCost > 0) existingPrice.PurchasePrice = row.UnitCost;
                        _unitOfWork.ProductPrices.Update(existingPrice);
                    }
                }

                saved++;
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            LastImportedCount = saved;
            if (saved == 0)
            {
                Warn("لم يُحفظ أي منتج — تحقق من الأسماء");
                return false;
            }

            await RefreshPricingNeedFlagsAsync();
            // بعد إضافة منتجات جديدة نحتاج خطوة التسعير إن وُجدت منتجات بلا أسعار
            _needsProductPricing = true;
            UpdatePricingStepOptionality();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<bool> SavePricingTypesAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()).ToList();

        if (rows.Count == 0)
        {
            if (!_needsPricingTypes) return true;
            Warn("أضف نوع تسعير واحداً على الأقل (مثل: سعر مفرد)");
            return false;
        }

        var existing = (await _pricingTypeService.GetActiveAsync()).ToList();
        var saved = 0;
        foreach (var row in rows)
        {
            if (existing.Any(t => string.Equals(t.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue;
            await _pricingTypeService.CreateAsync(new PricingType
            {
                Name = row.Name.Trim(),
                IsActive = true,
                IsDefault = existing.Count == 0 && saved == 0
            });
            saved++;
        }

        // ضمان وجود نوع افتراضي دائماً
        await _pricingTypeService.EnsureDefaultExistsAsync();
        await RefreshPricingNeedFlagsAsync();
        UpdatePricingStepOptionality();

        LastImportedCount = Math.Max(saved, rows.Count);
        if (_needsPricingTypes && saved == 0 && existing.Count == 0)
        {
            Warn("تعذّر إنشاء أنواع التسعير");
            return false;
        }
        _needsPricingTypes = false;
        UpdatePricingStepOptionality();
        return true;
    }

    private async Task<bool> SaveProductPricingAsync()
    {
        // إن لم توجد أنواع تسعير أنشئ الافتراضي أولاً
        var types = (await _pricingTypeService.GetActiveAsync()).ToList();
        if (types.Count == 0)
        {
            await _pricingTypeService.EnsureDefaultExistsAsync();
            types = (await _pricingTypeService.GetActiveAsync()).ToList();
            await RefreshLookupsAsync();
        }

        if (types.Count == 0)
        {
            Warn("لا توجد أنواع تسعير — ارجع لخطوة أنواع التسعير أولاً");
            return false;
        }

        var rows = CurrentRows.Where(r =>
                !string.IsNullOrWhiteSpace(r.ProductName ?? r.Name))
            .ToList();

        // إن كانت الخطوة إلزامية والجدول فارغ — عبّئه ثم اطلب الإدخال
        if (rows.Count == 0)
        {
            await PrefillProductPricingRowsAsync();
            rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.ProductName ?? r.Name)).ToList();
        }

        if (rows.Count == 0)
        {
            if (!_needsProductPricing) return true;
            Warn("لا توجد منتجات لتسعيرها — أضف منتجات أولاً أو تخطَّ لاحقاً بعد إضافة المنتجات");
            return false;
        }

        // صفوف بلا نوع تسعير → استخدم الافتراضي
        var defaultType = types.FirstOrDefault(t => t.IsDefault) ?? types.First();
        foreach (var row in rows.Where(r => string.IsNullOrWhiteSpace(r.PricingTypeName)))
            row.PricingTypeName = defaultType.Name;

        var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
        var prices = new List<ProductPrice>();
        var skipped = 0;

        foreach (var row in rows)
        {
            var productName = (row.ProductName ?? row.Name).Trim();
            var product = products.FirstOrDefault(p =>
                string.Equals(p.Name, productName, StringComparison.OrdinalIgnoreCase));
            var type = types.FirstOrDefault(t =>
                string.Equals(t.Name, row.PricingTypeName!.Trim(), StringComparison.OrdinalIgnoreCase));
            if (product is null || type is null)
            {
                skipped++;
                row.IsValid = false;
                row.ErrorText = product is null ? "المنتج غير موجود" : "نوع التسعير غير موجود";
                continue;
            }

            var sale = row.SalePrice > 0 ? row.SalePrice : row.Amount;
            var purchase = row.PurchasePrice > 0 ? row.PurchasePrice : row.UnitCost;
            if (sale <= 0 && purchase <= 0)
            {
                skipped++;
                row.IsValid = false;
                row.ErrorText = "أدخل سعر البيع أو الشراء";
                continue;
            }

            prices.Add(new ProductPrice
            {
                ProductId = product.Id,
                PricingTypeId = type.Id,
                SalePrice = sale,
                PurchasePrice = purchase
            });
            row.IsValid = true;
            row.ErrorText = null;
        }

        if (prices.Count > 0)
            await _productPriceService.UpsertManyAsync(prices);

        LastImportedCount = prices.Count;
        LastSkippedCount = skipped;
        await RefreshPricingNeedFlagsAsync();
        UpdatePricingStepOptionality();

        if (prices.Count == 0)
        {
            Warn("لم يُحفظ أي سعر — تأكد من أسماء المنتجات وأنواع التسعير وأدخل مبلغاً أكبر من صفر");
            return false;
        }

        _needsProductPricing = false;
        UpdatePricingStepOptionality();
        return true;
    }

    private async Task<bool> SaveCustomersAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Amount > 0).ToList();
        if (rows.Count == 0) { Warn("أضف عملاء مع رصيد أو اضغط تخطي"); return false; }
        if (CurrentRows.Any(r => !r.IsValid))
        {
            Warn("صحّح الصفوف غير الصالحة أولاً");
            return false;
        }

        var result = await _openingPartyBalance.CreateCustomerOpeningBalancesBatchAsync(
            rows.Select(r => new OpeningPartyBalanceRequest
            {
                PartyName = r.Name.Trim(),
                Phone = r.Phone,
                FileNumber = r.FileNumber,
                Amount = r.Amount,
                Date = r.Date,
                Notes = r.Notes
            }).ToList());

        LastImportedCount = result.SuccessCount;
        LastSkippedCount = result.FailedCount;
        if (result.SuccessCount == 0)
        {
            BeautifulMessageDialog.ShowError(string.Join("\n", result.Errors.Take(5)));
            return false;
        }
        return true;
    }

    private async Task<bool> SaveSuppliersAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Amount > 0).ToList();
        if (rows.Count == 0) { Warn("أضف موردين مع رصيد أو اضغط تخطي"); return false; }
        if (CurrentRows.Any(r => !r.IsValid))
        {
            Warn("صحّح الصفوف غير الصالحة أولاً");
            return false;
        }

        var result = await _openingPartyBalance.CreateSupplierOpeningBalancesBatchAsync(
            rows.Select(r => new OpeningPartyBalanceRequest
            {
                PartyName = r.Name.Trim(),
                Phone = r.Phone,
                FileNumber = r.FileNumber,
                Amount = r.Amount,
                Date = r.Date,
                Notes = r.Notes
            }).ToList());

        LastImportedCount = result.SuccessCount;
        LastSkippedCount = result.FailedCount;
        if (result.SuccessCount == 0)
        {
            BeautifulMessageDialog.ShowError(string.Join("\n", result.Errors.Take(5)));
            return false;
        }
        return true;
    }

    private async Task<bool> SaveExpenseTypesAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()).ToList();
        if (rows.Count == 0) { Warn("أضف أنواع مصاريف أو اضغط تخطي"); return false; }

        var existing = (await _expenseService.GetAllExpenseTypesAsync()).ToList();
        var saved = 0;
        foreach (var row in rows)
        {
            if (existing.Any(e => string.Equals(e.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue;
            await _expenseService.AddExpenseTypeAsync(row.Name.Trim());
            saved++;
        }
        LastImportedCount = saved;
        return true;
    }

    private async Task<bool> SaveInstallmentsAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Amount > 0 && r.NumberOfInstallments > 0).ToList();
        if (rows.Count == 0) { Warn("أضف أرصدة أقساط أو اضغط تخطي"); return false; }
        if (CurrentRows.Any(r => !r.IsValid))
        {
            Warn("صحّح الصفوف غير الصالحة أولاً");
            return false;
        }

        var result = await _installmentService.CreateOpeningBalancePlansBatchAsync(
            rows.Select(r => new OpeningInstallmentBalanceRequest
            {
                CustomerName = r.Name.Trim(),
                FileNumber = r.FileNumber,
                TotalAmount = r.Amount,
                NumberOfInstallments = r.NumberOfInstallments,
                PaidInstallmentsCount = r.PaidInstallmentsCount,
                StartDate = r.Date,
                Notes = r.Notes
            }).ToList());

        LastImportedCount = result.SuccessCount;
        LastSkippedCount = result.FailedCount;
        if (result.SuccessCount == 0)
        {
            BeautifulMessageDialog.ShowError(string.Join("\n", result.Errors.Take(5)));
            return false;
        }
        return true;
    }

    private static void Warn(string message) => BeautifulMessageDialog.ShowWarning(message);
}
