using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
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
    private readonly IFeatureFlagService _featureFlags;
    private readonly IBranchService _branchService;
    private readonly IBusinessSettingsService _businessSettingsService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IUserPreferencesService _preferences;
    private readonly IBranchContext _branchContext;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly HashSet<MigrationStepKind> _savedSteps = [];
    private readonly Dictionary<MigrationStepKind, List<MigrationNamedBalanceRow>> _rowsByStep = new();
    private CancellationTokenSource? _navCts;
    private MigrationStepKind? _displayedKind;
    private bool _needsCapital = true;
    private bool _needsPricingTypes = true;
    private bool _stepDirty;

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
    [ObservableProperty] private decimal _capitalAmountUsd;
    [ObservableProperty] private DateTime _capitalDate = DateTime.Today;
    [ObservableProperty] private string _capitalNotes = string.Empty;
    [ObservableProperty] private decimal _profitOpeningBalance;
    [ObservableProperty] private decimal _profitOpeningBalanceUsd;
    [ObservableProperty] private bool _enableMultiCurrency;
    [ObservableProperty] private decimal _initialUsdToIqd;
    [ObservableProperty] private string _capitalTotalInIqdDisplay = "0 د.ع";
    [ObservableProperty] private string _newBranchName = string.Empty;
    [ObservableProperty] private string _newBranchCode = string.Empty;
    [ObservableProperty] private decimal _newBranchCapitalIqd;
    [ObservableProperty] private decimal _newBranchCapitalUsd;
    [ObservableProperty] private Branch? _selectedBranchForWarehouse;
    [ObservableProperty] private string _newRowName = string.Empty;
    [ObservableProperty] private string? _newRowPhone;
    [ObservableProperty] private string? _newRowExtra;
    [ObservableProperty] private decimal _newRowAmount;
    [ObservableProperty] private decimal _newRowAmountUsd;
    [ObservableProperty] private decimal _newRowAmount2;
    [ObservableProperty] private decimal _newRowAmount3;
    [ObservableProperty] private int _newRowInt = 1;
    [ObservableProperty] private int _newRowInt2;
    [ObservableProperty] private DateTime _newRowDate = DateTime.Today;
    [ObservableProperty] private string _newRowKind = "Cash";
    [ObservableProperty] private string? _selectedWarehouseName;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private CurrencyOption? _newRowCostCurrencyOption;
    [ObservableProperty] private decimal _newRowFxRate = 1m;
    [ObservableProperty] private bool _showFxRateInput;
    [ObservableProperty] private int _productsGridVersion;

    /// <summary>أعمدة ميزات المنتج في جدول النقل — حسب FeatureFlags الحالية.</summary>
    [ObservableProperty] private bool _showProductPharmacyFields;
    [ObservableProperty] private bool _showProductCarFields;
    [ObservableProperty] private bool _showProductWeightFields;
    [ObservableProperty] private bool _showProductDiscountFields;
    [ObservableProperty] private bool _showProductPurchasePriceColumns;

    public IReadOnlyList<string> PlateTypeOptions { get; } =
    [
        AlMuhasib.Core.Helpers.VehiclePlateTypeHelper.NoneLabel,
        AlMuhasib.Core.Helpers.VehiclePlateTypeHelper.InspectionLabel,
        AlMuhasib.Core.Helpers.VehiclePlateTypeHelper.OfficialLabel
    ];

    public IReadOnlyList<string> WeightUnitOptions { get; } = ["كغ", "غرام", "لتر", "مل", "متر", "سم"];
    public IReadOnlyList<string> DiscountTypeOptions { get; } = ["بدون", "نسبة مئوية", "قيمة ثابتة"];

    public ObservableCollection<MigrationStepInfo> Steps { get; } = [];
    public ObservableCollection<MigrationNamedBalanceRow> CurrentRows { get; } = [];
    public ObservableCollection<MigrationBranchCapitalRow> BranchRows { get; } = [];
    public ObservableCollection<Branch> AvailableBranches { get; } = [];
    public ObservableCollection<string> AvailableCategories { get; } = [];
    public ObservableCollection<string> AvailableProducts { get; } = [];
    public ObservableCollection<string> AvailablePricingTypes { get; } = [];
    public ObservableCollection<string> AvailableWarehouses { get; } = [];
    public ObservableCollection<string> AccountKinds { get; } = ["قاصة", "مصرف"];
    public ObservableCollection<MigrationProductPriceEditor> ProductPriceEditors { get; } = [];
    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = new(CurrencyOption.All);

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
            // الفروع ورأس المال والمخازن خطوات إلزامية
            if (CurrentKind is MigrationStepKind.Branches or MigrationStepKind.Capital or MigrationStepKind.Warehouses)
                return false;
            if (CurrentKind == MigrationStepKind.PricingTypes && _needsPricingTypes) return false;
            return CurrentStepInfo.IsOptional;
        }
    }
    public bool HasLastResult => LastImportedCount > 0 || LastSkippedCount > 0;
    public bool HasRows => CurrentRows.Count > 0;
    public int CurrentRowCount => CurrentRows.Count;
    public bool HasRowErrors => CurrentRows.Any(r => !string.IsNullOrWhiteSpace(r.ErrorText));
    public bool HasBranchRows => BranchRows.Count > 0;
    public bool IsBranchesStep => CurrentKind == MigrationStepKind.Branches;
    public bool IsCapitalStep => CurrentKind == MigrationStepKind.Capital;
    public bool IsCustomFormStep => IsBranchesStep || IsCapitalStep;
    public bool IsCashStep => CurrentKind == MigrationStepKind.CashAndBank;
    public bool IsInvestorsStep => CurrentKind == MigrationStepKind.Investors;
    public bool IsWarehousesStep => CurrentKind == MigrationStepKind.Warehouses;
    public bool IsCategoriesStep => CurrentKind == MigrationStepKind.Categories;
    public bool IsProductsStep => CurrentKind == MigrationStepKind.Products;
    public bool IsPricingTypesStep => CurrentKind == MigrationStepKind.PricingTypes;
    public bool IsProductPricingStep => false;
    public bool IsCustomersStep => CurrentKind == MigrationStepKind.Customers;
    public bool IsSuppliersStep => CurrentKind == MigrationStepKind.Suppliers;
    public bool IsExpenseTypesStep => CurrentKind == MigrationStepKind.ExpenseTypes;
    public bool IsInstallmentsStep => CurrentKind == MigrationStepKind.Installments;
    public bool IsSimpleNameStep => IsCategoriesStep || IsPricingTypesStep || IsExpenseTypesStep;
    public bool ShowUsdFields => EnableMultiCurrency;

    public bool AllBranchesHaveCapital =>
        BranchRows.Count > 0 && BranchRows.All(b => b.HasExistingCapital);

    public bool CanEnterBranchCapital =>
        BranchRows.Any(b => !b.HasExistingCapital);
    public bool ShowManualEntryForm => false; // الإدخال اليدوي عبر الجدول الشبيه بـ Excel
    public bool ShowExcelPanel => IsExcelMode && !IsCustomFormStep && !IsCompleted;
    /// <summary>جدول إدخال شبيه Excel لكل الخطوات عدا الفروع والمنتجات (لها جداولها).</summary>
    public bool ShowDataGrid => !IsCustomFormStep && !IsCompleted && !IsProductsStep;
    public bool ShowProductsGrid => IsProductsStep && !IsCompleted;
    public bool ShowExcelLikeToolbar => (ShowDataGrid || ShowProductsGrid) && IsManualEntryMode;
    /// <summary>لا يعتمد على HasRows — وإلا عمود الاسم يبقى مخفياً بعد الإضافة بسبب ربط Visibility على DataGridColumn.</summary>
    public bool ShowColName => !IsCustomFormStep && !IsCompleted;
    public bool ShowColKind => IsCashStep;
    public bool ShowColCategory => IsProductsStep;
    public bool ShowColPhone => IsInvestorsStep || IsCustomersStep || IsSuppliersStep;
    public bool ShowColFileNumber => IsCustomersStep || IsSuppliersStep || IsInstallmentsStep;
    public bool ShowColBarcode => IsProductsStep;
    public bool ShowColAccountNumber => IsCashStep;
    public bool ShowColAmount => IsCashStep || IsInvestorsStep || IsCustomersStep || IsSuppliersStep || IsInstallmentsStep;
    public bool ShowColAmountUsd => EnableMultiCurrency &&
                                    (IsCustomersStep || IsSuppliersStep);
    public bool ShowColProfitPercent => IsInvestorsStep;
    public bool ShowColQuantity => false; // المنتجات تستخدم أعمدة كمية لكل مخزن
    public bool ShowColCost => false;
    public bool ShowColPrices => false;
    public bool ShowColWarehouse => false;
    public bool ShowColBranch => IsWarehousesStep;
    /// <summary>عملة الصف للقاصة فقط — المنتجات تستخدم تكلفة د.ع/$.</summary>
    public bool ShowColCurrency => EnableMultiCurrency && IsCashStep;
    public bool ShowColFxRate => false;
    public bool ShowColInstallments => IsInstallmentsStep;
    public bool ShowColPaidInstallments => IsInstallmentsStep;
    public bool ShowColDate => IsCustomersStep || IsSuppliersStep || IsInstallmentsStep;
    public bool ShowColNotes => IsWarehousesStep || IsCashStep || IsCategoriesStep || IsPricingTypesStep || IsExpenseTypesStep;

    public string ColNameHeader => CurrentKind switch
    {
        MigrationStepKind.Branches => "اسم الفرع",
        MigrationStepKind.Categories => "اسم الصنف",
        MigrationStepKind.Products => "اسم المنتج",
        MigrationStepKind.Warehouses => "اسم المخزن",
        MigrationStepKind.PricingTypes => "نوع التسعير",
        MigrationStepKind.ExpenseTypes => "نوع المصروف",
        MigrationStepKind.CashAndBank => "اسم القاصة / المصرف",
        MigrationStepKind.Investors => "اسم المستثمر",
        MigrationStepKind.Customers => "اسم العميل",
        MigrationStepKind.Suppliers => "اسم المورد",
        MigrationStepKind.Installments => "اسم الزبون",
        _ => "الاسم"
    };

    public string SimpleNameHint => CurrentKind switch
    {
        MigrationStepKind.Categories => "اسم الصنف",
        MigrationStepKind.PricingTypes => "نوع التسعير",
        MigrationStepKind.ExpenseTypes => "نوع المصروف",
        _ => "الاسم"
    };

    public string EntryHint => CurrentKind switch
    {
        MigrationStepKind.Branches => "أضف فروعاً إضافية إن لزم، ثم أدخل رأس المال لكل فرع",
        MigrationStepKind.CashAndBank => "أدخل القاصات/المصارف مباشرة في الجدول كما في Excel — Tab للتنقل",
        MigrationStepKind.Investors => "أدخل المستثمرين في الجدول — الاسم مطلوب، باقي الحقول اختياري",
        MigrationStepKind.Warehouses => "أدخل المخازن في الجدول — اختر الفرع لكل مخزن من عمود الفرع",
        MigrationStepKind.Categories => "أدخل التصنيفات مباشرة في الجدول — صف لكل صنف",
        MigrationStepKind.Products => "أدخل المنتجات مباشرة في الجدول كما في Excel — Tab للتنقل بين الخلايا",
        MigrationStepKind.PricingTypes => "أدخل أنواع التسعير في الجدول — صف لكل نوع",
        MigrationStepKind.Customers => "أدخل العملاء في الجدول — الاسم مطلوب، الرصيد والهاتف اختياري",
        MigrationStepKind.Suppliers => "أدخل الموردين في الجدول — الاسم مطلوب، الرصيد والهاتف اختياري",
        MigrationStepKind.ExpenseTypes => "أدخل أنواع المصاريف في الجدول — صف لكل نوع",
        MigrationStepKind.Installments => "أدخل الأقساط الافتتاحية في الجدول — الزبون والمبلغ وعدد الأقساط",
        _ => "١) نزّل القالب  ٢) استورد الملف  ٣) راجع الجدول  ٤) احفظ"
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
        ICurrentUserService currentUserService,
        IFeatureFlagService featureFlags,
        IBranchService branchService,
        IBusinessSettingsService businessSettingsService,
        IExchangeRateService exchangeRateService,
        IUserPreferencesService preferences,
        IBranchContext branchContext,
        IDbContextFactory<AppDbContext> dbFactory)
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
        _featureFlags = featureFlags;
        _branchService = branchService;
        _businessSettingsService = businessSettingsService;
        _exchangeRateService = exchangeRateService;
        _preferences = preferences;
        _branchContext = branchContext;
        _dbFactory = dbFactory;
        EnableMultiCurrency = _featureFlags.MultiCurrency;
        ShowMultiCurrency = EnableMultiCurrency;
        NewRowCostCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        PageTitle = "معالج النقل من نظام قديم";
        LoadPermissions(currentUserService, "DataImport");
        CurrentRows.CollectionChanged += OnCurrentRowsCollectionChanged;
        RefreshCapitalTotalDisplay();
    }

    public override async Task InitializeAsync()
    {
        EnableMultiCurrency = _featureFlags.MultiCurrency;
        ShowMultiCurrency = EnableMultiCurrency;
        RefreshProductFeatureColumnFlags();
        try
        {
            var latest = await _exchangeRateService.GetLatestAsync();
            if (latest is not null && latest.UsdToIqd > 0)
                InitialUsdToIqd = latest.UsdToIqd;
        }
        catch
        {
            // يُدخل المستخدم السعر يدوياً إن تعذّر التحميل
        }

        await LoadBranchCapitalRowsAsync();
        _needsCapital = BranchRows.Any(r => !r.HasExistingCapital);
        await RefreshPricingNeedFlagsAsync();
        BuildSteps();
        await RefreshLookupsAsync();
        UpdateProgress();
        NotifyStepProps();
        _stepDirty = false;
        await PrepareCurrentStepAsync();
    }

    private void RefreshProductFeatureColumnFlags()
    {
        var flags = _featureFlags.Current;
        ShowProductPharmacyFields = flags.TemplatePharmacy;
        ShowProductCarFields = flags.CarShowroom;
        ShowProductWeightFields = flags.MenuWeight;
        ShowProductDiscountFields = flags.ProductDiscountEnabled;
        ShowProductPurchasePriceColumns = flags.ProductPricingEnabled;
        ProductsGridVersion++;
    }

    partial void OnEnableMultiCurrencyChanged(bool value)
    {
        ShowMultiCurrency = value;
        RefreshCapitalTotalDisplay();
        OnPropertyChanged(nameof(ShowUsdFields));
        OnPropertyChanged(nameof(ShowColCurrency));
        OnPropertyChanged(nameof(ShowColFxRate));
        OnPropertyChanged(nameof(ShowColAmountUsd));
        if (!value)
        {
            NewRowCostCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
            ShowFxRateInput = false;
        }
        MarkStepDirty();
        NotifyStepProps();
    }

    partial void OnInitialUsdToIqdChanged(decimal value)
    {
        RefreshCapitalTotalDisplay();
        MarkStepDirty();
    }

    partial void OnCapitalDateChanged(DateTime value) => MarkStepDirty();
    partial void OnCapitalNotesChanged(string value) => MarkStepDirty();
    partial void OnCapitalAmountChanged(decimal value)
    {
        RefreshCapitalTotalDisplay();
        MarkStepDirty();
    }
    partial void OnCapitalAmountUsdChanged(decimal value)
    {
        RefreshCapitalTotalDisplay();
        MarkStepDirty();
    }
    partial void OnProfitOpeningBalanceChanged(decimal value)
    {
        RefreshCapitalTotalDisplay();
        MarkStepDirty();
    }
    partial void OnProfitOpeningBalanceUsdChanged(decimal value)
    {
        RefreshCapitalTotalDisplay();
        MarkStepDirty();
    }

    private void RefreshCapitalTotalDisplay()
    {
        decimal total;
        decimal profit;
        if (BranchRows.Count > 0)
        {
            total = BranchRows.Sum(ComputeRowCapitalInBaseIqd);
            profit = BranchRows.Sum(ComputeRowProfitInBaseIqd);
        }
        else
        {
            total = ComputeLegacyCapitalInBaseIqd();
            profit = ComputeLegacyProfitInBaseIqd();
        }

        CapitalTotalInIqdDisplay = profit != 0
            ? $"رأس المال المعادل: {AccountingCurrencyHelper.Format(total, AccountingCurrency.IQD)} — الأرباح الافتتاحية المعادلة: {AccountingCurrencyHelper.Format(profit, AccountingCurrency.IQD)}"
            : $"رأس المال المعادل بالدينار (العملة الأساسية): {AccountingCurrencyHelper.Format(total, AccountingCurrency.IQD)}";
    }

    private decimal ComputeLegacyCapitalInBaseIqd()
    {
        var iqd = AccountingCurrencyHelper.RoundIqd(CapitalAmount);
        if (!EnableMultiCurrency || CapitalAmountUsd <= 0 || InitialUsdToIqd <= 0)
            return iqd;
        return iqd + AccountingCurrencyHelper.ToBaseIqd(
            AccountingCurrencyHelper.RoundUsd(CapitalAmountUsd),
            AccountingCurrency.USD,
            InitialUsdToIqd);
    }

    private decimal ComputeLegacyProfitInBaseIqd()
    {
        var iqd = AccountingCurrencyHelper.RoundIqd(ProfitOpeningBalance);
        if (!EnableMultiCurrency || ProfitOpeningBalanceUsd == 0 || InitialUsdToIqd <= 0)
            return iqd;
        return iqd + AccountingCurrencyHelper.ToBaseIqd(
            AccountingCurrencyHelper.RoundUsd(ProfitOpeningBalanceUsd),
            AccountingCurrency.USD,
            InitialUsdToIqd);
    }

    private decimal ComputeRowCapitalInBaseIqd(MigrationBranchCapitalRow row)
    {
        var iqd = AccountingCurrencyHelper.RoundIqd(row.CapitalAmount);
        if (!EnableMultiCurrency || row.CapitalAmountUsd <= 0 || InitialUsdToIqd <= 0)
            return iqd;
        return iqd + AccountingCurrencyHelper.ToBaseIqd(
            AccountingCurrencyHelper.RoundUsd(row.CapitalAmountUsd),
            AccountingCurrency.USD,
            InitialUsdToIqd);
    }

    private decimal ComputeRowProfitInBaseIqd(MigrationBranchCapitalRow row)
    {
        var iqd = AccountingCurrencyHelper.RoundIqd(row.ProfitOpeningBalance);
        if (!EnableMultiCurrency || row.ProfitOpeningBalanceUsd == 0 || InitialUsdToIqd <= 0)
            return iqd;
        return iqd + AccountingCurrencyHelper.ToBaseIqd(
            AccountingCurrencyHelper.RoundUsd(row.ProfitOpeningBalanceUsd),
            AccountingCurrency.USD,
            InitialUsdToIqd);
    }

    private async Task LoadBranchCapitalRowsAsync()
    {
        foreach (var existing in BranchRows.ToList())
            existing.PropertyChanged -= OnBranchRowPropertyChanged;
        BranchRows.Clear();

        var branches = await _branchService.GetAllAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;

        var capitalEntries = await db.CapitalEntries
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.BranchId, c.Type, c.Amount })
            .ToListAsync();

        foreach (var branch in branches)
        {
            var branchEntries = capitalEntries.Where(c => c.BranchId == branch.Id).ToList();
            var hasInitial = branchEntries.Any(c => c.Type == CapitalEntryType.Initial);
            var hasProfit = branchEntries.Any(c => c.Type == CapitalEntryType.ProfitOpeningBalance);
            var row = new MigrationBranchCapitalRow
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                BranchCode = branch.Code,
                IsMain = branch.IsMain,
                IsExisting = true,
                HasExistingCapital = hasInitial,
                HasExistingProfit = hasProfit,
                CapitalAmount = hasInitial
                    ? branchEntries.Where(c => c.Type == CapitalEntryType.Initial).Sum(c => c.Amount)
                    : 0,
                ProfitOpeningBalance = hasProfit
                    ? branchEntries.Where(c => c.Type == CapitalEntryType.ProfitOpeningBalance).Sum(c => c.Amount)
                    : 0
            };
            row.PropertyChanged += OnBranchRowPropertyChanged;
            BranchRows.Add(row);
        }

        OnPropertyChanged(nameof(AllBranchesHaveCapital));
        OnPropertyChanged(nameof(CanEnterBranchCapital));
        OnPropertyChanged(nameof(HasBranchRows));
        RefreshCapitalTotalDisplay();
    }

    private void OnBranchRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MigrationBranchCapitalRow.CapitalAmount)
            or nameof(MigrationBranchCapitalRow.CapitalAmountUsd)
            or nameof(MigrationBranchCapitalRow.ProfitOpeningBalance)
            or nameof(MigrationBranchCapitalRow.ProfitOpeningBalanceUsd))
        {
            RefreshCapitalTotalDisplay();
            MarkStepDirty();
        }
    }

    private bool _suppressStepDirty;

    private void MarkStepDirty()
    {
        if (_suppressStepDirty || CurrentKind is null || IsCompleted) return;
        _stepDirty = true;
        if (!_savedSteps.Remove(CurrentKind.Value))
            return;

        if (CurrentStepInfo is not null)
            CurrentStepInfo.IsSaved = false;
        UpdateProgress();
        SyncStepFlags();
    }

    private void OnCurrentRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MigrationNamedBalanceRow row in e.OldItems)
                row.PropertyChanged -= OnCurrentRowPropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (MigrationNamedBalanceRow row in e.NewItems)
            {
                row.PropertyChanged -= OnCurrentRowPropertyChanged;
                row.PropertyChanged += OnCurrentRowPropertyChanged;
                if (IsProductsStep)
                    SyncProductRowStructure(row);
                else
                    ApplyNewRowDefaults(row);
            }
        }

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(CurrentRowCount));
        OnPropertyChanged(nameof(HasRowErrors));
        OnPropertyChanged(nameof(ShowDataGrid));
        OnPropertyChanged(nameof(ShowProductsGrid));
        OnPropertyChanged(nameof(ShowExcelLikeToolbar));
        MarkStepDirty();
    }

    private void OnCurrentRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => MarkStepDirty();

    private async Task RefreshPricingNeedFlagsAsync()
    {
        var types = (await _pricingTypeService.GetActiveAsync()).ToList();
        _needsPricingTypes = types.Count == 0;
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

        // الفروع + رأس المال دائماً أولاً (مطلوب)
        Add(MigrationStepKind.Branches, "الفروع ورأس المال",
            "الفرع الرئيسي موجود — أضف فروعاً إضافية إن لزم، وأدخل رأس المال والأرباح لكل فرع، مع خيار تفعيل الدولار.",
            PackIconKind.StoreMarker, optional: false);

        Add(MigrationStepKind.Categories, "التصنيفات",
            "أضف تصنيفات المنتجات أولاً لتسهيل إدخال المنتجات لاحقاً.",
            PackIconKind.Shape);
        Add(MigrationStepKind.Warehouses, "المخازن",
            "اربط كل مخزن بفرع — المخزن مطلوب قبل المنتجات والأرصدة والأقساط.",
            PackIconKind.Warehouse, optional: false);
        Add(MigrationStepKind.PricingTypes, "أنواع التسعير",
            _needsPricingTypes
                ? "لا توجد أنواع تسعير بعد — أضفها الآن (مثل: سعر مفرد، جملة، وكيل)."
                : "أضف أنواع تسعير إضافية أو تخطَّ إن كانت مكتملة.",
            PackIconKind.TagMultiple,
            optional: !_needsPricingTypes);
        Add(MigrationStepKind.Products, "المنتجات",
            "أدخل المنتج بالكامل مع الكمية والتكلفة وأسعار كل نوع تسعير في نفس الخطوة.",
            PackIconKind.PackageVariant);
        Add(MigrationStepKind.CashAndBank, "القاصات والمصرف",
            "أنشئ القاصات وحسابات المصرف مع أرصدتها الافتتاحية.",
            PackIconKind.SafeSquareOutline);
        Add(MigrationStepKind.Investors, "المستثمرون",
            "أضف المستثمرين — الرصيد الافتتاحي اختياري.",
            PackIconKind.AccountCash);
        Add(MigrationStepKind.Customers, "العملاء",
            "أضف العملاء — الرصيد الآجل الافتتاحي اختياري.",
            PackIconKind.AccountGroup);
        Add(MigrationStepKind.Suppliers, "الموردون",
            "أضف الموردين — الرصيد الآجل الافتتاحي اختياري.",
            PackIconKind.TruckDelivery);
        Add(MigrationStepKind.ExpenseTypes, "أنواع المصاريف",
            "أنواع المصاريف الافتتاحية للنظام.",
            PackIconKind.CashMinus);
        Add(MigrationStepKind.Installments, "الأقساط",
            "أرصدة الأقساط الافتتاحية بنفس منطق شاشة الأرصدة الافتتاحية.",
            PackIconKind.CashClock);

        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].StepNumber = i + 1;
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
        PersistDisplayedRows();
        SyncStepFlags();
        SelectedFilePath = null;
        LastImportedCount = 0;
        LastSkippedCount = 0;
        StatusMessage = string.Empty;
        ResetNewRowFields();
        IsManualEntryMode = true;
        IsExcelMode = false;
        StepTransitionToken++;
        UpdateProgress();

        _navCts?.Cancel();
        _navCts?.Dispose();
        _navCts = new CancellationTokenSource();
        var token = _navCts.Token;

        LoadRowsForCurrentStep();
        NotifyStepProps();
        _ = PrepareStepAfterNavigationAsync(token);
    }

    private void PersistDisplayedRows()
    {
        if (_displayedKind is not { } kind) return;
        _rowsByStep[kind] = CurrentRows.ToList();
    }

    private void LoadRowsForCurrentStep()
    {
        _suppressStepDirty = true;
        try
        {
            CurrentRows.Clear();
            _displayedKind = CurrentKind;
            if (CurrentKind is not { } kind) return;
            if (!_rowsByStep.TryGetValue(kind, out var bag)) return;
            foreach (var row in bag)
                CurrentRows.Add(row);
        }
        finally
        {
            _suppressStepDirty = false;
        }
    }

    private async Task PrepareStepAfterNavigationAsync(CancellationToken ct)
    {
        try
        {
            await RefreshLookupsAsync();
            if (ct.IsCancellationRequested) return;
            await PrepareCurrentStepAsync(ct);
            if (ct.IsCancellationRequested) return;
            NotifyStepProps();
        }
        catch (OperationCanceledException)
        {
            // تنقّل سريع — تجاهل
        }
    }

    private async Task PrepareCurrentStepAsync(CancellationToken ct = default)
    {
        if (CurrentKind is null || IsCompleted) return;

        _suppressStepDirty = true;
        try
        {
            await HydrateExistingIntoCurrentRowsAsync(ct);
            if (ct.IsCancellationRequested) return;

            if (CurrentKind == MigrationStepKind.PricingTypes)
            {
                await RefreshPricingNeedFlagsAsync();
                if (ct.IsCancellationRequested) return;
                UpdatePricingStepOptionality();
                if (CurrentRows.Count == 0 && _needsPricingTypes)
                {
                    foreach (var name in new[] { "سعر مفرد", "جملة", "وكيل" })
                        CurrentRows.Add(new MigrationNamedBalanceRow { Name = name });
                    PersistDisplayedRows();
                    StatusMessage = "تم اقتراح أنواع تسعير افتراضية — عدّلها في الجدول ثم احفظ";
                }
                EnsureBlankRowsForCurrentStep();
            }
            else if (CurrentKind == MigrationStepKind.Products)
            {
                SyncProductPriceEditors();
                EnsureProductRowsReady();
            }
            else if (CurrentKind == MigrationStepKind.CashAndBank && CurrentRows.Count == 0)
            {
                CurrentRows.Add(new MigrationNamedBalanceRow
                {
                    Name = "قاصة دينار", Kind = "قاصة", Amount = 0,
                    CostCurrency = AccountingCurrency.IQD, Notes = "قابلة للتعديل"
                });
                if (EnableMultiCurrency)
                {
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Name = "قاصة دولار", Kind = "قاصة", Amount = 0,
                        CostCurrency = AccountingCurrency.USD, Notes = "قابلة للتعديل"
                    });
                }
                PersistDisplayedRows();
                EnsureBlankRowsForCurrentStep(extraBlank: 5);
                StatusMessage = EnableMultiCurrency
                    ? "قاصتان افتراضيتان — عدّل في الجدول ثم احفظ"
                    : "قاصة دينار افتراضية — عدّل في الجدول ثم احفظ";
            }
            else if (!IsCustomFormStep)
            {
                EnsureBlankRowsForCurrentStep();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            StatusMessage = $"تعذّر تجهيز الخطوة: {ex.Message}";
        }
        finally
        {
            _suppressStepDirty = false;
        }

        _stepDirty = false;
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(CurrentRowCount));
        OnPropertyChanged(nameof(HasRowErrors));
        OnPropertyChanged(nameof(ShowDataGrid));
        OnPropertyChanged(nameof(CanSkip));
        OnPropertyChanged(nameof(StepDescription));
    }

    /// <summary>يعرض المدخلات الموجودة مسبقاً في الجدول لتجنب التعارض مع الإدخالات الجديدة.</summary>
    private async Task HydrateExistingIntoCurrentRowsAsync(CancellationToken ct)
    {
        if (CurrentKind is null or MigrationStepKind.Branches or MigrationStepKind.Capital
            or MigrationStepKind.Installments)
            return;

        var existing = await LoadExistingRowsForStepAsync(CurrentKind.Value, ct);
        if (ct.IsCancellationRequested || existing.Count == 0) return;

        var added = 0;
        _suppressStepDirty = true;
        try
        {
            foreach (var row in existing)
            {
                var already = CurrentRows.Any(r =>
                    (row.SourceId is int sid && r.SourceId == sid &&
                     string.Equals(r.Kind, row.Kind, StringComparison.OrdinalIgnoreCase))
                    || (string.Equals(r.Name?.Trim(), row.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(r.Kind, row.Kind, StringComparison.OrdinalIgnoreCase)));
                if (already) continue;
                CurrentRows.Add(row);
                added++;
            }
        }
        finally
        {
            _suppressStepDirty = false;
        }

        if (added > 0)
        {
            PersistDisplayedRows();
            StatusMessage = $"تم عرض {added} مدخل موجود مسبقاً — يمكنك تعديله أو إضافة صفوف جديدة";
        }
    }

    private async Task<List<MigrationNamedBalanceRow>> LoadExistingRowsForStepAsync(
        MigrationStepKind kind, CancellationToken ct)
    {
        var list = new List<MigrationNamedBalanceRow>();
        switch (kind)
        {
            case MigrationStepKind.Categories:
                foreach (var c in await _unitOfWork.Categories.GetAllAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = c.Id, IsExisting = true, Name = c.Name,
                        Notes = "موجود مسبقاً"
                    });
                }
                break;

            case MigrationStepKind.Warehouses:
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                db.BypassBranchFilter = true;
                var allowedBranchIds = GetAllowedBranchIds();
                var warehouses = await db.Warehouses.IgnoreQueryFilters()
                    .Where(w => !w.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync(ct);
                var branchNames = await db.Branches.IgnoreQueryFilters()
                    .AsNoTracking()
                    .ToDictionaryAsync(b => b.Id, b => b.Name, ct);
                // اعرض فقط مخازن الفروع المسموحة للمستخدم
                foreach (var w in warehouses.Where(w => allowedBranchIds.Contains(w.BranchId)))
                {
                    if (ct.IsCancellationRequested) break;
                    branchNames.TryGetValue(w.BranchId, out var branchName);
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = w.Id,
                        IsExisting = true,
                        Name = w.Name,
                        BranchId = w.BranchId,
                        BranchName = branchName,
                        Notes = string.IsNullOrWhiteSpace(w.Location) ? null : w.Location
                    });
                }
                break;
            }

            case MigrationStepKind.PricingTypes:
                foreach (var t in await _pricingTypeService.GetActiveAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = t.Id, IsExisting = true, Name = t.Name,
                        Notes = "موجود مسبقاً"
                    });
                }
                break;

            case MigrationStepKind.Products:
                var stocks = (await _unitOfWork.WarehouseStocks.GetAllAsync()).ToList();
                var cats = (await _unitOfWork.Categories.GetAllAsync()).ToDictionary(c => c.Id, c => c.Name);
                var whs = (await _unitOfWork.Warehouses.GetAllAsync()).ToDictionary(w => w.Id, w => w.Name);
                var allPrices = (await _unitOfWork.ProductPrices.GetAllAsync()).ToList();
                var pricingLookup = (await _pricingTypeService.GetActiveAsync())
                    .ToDictionary(t => t.Id, t => t.Name);
                foreach (var p in await _unitOfWork.Products.GetAllAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    var stock = stocks.Where(s => s.ProductId == p.Id).OrderByDescending(s => s.Quantity).FirstOrDefault();
                    var row = new MigrationNamedBalanceRow
                    {
                        SourceId = p.Id, IsExisting = true, Name = p.Name,
                        Barcode = p.Barcode,
                        CategoryName = cats.GetValueOrDefault(p.CategoryId),
                        Quantity = stock?.Quantity ?? 0,
                        UnitCost = stock?.UnitCost ?? 0,
                        WarehouseName = stock is not null ? whs.GetValueOrDefault(stock.WarehouseId) : null,
                        Notes = "موجود مسبقاً",
                        CostCurrency = AccountingCurrency.IQD
                    };
                    foreach (var price in allPrices.Where(pp => pp.ProductId == p.Id))
                    {
                        if (!pricingLookup.TryGetValue(price.PricingTypeId, out var typeName))
                            continue;
                        row.ProductPrices.Add(new MigrationProductPriceCell
                        {
                            PricingTypeName = typeName,
                            SalePrice = price.SalePrice,
                            SalePriceUsd = price.SalePriceUsd,
                            PurchasePrice = price.PurchasePrice,
                            PurchasePriceUsd = price.PurchasePriceUsd
                        });
                    }
                    row.RebuildPricesSummary();
                    list.Add(row);
                }
                break;

            case MigrationStepKind.CashAndBank:
                foreach (var c in await _unitOfWork.CashBoxes.GetAllAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = c.Id, IsExisting = true, Name = c.Name,
                        Kind = "قاصة", Amount = c.Balance, CostCurrency = c.Currency,
                        Notes = "موجود مسبقاً — قابل للتعديل"
                    });
                }
                foreach (var b in await _unitOfWork.BankAccounts.GetAllAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = b.Id, IsExisting = true, Name = b.Name,
                        Kind = "مصرف", Amount = b.Balance, AccountNumber = b.AccountNumber,
                        CostCurrency = b.Currency,
                        Notes = "موجود مسبقاً — قابل للتعديل"
                    });
                }
                break;

            case MigrationStepKind.Investors:
                foreach (var i in await _investorService.GetAllInvestorsAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = i.Id, IsExisting = true, Name = i.Name,
                        Phone = i.Phone, ProfitPercentage = i.ProfitPercentage,
                        Amount = i.OpeningBalance,
                        Notes = "موجود مسبقاً"
                    });
                }
                break;

            case MigrationStepKind.Customers:
                foreach (var c in await _unitOfWork.Customers.GetAllAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = c.Id, IsExisting = true, Name = c.Name,
                        Phone = c.Phone, Notes = "موجود مسبقاً"
                    });
                }
                break;

            case MigrationStepKind.Suppliers:
                foreach (var s in await _unitOfWork.Suppliers.GetAllAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = s.Id, IsExisting = true, Name = s.Name,
                        Phone = s.Phone, Notes = "موجود مسبقاً"
                    });
                }
                break;

            case MigrationStepKind.ExpenseTypes:
                await _expenseService.EnsureDefaultExpenseTypesAsync();
                foreach (var e in await _expenseService.GetAllExpenseTypesAsync())
                {
                    if (ct.IsCancellationRequested) break;
                    list.Add(new MigrationNamedBalanceRow
                    {
                        SourceId = e.Id, IsExisting = true, Name = e.Name,
                        Notes = "موجود مسبقاً"
                    });
                }
                break;
        }

        return list;
    }

    private void UpdatePricingStepOptionality()
    {
        foreach (var step in Steps.Where(s => s.Kind == MigrationStepKind.PricingTypes))
        {
            step.IsOptional = !_needsPricingTypes;
            step.Description = _needsPricingTypes
                ? "لا توجد أنواع تسعير بعد — أضفها الآن (مثل: سعر مفرد، جملة، وكيل)."
                : "أضف أنواع تسعير إضافية أو تخطَّ إن كانت مكتملة.";
        }
    }

    private void SyncProductPriceEditors()
    {
        ProductPriceEditors.Clear();
        foreach (var name in AvailablePricingTypes)
            ProductPriceEditors.Add(new MigrationProductPriceEditor { PricingTypeName = name });
    }

    private void EnsureProductRowsReady()
    {
        SyncAllProductRowStructures();
        if (CurrentRows.Count == 0)
        {
            for (var i = 0; i < 8; i++)
            {
                var row = new MigrationNamedBalanceRow { IsValid = true };
                SyncProductRowStructure(row);
                CurrentRows.Add(row);
            }
            PersistDisplayedRows();
        }

        ProductsGridVersion++;
    }

    /// <summary>يملأ الجدول بصفوف فارغة جاهزة للإدخال السريع كما في Excel.</summary>
    private void EnsureBlankRowsForCurrentStep(int extraBlank = 8)
    {
        if (IsProductsStep || IsCustomFormStep || CurrentKind is null)
            return;

        var blankCount = CurrentRows.Count(r => string.IsNullOrWhiteSpace(r.Name));
        var toAdd = Math.Max(0, extraBlank - blankCount);
        for (var i = 0; i < toAdd; i++)
        {
            var row = CreateBlankRowForCurrentStep();
            CurrentRows.Add(row);
        }

        if (toAdd > 0)
            PersistDisplayedRows();
    }

    private MigrationNamedBalanceRow CreateBlankRowForCurrentStep()
    {
        var row = new MigrationNamedBalanceRow { IsValid = true };
        ApplyNewRowDefaults(row);
        return row;
    }

    private void ApplyNewRowDefaults(MigrationNamedBalanceRow row)
    {
        if (IsCashStep && string.IsNullOrWhiteSpace(row.Kind))
            row.Kind = "قاصة";
        if (IsInstallmentsStep && row.NumberOfInstallments <= 0)
            row.NumberOfInstallments = 1;
        if (row.Date == default)
            row.Date = DateTime.Today;
        if (IsWarehousesStep && row.BranchId is null
            && SelectedBranchForWarehouse is not null)
        {
            row.BranchId = SelectedBranchForWarehouse.Id;
            row.BranchName = SelectedBranchForWarehouse.Name;
        }
    }

    partial void OnSelectedBranchForWarehouseChanged(Branch? value)
    {
        if (!IsWarehousesStep || value is null)
            return;

        // طبّق الفرع المختار على الصفوف الفارغة فقط (لا نكتب فوق اختيار موجود)
        foreach (var row in CurrentRows.Where(r =>
                     r.BranchId is null && string.IsNullOrWhiteSpace(r.BranchName)))
        {
            row.BranchId = value.Id;
            row.BranchName = value.Name;
        }
    }

    private void SyncAllProductRowStructures()
    {
        foreach (var row in CurrentRows)
            SyncProductRowStructure(row);
    }

    private void SyncProductRowStructure(MigrationNamedBalanceRow row)
    {
        var pricing = AvailablePricingTypes.Count > 0
            ? AvailablePricingTypes.ToList()
            : ["سعر مفرد"];
        var warehouses = AvailableWarehouses.Count > 0
            ? AvailableWarehouses.ToList()
            : [];
        row.EnsureProductStructure(pricing, warehouses, EnableMultiCurrency);
    }

    public IReadOnlyList<string> GetProductPricingTypeNames()
        => AvailablePricingTypes.Count > 0
            ? AvailablePricingTypes.ToList()
            : ["سعر مفرد"];

    public IReadOnlyList<string> GetProductWarehouseNames()
        => AvailableWarehouses.ToList();

    partial void OnNewRowCostCurrencyOptionChanged(CurrencyOption? value)
    {
        ShowFxRateInput = EnableMultiCurrency && value?.Currency == AccountingCurrency.USD;
        if (!ShowFxRateInput) NewRowFxRate = 1m;
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
        OnPropertyChanged(nameof(CurrentRowCount));
        OnPropertyChanged(nameof(HasRowErrors));
        OnPropertyChanged(nameof(HasBranchRows));
        OnPropertyChanged(nameof(AllBranchesHaveCapital));
        OnPropertyChanged(nameof(CanEnterBranchCapital));
        OnPropertyChanged(nameof(ColNameHeader));
        OnPropertyChanged(nameof(SimpleNameHint));
        OnPropertyChanged(nameof(IsBranchesStep));
        OnPropertyChanged(nameof(IsCapitalStep));
        OnPropertyChanged(nameof(IsCustomFormStep));
        OnPropertyChanged(nameof(ShowUsdFields));
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
        OnPropertyChanged(nameof(ShowProductsGrid));
        OnPropertyChanged(nameof(ShowExcelLikeToolbar));
        OnPropertyChanged(nameof(ShowColName));
        OnPropertyChanged(nameof(ShowColKind));
        OnPropertyChanged(nameof(ShowColCategory));
        OnPropertyChanged(nameof(ShowColPhone));
        OnPropertyChanged(nameof(ShowColFileNumber));
        OnPropertyChanged(nameof(ShowColBarcode));
        OnPropertyChanged(nameof(ShowColAccountNumber));
        OnPropertyChanged(nameof(ShowColAmount));
        OnPropertyChanged(nameof(ShowColAmountUsd));
        OnPropertyChanged(nameof(ShowColProfitPercent));
        OnPropertyChanged(nameof(ShowColQuantity));
        OnPropertyChanged(nameof(ShowColCost));
        OnPropertyChanged(nameof(ShowColPrices));
        OnPropertyChanged(nameof(ShowColWarehouse));
        OnPropertyChanged(nameof(ShowColBranch));
        OnPropertyChanged(nameof(ShowColCurrency));
        OnPropertyChanged(nameof(ShowColFxRate));
        OnPropertyChanged(nameof(ShowColInstallments));
        OnPropertyChanged(nameof(ShowColPaidInstallments));
        OnPropertyChanged(nameof(ShowColDate));
        OnPropertyChanged(nameof(ShowColNotes));
        OnPropertyChanged(nameof(EntryHint));
    }

    private void ResetNewRowFields()
    {
        NewRowName = string.Empty;
        NewRowPhone = null;
        NewRowExtra = null;
        NewRowAmount = 0;
        NewRowAmountUsd = 0;
        NewRowAmount2 = 0;
        NewRowAmount3 = 0;
        NewRowInt = 1;
        NewRowInt2 = 0;
        NewRowDate = DateTime.Today;
        NewRowKind = "قاصة";
        NewRowCostCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        NewRowFxRate = 1m;
        foreach (var editor in ProductPriceEditors)
        {
            editor.SalePrice = 0;
            editor.SalePriceUsd = 0;
            editor.PurchasePrice = 0;
            editor.PurchasePriceUsd = 0;
        }
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

            AvailableBranches.Clear();
            var allowedBranchIds = GetAllowedBranchIds();
            foreach (var b in await _branchService.GetActiveAsync())
            {
                if (allowedBranchIds.Contains(b.Id))
                    AvailableBranches.Add(b);
            }
            // فرع الجلسة الحالي إن كان مسموحاً، وإلا فرع واحد تلقائياً
            if (SelectedBranchForWarehouse is null
                && _branchContext.CurrentBranchId is int currentId
                && AvailableBranches.Any(b => b.Id == currentId))
            {
                SelectedBranchForWarehouse = AvailableBranches.First(b => b.Id == currentId);
            }
            else if (SelectedBranchForWarehouse is null && AvailableBranches.Count == 1)
                SelectedBranchForWarehouse = AvailableBranches[0];
            else if (SelectedBranchForWarehouse is not null
                     && AvailableBranches.All(b => b.Id != SelectedBranchForWarehouse.Id))
                SelectedBranchForWarehouse = AvailableBranches.FirstOrDefault(
                    b => _branchContext.CurrentBranchId is int cid && b.Id == cid)
                    ?? AvailableBranches.FirstOrDefault();

            AvailableWarehouses.Clear();
            await using (var db = await _dbFactory.CreateDbContextAsync())
            {
                db.BypassBranchFilter = true;
                var warehouses = await db.Warehouses.IgnoreQueryFilters()
                    .Where(w => !w.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
                var branchNames = await db.Branches.IgnoreQueryFilters()
                    .AsNoTracking()
                    .ToDictionaryAsync(b => b.Id, b => b.Name);
                foreach (var w in warehouses
                             .Where(w => allowedBranchIds.Contains(w.BranchId))
                             .OrderBy(w => w.Name))
                {
                    branchNames.TryGetValue(w.BranchId, out var branchName);
                    AvailableWarehouses.Add(string.IsNullOrWhiteSpace(branchName)
                        ? w.Name
                        : $"{w.Name} ({branchName})");
                }
            }

            if (string.IsNullOrWhiteSpace(SelectedWarehouseName)
                || !AvailableWarehouses.Contains(SelectedWarehouseName))
            {
                SelectedWarehouseName = AvailableWarehouses.Count > 0
                    ? AvailableWarehouses[0]
                    : null;
            }

            if (IsProductsStep)
            {
                SyncProductPriceEditors();
                SyncAllProductRowStructures();
                ProductsGridVersion++;
            }
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
        if (IsProductsStep)
            EnsureProductRowsReady();
        else if (!IsCustomFormStep)
            EnsureBlankRowsForCurrentStep();
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

        // فقط الرجوع لخطوات سابقة أو المحفوظة — منع القفز للأمام بدون حفظ ما قبلها
        if (index > CurrentStepIndex)
        {
            for (var i = 0; i < index; i++)
            {
                if (!_savedSteps.Contains(Steps[i].Kind))
                {
                    Warn("احفظ الخطوات السابقة بالترتيب قبل الانتقال لهذه الخطوة");
                    return;
                }
            }
        }

        CurrentStepIndex = index;
    }

    [RelayCommand]
    private void AddManualRow()
    {
        if (CurrentKind is null || IsCustomFormStep) return;
        var kind = CurrentKind.Value;
        var row = new MigrationNamedBalanceRow();

        switch (kind)
        {
            case MigrationStepKind.CashAndBank:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم القاصة أو المصرف"); return; }
                row.Name = NewRowName.Trim();
                row.Amount = NewRowAmount;
                row.Kind = NewRowKind.Contains("مصرف", StringComparison.Ordinal)
                           || NewRowKind.Contains("Bank", StringComparison.OrdinalIgnoreCase)
                    ? "مصرف" : "قاصة";
                row.AccountNumber = NewRowExtra;
                row.CostCurrency = NewRowCostCurrencyOption?.Currency ?? AccountingCurrency.IQD;
                row.Notes = NewRowKind;
                break;
            case MigrationStepKind.Investors:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم المستثمر"); return; }
                if (NewRowAmount < 0)
                {
                    Warn("الرصيد الافتتاحي لا يمكن أن يكون سالباً");
                    return;
                }
                row.Name = NewRowName.Trim();
                row.Phone = NewRowPhone;
                row.ProfitPercentage = NewRowAmount2;
                row.Amount = NewRowAmount;
                row.AmountUsd = 0m;
                row.CostCurrency = AccountingCurrency.IQD;
                row.FxRate = 1m;
                break;
            case MigrationStepKind.Warehouses:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل اسم المخزن"); return; }
                if (SelectedBranchForWarehouse is null)
                {
                    Warn("اختر الفرع للمخزن");
                    return;
                }
                row.Name = NewRowName.Trim();
                row.Notes = NewRowExtra;
                row.BranchId = SelectedBranchForWarehouse.Id;
                row.BranchName = SelectedBranchForWarehouse.Name;
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
                row.Quantity = NewRowAmount2;
                row.UnitCost = NewRowAmount3;
                row.WarehouseName = SelectedWarehouseName;
                row.CostCurrency = NewRowCostCurrencyOption?.Currency ?? AccountingCurrency.IQD;
                row.FxRate = row.CostCurrency == AccountingCurrency.USD
                    ? (NewRowFxRate > 0 ? NewRowFxRate : 0m)
                    : 1m;
                if (row.CostCurrency == AccountingCurrency.USD && row.UnitCost > 0 && row.FxRate <= 0)
                {
                    Warn("أدخل سعر الصرف (دولار→دينار) لتكلفة المنتج بالدولار");
                    return;
                }
                foreach (var editor in ProductPriceEditors.Where(e =>
                             e.SalePrice > 0 || e.SalePriceUsd > 0 || e.PurchasePrice > 0 || e.PurchasePriceUsd > 0))
                {
                    row.ProductPrices.Add(new MigrationProductPriceCell
                    {
                        PricingTypeName = editor.PricingTypeName,
                        SalePrice = editor.SalePrice,
                        SalePriceUsd = EnableMultiCurrency ? editor.SalePriceUsd : 0m,
                        PurchasePrice = editor.PurchasePrice,
                        PurchasePriceUsd = EnableMultiCurrency ? editor.PurchasePriceUsd : 0m
                    });
                }
                // توافق خلفي: أول سعر بيع يُخزَّن أيضاً في UnitPrice
                var primary = row.ProductPrices.FirstOrDefault(p => p.SalePrice > 0 || p.SalePriceUsd > 0);
                if (primary is not null)
                {
                    row.UnitPrice = primary.SalePrice > 0 ? primary.SalePrice : primary.SalePriceUsd;
                    row.Amount = row.UnitPrice;
                    row.SalePrice = row.UnitPrice;
                }
                else if (NewRowAmount > 0)
                {
                    row.UnitPrice = NewRowAmount;
                    row.Amount = NewRowAmount;
                }
                row.RebuildPricesSummary();
                break;
            case MigrationStepKind.Customers:
            case MigrationStepKind.Suppliers:
                if (string.IsNullOrWhiteSpace(NewRowName)) { Warn("أدخل الاسم"); return; }
                if (NewRowAmount < 0 || NewRowAmountUsd < 0)
                {
                    Warn("الرصيد الافتتاحي لا يمكن أن يكون سالباً");
                    return;
                }
                if (EnableMultiCurrency && NewRowAmountUsd > 0 && InitialUsdToIqd <= 0)
                {
                    Warn("أدخل سعر الصرف الافتتاحي في خطوة الفروع قبل إدخال رصيد بالدولار");
                    return;
                }
                row.Name = NewRowName.Trim();
                row.Phone = NewRowPhone;
                row.FileNumber = NewRowExtra;
                row.Amount = NewRowAmount;
                row.AmountUsd = EnableMultiCurrency ? NewRowAmountUsd : 0m;
                row.Date = NewRowDate;
                row.CostCurrency = AccountingCurrency.IQD;
                row.FxRate = 1m;
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

        row.IsValid = true;
        row.ErrorText = null;
        CurrentRows.Add(row);
        PersistDisplayedRows();
        ResetNewRowFields();
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ShowDataGrid));
        NotifyStepProps();
        // إعادة إشعار أعمدة الخطوة الحالية بعد الإضافة (لتحديث Visibility في الواجهة)
        OnPropertyChanged(nameof(ShowColAmount));
        OnPropertyChanged(nameof(ShowColCategory));
        OnPropertyChanged(nameof(ShowColQuantity));
        OnPropertyChanged(nameof(ShowColCost));
        OnPropertyChanged(nameof(ShowColPrices));
        OnPropertyChanged(nameof(ShowColWarehouse));
        OnPropertyChanged(nameof(ShowColBranch));
        OnPropertyChanged(nameof(ShowColBarcode));
        OnPropertyChanged(nameof(ShowColNotes));
        OnPropertyChanged(nameof(ShowColCurrency));
        StatusMessage = $"صفوف جاهزة: {CurrentRows.Count}";
    }

    [RelayCommand]
    private void RemoveRow(MigrationNamedBalanceRow? row)
    {
        if (row is null) return;
        CurrentRows.Remove(row);
        PersistDisplayedRows();
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ShowDataGrid));
        NotifyStepProps();
    }

    [RelayCommand]
    private void ClearRows()
    {
        if (CurrentRows.Count == 0) return;
        if (!BeautifulMessageDialog.ShowConfirm("مسح جميع صفوف هذه الخطوة؟", "تأكيد"))
            return;
        CurrentRows.Clear();
        PersistDisplayedRows();
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ShowDataGrid));
        NotifyStepProps();
        StatusMessage = "تم مسح الصفوف";
        if (IsProductsStep)
            EnsureProductRowsReady();
        else if (!IsCustomFormStep)
            EnsureBlankRowsForCurrentStep();
    }

    [RelayCommand]
    private void AddProductBlankRows() => AddBlankRows();

    [RelayCommand]
    private void AddBlankRows()
    {
        if (IsCustomFormStep || CurrentKind is null) return;

        if (IsProductsStep)
        {
            for (var i = 0; i < 5; i++)
            {
                var row = new MigrationNamedBalanceRow { IsValid = true };
                SyncProductRowStructure(row);
                CurrentRows.Add(row);
            }
            PersistDisplayedRows();
            ProductsGridVersion++;
        }
        else
        {
            for (var i = 0; i < 5; i++)
                CurrentRows.Add(CreateBlankRowForCurrentStep());
            PersistDisplayedRows();
        }

        StatusMessage = $"صفوف جاهزة: {CurrentRows.Count}";
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
            PersistDisplayedRows();
            StatusMessage = CurrentRows.Count == 0
                ? "الملف لا يحتوي على بيانات صالحة"
                : $"تم استيراد {CurrentRows.Count} صف — راجع المعاينة ثم اضغط حفظ الخطوة";
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

    private async Task<byte[]> BuildTemplateBytesAsync(MigrationStepKind kind)
    {
        if (kind == MigrationStepKind.Products)
        {
            await RefreshLookupsAsync();
            var types = AvailablePricingTypes.Count > 0
                ? AvailablePricingTypes.ToList()
                : ["سعر مفرد", "جملة", "وكيل"];
            return MigrationProductExcelHelper.BuildTemplate(
                types,
                AvailableWarehouses.ToList(),
                EnableMultiCurrency);
        }

        return kind switch
        {
            MigrationStepKind.CashAndBank => MigrationExcelHelper.BuildTemplate(
                ["النوع", "الاسم", "الرصيد", "رقم_الحساب", "العملة"],
                ["قاصة", "قاصة دينار", "0", "", "IQD"],
                ["مصرف", "حساب جاري", "0", "123456", "IQD"],
                "النوع: قاصة أو مصرف. العملة: IQD أو USD. رقم_الحساب للمصارف فقط."),
            MigrationStepKind.Investors => MigrationExcelHelper.BuildTemplate(
                ["الاسم", "الهاتف", "نسبة_الربح", "الرصيد_الافتتاحي"],
                ["مستثمر تجريبي", "07xxxxxxxxx", "50", "0"],
                guide: "نسبة_الربح رقمية (مثال 50). الرصيد_الافتتاحي بالدينار فقط — اختياري."),
            MigrationStepKind.Warehouses => MigrationExcelHelper.BuildTemplate(
                ["الاسم", "الفرع", "الموقع"],
                ["المخزن الرئيسي", "MAIN", "بغداد"],
                guide: "الاسم مطلوب. الفرع: رمز أو اسم الفرع. الموقع اختياري."),
            MigrationStepKind.Categories => MigrationExcelHelper.BuildTemplate(
                ["اسم_الصنف"],
                ["عام"],
                ["إلكترونيات"],
                "عمود اسم_الصنف (أو الاسم) — كل صف صنف واحد."),
            MigrationStepKind.PricingTypes => MigrationExcelHelper.BuildTemplate(
                ["الاسم"],
                ["سعر مفرد"],
                ["جملة"],
                "أضف أنواع التسعير المستخدمة في البيع (سعر مفرد، جملة، وكيل...)."),
            MigrationStepKind.Customers => _customerBalanceExcel.GenerateTemplate(),
            MigrationStepKind.Suppliers => _supplierBalanceExcel.GenerateTemplate(),
            MigrationStepKind.ExpenseTypes => MigrationExcelHelper.BuildTemplate(
                ["الاسم"],
                ["إيجار"],
                ["رواتب"],
                "كل صف = نوع مصروف واحد."),
            MigrationStepKind.Installments => _openingInstallmentExcel.GenerateTemplate(),
            _ => MigrationExcelHelper.BuildTemplate(["الاسم"], guide: "عمود الاسم مطلوب.")
        };
    }

    private async Task LoadExcelIntoRowsAsync(MigrationStepKind kind, string path)
    {
        switch (kind)
        {
            case MigrationStepKind.CashAndBank:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 5))
                {
                    var isBank = r[0].Contains("مصرف", StringComparison.OrdinalIgnoreCase) ||
                                 r[0].Contains("Bank", StringComparison.OrdinalIgnoreCase);
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Kind = isBank ? "مصرف" : "قاصة",
                        Name = r[1],
                        Amount = MigrationExcelHelper.ParseDecimal(r[2]),
                        AccountNumber = r[3],
                        CostCurrency = ParseCashCurrency(r.Length > 4 ? r[4] : null),
                        Notes = r[0]
                    });
                }
                break;
            case MigrationStepKind.Investors:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 4))
                {
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Name = r[0],
                        Phone = r[1],
                        ProfitPercentage = MigrationExcelHelper.ParseDecimal(r[2]),
                        Amount = MigrationExcelHelper.ParseDecimal(r[3]),
                        AmountUsd = 0m,
                        CostCurrency = AccountingCurrency.IQD,
                        FxRate = 1m
                    });
                }
                break;
            case MigrationStepKind.Warehouses:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 3))
                {
                    var branchKey = r.Length > 1 ? r[1].Trim() : string.Empty;
                    var branch = ResolveBranch(branchKey);
                    CurrentRows.Add(new MigrationNamedBalanceRow
                    {
                        Name = r[0],
                        BranchId = branch?.Id,
                        BranchName = branch?.Name ?? (string.IsNullOrWhiteSpace(branchKey) ? null : branchKey),
                        Notes = r.Length > 2 ? r[2] : null
                    });
                }
                break;
            case MigrationStepKind.Categories:
            case MigrationStepKind.ExpenseTypes:
            case MigrationStepKind.PricingTypes:
                foreach (var r in MigrationExcelHelper.ReadDataRows(path, 1))
                {
                    if (string.IsNullOrWhiteSpace(r[0])) continue;
                    // تخطّي صف المثال إن وُجد بنفس الاسم في الهيدر
                    CurrentRows.Add(new MigrationNamedBalanceRow { Name = r[0].Trim() });
                }
                break;
            case MigrationStepKind.Products:
                await RefreshLookupsAsync();
                var typeNames = AvailablePricingTypes.Count > 0
                    ? AvailablePricingTypes.ToList()
                    : ["سعر مفرد", "جملة", "وكيل"];
                // إزالة صفوف فارغة مُعدّة مسبقاً قبل الاستيراد
                for (var i = CurrentRows.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrWhiteSpace(CurrentRows[i].Name))
                        CurrentRows.RemoveAt(i);
                }
                foreach (var imported in MigrationProductExcelHelper.Parse(
                             path, typeNames, AvailableWarehouses.ToList(), EnableMultiCurrency, InitialUsdToIqd))
                {
                    SyncProductRowStructure(imported);
                    CurrentRows.Add(imported);
                }
                ProductsGridVersion++;
                break;
            case MigrationStepKind.Customers:
                foreach (var r in _customerBalanceExcel.ParseImportFile(path))
                    CurrentRows.Add(FromPartyImport(r, allowZeroOpeningBalance: true));
                break;
            case MigrationStepKind.Suppliers:
                foreach (var r in _supplierBalanceExcel.ParseImportFile(path))
                    CurrentRows.Add(FromPartyImport(r, allowZeroOpeningBalance: true));
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

    private bool TryApplyRowCurrency(MigrationNamedBalanceRow row)
    {
        row.CostCurrency = NewRowCostCurrencyOption?.Currency ?? AccountingCurrency.IQD;
        if (!EnableMultiCurrency)
            row.CostCurrency = AccountingCurrency.IQD;

        row.FxRate = row.CostCurrency == AccountingCurrency.USD
            ? (NewRowFxRate > 0 ? NewRowFxRate : 0m)
            : 1m;
        if (row.CostCurrency == AccountingCurrency.USD && row.Amount > 0 && row.FxRate <= 0)
        {
            Warn("أدخل سعر الصرف (دولار→دينار) عند اختيار الدولار");
            return false;
        }
        return true;
    }

    private decimal ResolveProductUnitCostInIqd(MigrationNamedBalanceRow row)
    {
        var iqd = row.UnitCost;
        if (EnableMultiCurrency && row.UnitCostUsd > 0)
        {
            if (InitialUsdToIqd <= 0)
                throw new InvalidOperationException("سعر الصرف الافتتاحي مطلوب لتكلفة بالدولار");
            iqd += AccountingCurrencyHelper.ToBaseIqd(row.UnitCostUsd, AccountingCurrency.USD, InitialUsdToIqd);
        }
        else if (row.CostCurrency == AccountingCurrency.USD && row.UnitCost > 0)
        {
            if (row.FxRate <= 0)
                throw new InvalidOperationException("سعر الصرف مطلوب لتكلفة بالدولار");
            return AccountingCurrencyHelper.ToBaseIqd(row.UnitCost, AccountingCurrency.USD, row.FxRate);
        }

        return iqd <= 0 ? 0 : AccountingCurrencyHelper.RoundIqd(iqd);
    }

    private decimal ResolveSalePriceInIqd(MigrationProductPriceCell cell)
    {
        // أسعار الدولار تُحفظ في SalePriceUsd بشكل مستقل — لا تحويل إلى دينار.
        return cell.SalePrice <= 0 ? 0 : AccountingCurrencyHelper.RoundIqd(cell.SalePrice);
    }

    private static AccountingCurrency ParseCashCurrency(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return AccountingCurrency.IQD;
        text = text.Trim();
        if (text.Contains("USD", StringComparison.OrdinalIgnoreCase)
            || text.Contains("دولار", StringComparison.OrdinalIgnoreCase)
            || text == "$")
            return AccountingCurrency.USD;
        return AccountingCurrency.IQD;
    }

    private static MigrationNamedBalanceRow FromPartyImport(
        OpeningPartyBalanceImportRow r, bool allowZeroOpeningBalance = false)
    {
        var hasAnyBalance = r.Amount > 0 || r.AmountUsd > 0;
        var amountErrorOnly = allowZeroOpeningBalance
            && !r.IsValid
            && !hasAnyBalance
            && r.Errors.Count > 0
            && r.Errors.All(e => e.Contains("المبلغ", StringComparison.Ordinal));

        decimal amountIqd;
        decimal amountUsd;
        if (r.AmountUsd > 0 || (r.Amount > 0 && r.Currency == AccountingCurrency.IQD))
        {
            amountIqd = amountErrorOnly ? 0 : r.Amount;
            amountUsd = amountErrorOnly ? 0 : r.AmountUsd;
        }
        else if (r.Currency == AccountingCurrency.USD && r.Amount > 0)
        {
            // قالب قديم: المبلغ بالدولار
            amountIqd = 0;
            amountUsd = amountErrorOnly ? 0 : r.Amount;
        }
        else
        {
            amountIqd = amountErrorOnly ? 0 : r.Amount;
            amountUsd = amountErrorOnly ? 0 : r.AmountUsd;
        }

        return new MigrationNamedBalanceRow
        {
            Name = r.PartyName,
            Phone = r.Phone,
            FileNumber = r.FileNumber,
            Amount = amountIqd,
            AmountUsd = amountUsd,
            Date = r.Date,
            Notes = r.Notes,
            CostCurrency = AccountingCurrency.IQD,
            FxRate = 1m,
            IsValid = r.IsValid || amountErrorOnly,
            ErrorText = (r.IsValid || amountErrorOnly) ? null : r.ErrorsText
        };
    }

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
            _stepDirty = false;
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
        if (!CanSkip || CurrentKind is null || IsBusy) return;

        try
        {
            IsBusy = true;
            NotifyStepProps();

            if (CurrentRows.Count > 0)
            {
                if (!BeautifulMessageDialog.ShowConfirm(
                        "توجد صفوف غير محفوظة في هذه الخطوة. هل تريد تخطيها بدون حفظ؟",
                        "تخطي الخطوة"))
                    return;
                CurrentRows.Clear();
            }

            MarkSaved(CurrentKind.Value);
            _stepDirty = false;
            StatusMessage = "تم تخطي الخطوة";
            await AdvanceAsync();
        }
        finally
        {
            IsBusy = false;
            NotifyStepProps();
        }
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (IsCompleted || CurrentKind is null || IsBusy) return;

        try
        {
            IsBusy = true;
            NotifyStepProps();

            var needsPersist = !_savedSteps.Contains(CurrentKind.Value) || _stepDirty;
            // العملاء/الموردون: دائماً أعد الحفظ إن وُجدت أسماء — لدعم الإضافة بعد زيارة سابقة
            if (CurrentKind is MigrationStepKind.Customers or MigrationStepKind.Suppliers
                && CurrentRows.Any(r => !string.IsNullOrWhiteSpace(r.Name)))
                needsPersist = true;

            if (needsPersist)
            {
                var hasData = CurrentKind is MigrationStepKind.Branches
                    ? BranchRows.Any(b =>
                        (!b.HasExistingCapital && (b.CapitalAmount > 0 || b.CapitalAmountUsd > 0))
                        || (!b.HasExistingProfit && (b.ProfitOpeningBalance != 0 || b.ProfitOpeningBalanceUsd != 0)))
                      || EnableMultiCurrency
                    : IsCapitalStep
                        ? CapitalAmount > 0 || CapitalAmountUsd > 0
                          || ProfitOpeningBalance != 0 || ProfitOpeningBalanceUsd != 0
                        : CurrentRows.Any(r => !string.IsNullOrWhiteSpace(r.Name) || !string.IsNullOrWhiteSpace(r.ProductName));

                if (!hasData && CurrentStepInfo?.IsOptional == true)
                {
                    MarkSaved(CurrentKind.Value);
                }
                else if (!hasData && CurrentKind == MigrationStepKind.Branches
                         && BranchRows.Count > 0 && BranchRows.All(b => b.HasExistingCapital))
                {
                    var okCurrency = await PersistCurrencySettingsAsync();
                    if (!okCurrency) return;
                    MarkSaved(CurrentKind.Value);
                }
                else if (!hasData && CurrentKind == MigrationStepKind.Warehouses
                         && await AnyWarehouseExistsAsync())
                {
                    MarkSaved(CurrentKind.Value);
                }
                else
                {
                    var ok = await PersistStepAsync(CurrentKind.Value);
                    if (!ok) return;
                    MarkSaved(CurrentKind.Value);
                }

                _stepDirty = false;
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
        MigrationStepKind.Branches => await SaveBranchesAndCapitalAsync(),
        MigrationStepKind.Capital => await SaveBranchesAndCapitalAsync(),
        MigrationStepKind.CashAndBank => await SaveCashAndBankAsync(),
        MigrationStepKind.Investors => await SaveInvestorsAsync(),
        MigrationStepKind.Warehouses => await SaveWarehousesAsync(),
        MigrationStepKind.Categories => await SaveCategoriesAsync(),
        MigrationStepKind.Products => await SaveProductsAsync(),
        MigrationStepKind.PricingTypes => await SavePricingTypesAsync(),
        MigrationStepKind.Customers => await SaveCustomersAsync(),
        MigrationStepKind.Suppliers => await SaveSuppliersAsync(),
        MigrationStepKind.ExpenseTypes => await SaveExpenseTypesAsync(),
        MigrationStepKind.Installments => await SaveInstallmentsAsync(),
        _ => false
    };

    [RelayCommand]
    private async Task AddBranchAsync()
    {
        var name = NewBranchName?.Trim();
        var code = NewBranchCode?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            Warn("أدخل اسم ورمز الفرع");
            return;
        }

        try
        {
            IsBusy = true;
            var branch = await _branchService.CreateAsync(name, code);
            // حدّث جلسة الفروع المسموحة فوراً (بدون إعادة تسجيل دخول)
            var allowed = _branchContext.AllowedBranchIds
                .Append(branch.Id)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            _branchContext.SetAllowedBranches(
                allowed,
                canViewAll: allowed.Count > 1 && (_branchContext.CanViewAllBranches || _branchContext.CanManageAllBranches),
                canManageAll: allowed.Count > 1 && _branchContext.CanManageAllBranches);

            var capitalIqd = NewBranchCapitalIqd;
            var capitalUsd = EnableMultiCurrency ? NewBranchCapitalUsd : 0m;
            var row = new MigrationBranchCapitalRow
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                BranchCode = branch.Code,
                IsMain = branch.IsMain,
                IsExisting = true,
                HasExistingCapital = false,
                CapitalAmount = capitalIqd,
                CapitalAmountUsd = capitalUsd
            };
            row.PropertyChanged += OnBranchRowPropertyChanged;
            BranchRows.Add(row);
            NewBranchName = string.Empty;
            NewBranchCode = string.Empty;
            NewBranchCapitalIqd = 0m;
            NewBranchCapitalUsd = 0m;
            AvailableBranches.Add(branch);
            SelectedBranchForWarehouse ??= branch;
            _needsCapital = true;
            RefreshCapitalTotalDisplay();
            OnPropertyChanged(nameof(AllBranchesHaveCapital));
            OnPropertyChanged(nameof(CanEnterBranchCapital));
            OnPropertyChanged(nameof(HasBranchRows));
            MarkStepDirty();
            OnPropertyChanged(nameof(CanSkip));
            StatusMessage = capitalIqd > 0 || capitalUsd > 0
                ? $"تم إضافة الفرع «{branch.Name}» برأس ماله — احفظ الخطوة"
                : $"تم إضافة الفرع «{branch.Name}» — أدخل رأس ماله ثم احفظ";
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> PersistCurrencySettingsAsync()
    {
        var needsRateNow = EnableMultiCurrency && BranchRows.Any(r =>
            (!r.HasExistingCapital && r.CapitalAmountUsd > 0)
            || (!r.HasExistingProfit && r.ProfitOpeningBalanceUsd != 0));

        if (needsRateNow && InitialUsdToIqd <= 0)
        {
            Warn("عند إدخال مبالغ بالدولار يجب إدخال سعر الصرف الافتتاحي (كم دينار يساوي دولار واحد)");
            return false;
        }

        var settings = await _businessSettingsService.GetOrCreateAsync();
        await _businessSettingsService.SaveAsync(
            settings.ProductPricingEnabled,
            settings.UpdateProductPriceOnPurchase,
            settings.PeriodLockEnabled,
            settings.LockedThroughDate,
            EnableMultiCurrency);

        _preferences.Update(p => p.FeatureFlags.MultiCurrency = EnableMultiCurrency);
        _featureFlags.NotifyFlagsChanged();
        ShowMultiCurrency = EnableMultiCurrency;

        if (EnableMultiCurrency && InitialUsdToIqd > 0)
        {
            await _exchangeRateService.SaveAsync(new ExchangeRate
            {
                RateDate = CapitalDate.Date,
                UsdToIqd = AccountingCurrencyHelper.RoundIqd(InitialUsdToIqd),
                Notes = "سعر الصرف الافتتاحي — معالج النقل"
            });
        }

        return true;
    }

    private async Task<bool> SaveBranchesAndCapitalAsync()
    {
        if (BranchRows.Count == 0)
        {
            Warn("لا توجد فروع — تأكد من ترحيل قاعدة البيانات");
            return false;
        }

        if (EnableMultiCurrency && InitialUsdToIqd <= 0
            && BranchRows.Any(r =>
                (!r.HasExistingCapital && r.CapitalAmountUsd > 0)
                || (!r.HasExistingProfit && r.ProfitOpeningBalanceUsd != 0)))
        {
            Warn("أدخل سعر الصرف لتحويل المبالغ بالدولار إلى الدينار");
            return false;
        }

        var pendingCapital = BranchRows.Where(r => !r.HasExistingCapital).ToList();
        if (_needsCapital && pendingCapital.Count > 0)
        {
            var missing = pendingCapital.Where(r =>
                ComputeRowCapitalInBaseIqd(r) <= 0
                && r.CapitalAmountUsd <= 0).ToList();
            if (missing.Count > 0)
            {
                Warn($"أدخل رأس المال لكل فرع بدون رأس مال موجود: {string.Join("، ", missing.Select(m => m.BranchName))}");
                return false;
            }
        }

        if (!await PersistCurrencySettingsAsync())
            return false;

        var toCreate = BranchRows.Where(r =>
            (!r.HasExistingCapital && (ComputeRowCapitalInBaseIqd(r) > 0 || r.CapitalAmountUsd > 0))
            || (!r.HasExistingProfit && ComputeRowProfitInBaseIqd(r) != 0)).ToList();

        if (toCreate.Count == 0)
        {
            _needsCapital = BranchRows.Any(r => !r.HasExistingCapital);
            OnPropertyChanged(nameof(AllBranchesHaveCapital));
            OnPropertyChanged(nameof(CanEnterBranchCapital));
            LastImportedCount = 0;
            return true;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var saved = 0;
            foreach (var row in toCreate)
            {
                if (!row.HasExistingCapital)
                {
                    var capitalInBase = ComputeRowCapitalInBaseIqd(row);
                    if (capitalInBase > 0 || row.CapitalAmountUsd > 0)
                    {
                        if (capitalInBase <= 0 && row.CapitalAmountUsd > 0 && InitialUsdToIqd <= 0)
                        {
                            Warn($"الفرع «{row.BranchName}»: أدخل سعر الصرف لتحويل رأس المال بالدولار");
                            await tx.RollbackAsync();
                            return false;
                        }

                        db.CapitalEntries.Add(new CapitalEntry
                        {
                            BranchId = row.BranchId,
                            Amount = capitalInBase,
                            Date = CapitalDate,
                            Type = CapitalEntryType.Initial,
                            Notes = BuildBranchCapitalNotes(row)
                        });
                        row.HasExistingCapital = true;
                        saved++;
                    }
                }

                if (!row.HasExistingProfit)
                {
                    var profitInBase = ComputeRowProfitInBaseIqd(row);
                    if (profitInBase != 0)
                    {
                        db.CapitalEntries.Add(new CapitalEntry
                        {
                            BranchId = row.BranchId,
                            Amount = profitInBase,
                            Date = CapitalDate,
                            Type = CapitalEntryType.ProfitOpeningBalance,
                            Notes = BuildBranchProfitNotes(row)
                        });
                        row.HasExistingProfit = true;
                        saved++;
                    }
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            _needsCapital = BranchRows.Any(r => !r.HasExistingCapital);
            OnPropertyChanged(nameof(AllBranchesHaveCapital));
            OnPropertyChanged(nameof(CanEnterBranchCapital));
            LastImportedCount = saved;
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private string BuildBranchCapitalNotes(MigrationBranchCapitalRow row)
    {
        if (!string.IsNullOrWhiteSpace(CapitalNotes))
            return $"{CapitalNotes.Trim()} — {row.BranchName}";

        if (!EnableMultiCurrency || row.CapitalAmountUsd <= 0)
            return $"رأس المال الأولي — معالج النقل ({row.BranchName})";

        return $"رأس المال الأولي — دينار: {AccountingCurrencyHelper.Format(row.CapitalAmount, AccountingCurrency.IQD)} + دولار: {AccountingCurrencyHelper.Format(row.CapitalAmountUsd, AccountingCurrency.USD)} @ {InitialUsdToIqd:N0} — {row.BranchName}";
    }

    private string BuildBranchProfitNotes(MigrationBranchCapitalRow row)
    {
        if (!EnableMultiCurrency || row.ProfitOpeningBalanceUsd == 0)
            return $"الأرباح الافتتاحية — معالج النقل ({row.BranchName})";

        return $"الأرباح الافتتاحية — دينار: {AccountingCurrencyHelper.Format(row.ProfitOpeningBalance, AccountingCurrency.IQD)} + دولار: {AccountingCurrencyHelper.Format(row.ProfitOpeningBalanceUsd, AccountingCurrency.USD)} @ {InitialUsdToIqd:N0} — {row.BranchName}";
    }

    private Branch? ResolveBranch(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return SelectedBranchForWarehouse
                   ?? AvailableBranches.FirstOrDefault(b => b.IsMain)
                   ?? AvailableBranches.FirstOrDefault();

        key = key.Trim();
        return AvailableBranches.FirstOrDefault(b =>
                   string.Equals(b.Code, key, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(b.Name, key, StringComparison.OrdinalIgnoreCase))
               ?? BranchRows.Select(r => AvailableBranches.FirstOrDefault(b => b.Id == r.BranchId))
                   .FirstOrDefault(b => b is not null
                       && (string.Equals(b!.Code, key, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(b.Name, key, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<bool> AnyWarehouseExistsAsync()
    {
        var allowedBranchIds = GetAllowedBranchIds();
        if (allowedBranchIds.Count == 0) return false;

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;
        return await db.Warehouses.IgnoreQueryFilters()
            .AnyAsync(w => !w.IsDeleted && allowedBranchIds.Contains(w.BranchId));
    }

    private async Task<bool> SaveCapitalAsync()
    {
        // مسار قديم: إن وُجدت فروع، احفظ عبر SaveBranchesAndCapitalAsync
        if (BranchRows.Count > 0)
            return await SaveBranchesAndCapitalAsync();

        if (await _unitOfWork.CapitalEntries.AnyAsync())
        {
            _needsCapital = false;
            return true;
        }

        if (CapitalAmount <= 0 && CapitalAmountUsd <= 0 && ProfitOpeningBalance == 0 && ProfitOpeningBalanceUsd == 0)
        {
            Warn("أدخل رأس المال أو الأرباح الافتتاحية");
            return false;
        }

        if (!await PersistCurrencySettingsAsync())
            return false;

        var capitalInBase = ComputeLegacyCapitalInBaseIqd();
        var profitInBase = ComputeLegacyProfitInBaseIqd();
        var branchId = _branchContext.CurrentBranchId
                       ?? (await _branchService.GetMainBranchAsync()).Id;

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;
        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            if (capitalInBase > 0 || CapitalAmountUsd > 0)
            {
                db.CapitalEntries.Add(new CapitalEntry
                {
                    BranchId = branchId,
                    Amount = capitalInBase,
                    Date = CapitalDate,
                    Type = CapitalEntryType.Initial,
                    Notes = string.IsNullOrWhiteSpace(CapitalNotes) ? "رأس المال — معالج النقل" : CapitalNotes
                });
            }

            if (profitInBase != 0)
            {
                db.CapitalEntries.Add(new CapitalEntry
                {
                    BranchId = branchId,
                    Amount = profitInBase,
                    Date = CapitalDate,
                    Type = CapitalEntryType.ProfitOpeningBalance,
                    Notes = "الأرباح الافتتاحية — معالج النقل"
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            _needsCapital = false;
            LastImportedCount = 1;
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
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

        // معاملة واحدة + SaveChangesAsync — تجنب Update() المتزامن الذي يجمد واجهة WPF
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var existingCash = (await _unitOfWork.CashBoxes.GetAllAsync()).ToList();
            var existingBank = (await _unitOfWork.BankAccounts.GetAllAsync()).ToList();
            var saved = 0;
            var updated = 0;

            foreach (var row in rows)
            {
                var name = row.Name.Trim();
                var isBank = string.Equals(row.Kind, "Bank", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(row.Kind, "مصرف", StringComparison.OrdinalIgnoreCase)
                             || (row.Notes?.Contains("مصرف", StringComparison.OrdinalIgnoreCase) == true);

                if (isBank)
                {
                    var bank = row.SourceId is int bid
                        ? existingBank.FirstOrDefault(b => b.Id == bid)
                        : existingBank.FirstOrDefault(b =>
                            string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));

                    if (bank is not null)
                    {
                        bank.Name = name;
                        bank.AccountNumber = string.IsNullOrWhiteSpace(row.AccountNumber)
                            ? bank.AccountNumber
                            : row.AccountNumber.Trim();
                        bank.Balance = row.Amount;
                        bank.Currency = row.CostCurrency;
                        _unitOfWork.BankAccounts.Update(bank);
                        updated++;
                    }
                    else
                    {
                        bank = new BankAccount
                        {
                            Name = name,
                            AccountNumber = row.AccountNumber,
                            Balance = row.Amount,
                            Currency = row.CostCurrency
                        };
                        await _unitOfWork.BankAccounts.AddAsync(bank);
                        existingBank.Add(bank);
                        saved++;
                    }
                }
                else
                {
                    var cash = row.SourceId is int cid
                        ? existingCash.FirstOrDefault(c => c.Id == cid)
                        : existingCash.FirstOrDefault(c =>
                            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

                    if (cash is not null)
                    {
                        cash.Name = name;
                        cash.Balance = row.Amount;
                        cash.Currency = row.CostCurrency;
                        _unitOfWork.CashBoxes.Update(cash);
                        updated++;
                    }
                    else
                    {
                        cash = new CashBox
                        {
                            Name = name,
                            Balance = row.Amount,
                            Currency = row.CostCurrency
                        };
                        await _unitOfWork.CashBoxes.AddAsync(cash);
                        existingCash.Add(cash);
                        saved++;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            LastImportedCount = saved + updated;
            LastSkippedCount = 0;
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<bool> SaveInvestorsAsync()
    {
        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        if (rows.Count == 0) { Warn("أضف مستثمراً أو اضغط تخطي"); return false; }

        var existing = (await _investorService.GetAllInvestorsAsync()).ToList();
        var openingItems = new List<InvestorOpeningBalanceItem>();

        foreach (var row in rows)
        {
            var match = row.SourceId is int sid
                ? existing.FirstOrDefault(i => i.Id == sid)
                : existing.FirstOrDefault(i =>
                    string.Equals(i.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            var isNew = match is null;
            if (isNew)
            {
                match = await _investorService.AddInvestorAsync(row.Name.Trim(), row.Phone, row.ProfitPercentage);
                existing.Add(match);
                row.SourceId = match.Id;
                row.IsExisting = true;
            }

            // أرصدة المستثمرين بالدينار فقط — لا دولار
            var openingIqd = AccountingCurrencyHelper.RoundIqd(row.Amount);

            var openingToSave = match!.OpeningBalance;
            if (isNew || Math.Abs(openingIqd - match.OpeningBalance) > 0.001m)
                openingToSave = openingIqd;

            openingItems.Add(new InvestorOpeningBalanceItem
            {
                InvestorId = match.Id,
                Name = match.Name,
                Phone = string.IsNullOrWhiteSpace(row.Phone) ? match.Phone : row.Phone,
                ProfitPercentage = row.ProfitPercentage > 0 ? row.ProfitPercentage : match.ProfitPercentage,
                OpeningBalance = openingToSave
            });
        }

        await _investorService.SaveOpeningBalancesAsync(openingItems);
        LastImportedCount = rows.Count;
        return true;
    }

    private async Task<bool> SaveWarehousesAsync()
    {
        var allowedBranchIds = GetAllowedBranchIds();
        var defaultBranchId = SelectedBranchForWarehouse?.Id
                              ?? (_branchContext.CurrentBranchId is int cur && allowedBranchIds.Contains(cur)
                                  ? cur
                                  : (int?)null)
                              ?? (allowedBranchIds.Count > 0 ? allowedBranchIds.First() : null);

        var rows = CurrentRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .ToList();

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;

        var existing = await db.Warehouses.IgnoreQueryFilters()
            .Where(w => !w.IsDeleted)
            .ToListAsync();
        var existingAllowed = existing
            .Where(w => allowedBranchIds.Contains(w.BranchId))
            .ToList();

        // تجاهل صفوف مخازن فروع خارج الصلاحية (قد تظهر من بيانات قديمة) — لا تمنع الحفظ
        var writableRows = new List<MigrationNamedBalanceRow>();
        foreach (var row in rows)
        {
            var branchId = row.BranchId
                           ?? ResolveBranch(row.BranchName)?.Id
                           ?? (row.IsExisting ? null : defaultBranchId);
            if (branchId is int bid && !allowedBranchIds.Contains(bid))
            {
                if (row.IsExisting)
                    continue; // مخزن فرع آخر: اتركه كما هو ولا تحفظه
                Warn($"المخزن «{row.Name.Trim()}»: لا تملك صلاحية على الفرع المحدد");
                return false;
            }

            writableRows.Add(row);
        }

        if (writableRows.Count == 0 && existingAllowed.Count > 0) return true;
        if (writableRows.Count == 0)
        {
            Warn("أدخل مخزناً واحداً على الأقل لفرع من فروعك — مطلوب قبل المنتجات");
            return false;
        }
        var saved = 0;
        var updated = 0;
        foreach (var row in writableRows)
        {
            var name = row.Name.Trim();
            var branchId = row.BranchId
                           ?? ResolveBranch(row.BranchName)?.Id
                           ?? defaultBranchId;
            if (branchId is null or <= 0 || !allowedBranchIds.Contains(branchId.Value))
            {
                Warn($"المخزن «{name}»: اختر الفرع من عمود الفرع قبل الحفظ");
                return false;
            }

            var branchName = AvailableBranches.FirstOrDefault(b => b.Id == branchId)?.Name
                             ?? row.BranchName;
            var locationRaw = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim();
            // لا تحفظ النص الحارس «موجود مسبقاً» كموقع للمخزن
            var location = string.Equals(locationRaw, "موجود مسبقاً", StringComparison.Ordinal)
                ? null
                : locationRaw;

            var match = row.SourceId is int sid
                ? existing.FirstOrDefault(w => w.Id == sid && allowedBranchIds.Contains(w.BranchId))
                : existing.FirstOrDefault(w => w.BranchId == branchId
                    && string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                var changed = false;
                if (!string.Equals(match.Name, name, StringComparison.Ordinal))
                {
                    match.Name = name;
                    changed = true;
                }
                if (!string.Equals(match.Location ?? string.Empty, location ?? string.Empty, StringComparison.Ordinal))
                {
                    match.Location = location;
                    changed = true;
                }
                if (match.BranchId != branchId.Value)
                {
                    match.BranchId = branchId.Value;
                    changed = true;
                }

                if (changed)
                {
                    match.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }

                row.SourceId = match.Id;
                row.BranchId = branchId;
                row.BranchName = branchName;
                continue;
            }

            var wh = new Warehouse
            {
                Name = name,
                Location = location,
                BranchId = branchId.Value
            };
            db.Warehouses.Add(wh);
            existing.Add(wh);
            row.BranchId = branchId;
            row.BranchName = branchName;
            saved++;
        }
        await db.SaveChangesAsync();
        LastImportedCount = saved + updated;
        await RefreshLookupsAsync();
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
        var rows = CurrentRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToList();

        if (rows.Count == 0) { Warn("أضف منتجات أو اضغط تخطي"); return false; }
        if (rows.Any(r => !r.IsValid))
        {
            Warn("صحّح الصفوف غير الصالحة أولاً");
            return false;
        }

        var allowedBranchIds = GetAllowedBranchIds();
        var warehouses = (await _unitOfWork.Warehouses.GetAllAsync()).ToList();
        // اقرأ كل المخازن ثم صفِّ حسب الفروع المسموحة للمستخدم فقط
        await using (var dbWh = await _dbFactory.CreateDbContextAsync())
        {
            dbWh.BypassBranchFilter = true;
            warehouses = await dbWh.Warehouses.IgnoreQueryFilters()
                .Where(w => !w.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        warehouses = warehouses
            .Where(w => allowedBranchIds.Contains(w.BranchId))
            .ToList();

        if (warehouses.Count == 0)
        {
            Warn("لا يوجد مخزن ضمن فروعك المسموحة — أضف مخزناً لفرع تملكه أولاً");
            return false;
        }

        var defaultWarehouse = ResolveWarehouse(warehouses, SelectedWarehouseName)
            ?? warehouses[0];
        if (!allowedBranchIds.Contains(defaultWarehouse.BranchId))
        {
            Warn("المخزن الافتراضي خارج صلاحياتك");
            return false;
        }

        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
        var prices = (await _unitOfWork.ProductPrices.GetAllAsync()).ToList();
        // مخزون متعدد الفروع: يُحفظ بعد المنتجات عبر سياق Bypass (مع التحقق من الصلاحية أدناه)
        var pendingStocks = new List<(int ProductId, int WarehouseId, int BranchId, decimal Qty, decimal UnitCost)>();

        var pricingTypes = (await _pricingTypeService.GetActiveAsync()).ToList();
        if (pricingTypes.Count == 0)
        {
            await _pricingTypeService.EnsureDefaultExistsAsync();
            pricingTypes = (await _pricingTypeService.GetActiveAsync()).ToList();
        }
        var defaultPricing = pricingTypes.FirstOrDefault(t => t.IsDefault) ?? pricingTypes.FirstOrDefault();

        var saved = 0;
        var pricesUpdated = 0;
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
                        Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
                        CategoryId = category.Id
                    };
                    ApplyProductFeatureFields(product, row);
                    await _unitOfWork.Products.AddAsync(product);
                    await _unitOfWork.SaveChangesAsync();
                    products.Add(product);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(row.Barcode) && string.IsNullOrWhiteSpace(product.Barcode))
                        product.Barcode = row.Barcode.Trim();
                    if (!string.IsNullOrWhiteSpace(row.Description))
                        product.Description = row.Description.Trim();
                    ApplyProductFeatureFields(product, row);
                    _unitOfWork.Products.Update(product);
                }

                var warehouse = ResolveWarehouse(warehouses, row.WarehouseName)
                    ?? defaultWarehouse;

                decimal unitCostIqd;
                try
                {
                    unitCostIqd = ResolveProductUnitCostInIqd(row);
                }
                catch (Exception ex)
                {
                    Warn($"المنتج «{row.Name}»: {ex.Message}");
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                // كميات لكل مخزن مسموح فقط — لا إسقاط على مخزن خارج الصلاحية
                var rawQtyCells = row.WarehouseQtys
                    .Where(q => q.Quantity > 0 && !string.IsNullOrWhiteSpace(q.WarehouseName))
                    .ToList();
                var rejectedQty = rawQtyCells
                    .Select(q => (Wh: ResolveWarehouse(warehouses, q.WarehouseName), q.WarehouseName))
                    .Where(e => e.Wh is null || !allowedBranchIds.Contains(e.Wh.BranchId))
                    .ToList();
                if (rejectedQty.Count > 0)
                {
                    var names = string.Join("، ", rejectedQty.Select(r => r.WarehouseName).Distinct().Take(3));
                    Warn($"المنتج «{row.Name}»: لا يمكن إدخال كميات لمخازن خارج فروعك ({names})");
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                var qtyEntries = rawQtyCells
                    .Select(q => (Warehouse: ResolveWarehouse(warehouses, q.WarehouseName)!, q.Quantity))
                    .Where(e => allowedBranchIds.Contains(e.Warehouse.BranchId))
                    .ToList();

                if (qtyEntries.Count == 0 && row.Quantity > 0)
                {
                    if (!allowedBranchIds.Contains(warehouse.BranchId))
                    {
                        Warn($"المنتج «{row.Name}»: المخزن الافتراضي خارج صلاحياتك");
                        await _unitOfWork.RollbackTransactionAsync();
                        return false;
                    }
                    qtyEntries.Add((warehouse, row.Quantity));
                }

                foreach (var (wh, qty) in qtyEntries)
                    pendingStocks.Add((product.Id, wh.Id, wh.BranchId, qty, unitCostIqd));
                var priceCells = row.ProductPrices
                    .Where(p => p.SalePrice > 0 || p.SalePriceUsd > 0 || p.PurchasePrice > 0 || p.PurchasePriceUsd > 0)
                    .ToList();
                if (priceCells.Count == 0 && row.UnitPrice > 0 && defaultPricing is not null)
                {
                    priceCells.Add(new MigrationProductPriceCell
                    {
                        PricingTypeName = defaultPricing.Name,
                        SalePrice = row.UnitPrice,
                        PurchasePrice = unitCostIqd
                    });
                }

                foreach (var cell in priceCells)
                {
                    var type = pricingTypes.FirstOrDefault(t =>
                        string.Equals(t.Name, cell.PricingTypeName, StringComparison.OrdinalIgnoreCase));
                    if (type is null) continue;

                    var saleIqd = cell.SalePrice > 0 ? AccountingCurrencyHelper.RoundIqd(cell.SalePrice) : 0m;
                    var saleUsd = EnableMultiCurrency && cell.SalePriceUsd > 0
                        ? AccountingCurrencyHelper.RoundUsd(cell.SalePriceUsd)
                        : 0m;
                    var purchaseIqd = cell.PurchasePrice > 0
                        ? AccountingCurrencyHelper.RoundIqd(cell.PurchasePrice)
                        : 0m;
                    var purchaseUsd = EnableMultiCurrency && cell.PurchasePriceUsd > 0
                        ? AccountingCurrencyHelper.RoundUsd(cell.PurchasePriceUsd)
                        : 0m;

                    // إن لم يُدخل سعر شراء دينار نستخدم التكلفة كافتراضي عند الإنشاء فقط
                    if (purchaseIqd <= 0 && unitCostIqd > 0)
                        purchaseIqd = unitCostIqd;

                    if (saleIqd <= 0 && saleUsd <= 0 && purchaseIqd <= 0 && purchaseUsd <= 0)
                        continue;

                    var existingPrice = prices.FirstOrDefault(p =>
                        p.ProductId == product.Id && p.PricingTypeId == type.Id);
                    if (existingPrice is null)
                    {
                        existingPrice = new ProductPrice
                        {
                            ProductId = product.Id,
                            PricingTypeId = type.Id,
                            SalePrice = saleIqd,
                            SalePriceUsd = saleUsd,
                            PurchasePrice = purchaseIqd,
                            PurchasePriceUsd = purchaseUsd
                        };
                        await _unitOfWork.ProductPrices.AddAsync(existingPrice);
                        prices.Add(existingPrice);
                        pricesUpdated++;
                    }
                    else
                    {
                        var changed = false;
                        // تحديث صريح لأي قيمة أدخلها المستخدم (> 0)
                        if (cell.SalePrice > 0 && existingPrice.SalePrice != saleIqd)
                        {
                            existingPrice.SalePrice = saleIqd;
                            changed = true;
                        }
                        if (EnableMultiCurrency && cell.SalePriceUsd > 0 && existingPrice.SalePriceUsd != saleUsd)
                        {
                            existingPrice.SalePriceUsd = saleUsd;
                            changed = true;
                        }
                        if (cell.PurchasePrice > 0)
                        {
                            var roundedPurchase = AccountingCurrencyHelper.RoundIqd(cell.PurchasePrice);
                            if (existingPrice.PurchasePrice != roundedPurchase)
                            {
                                existingPrice.PurchasePrice = roundedPurchase;
                                changed = true;
                            }
                        }
                        else if (unitCostIqd > 0 && existingPrice.PurchasePrice <= 0)
                        {
                            existingPrice.PurchasePrice = unitCostIqd;
                            changed = true;
                        }
                        if (EnableMultiCurrency && cell.PurchasePriceUsd > 0 && existingPrice.PurchasePriceUsd != purchaseUsd)
                        {
                            existingPrice.PurchasePriceUsd = purchaseUsd;
                            changed = true;
                        }

                        if (changed)
                        {
                            _unitOfWork.ProductPrices.Update(existingPrice);
                            pricesUpdated++;
                        }
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

            if (pendingStocks.Count > 0)
                await PersistAllowedWarehouseStocksAsync(pendingStocks, allowedBranchIds);

            StatusMessage = EnableMultiCurrency
                ? $"تم حفظ/تحديث {saved} منتجاً — حُدّثت {pricesUpdated} تسعيرة (دينار/دولار منفصلة)."
                : $"تم حفظ/تحديث {saved} منتجاً — حُدّثت {pricesUpdated} تسعيرة بالدينار.";
            await RefreshLookupsAsync();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// حفظ كميات المخازن للفروع المسموحة فقط، مع ختم BranchId من المخزن.
    /// </summary>
    private async Task PersistAllowedWarehouseStocksAsync(
        IReadOnlyList<(int ProductId, int WarehouseId, int BranchId, decimal Qty, decimal UnitCost)> pending,
        HashSet<int> allowedBranchIds)
    {
        var safe = pending
            .Where(p => p.ProductId > 0
                        && p.WarehouseId > 0
                        && p.BranchId > 0
                        && allowedBranchIds.Contains(p.BranchId)
                        && p.Qty > 0)
            .ToList();
        if (safe.Count == 0) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BypassBranchFilter = true;

        var productIds = safe.Select(s => s.ProductId).Distinct().ToList();
        var warehouseIds = safe.Select(s => s.WarehouseId).Distinct().ToList();
        var existing = await db.WarehouseStocks.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                        && productIds.Contains(s.ProductId)
                        && warehouseIds.Contains(s.WarehouseId))
            .ToListAsync();

        foreach (var entry in safe)
        {
            var stock = existing.FirstOrDefault(s =>
                s.ProductId == entry.ProductId && s.WarehouseId == entry.WarehouseId);
            if (stock is null)
            {
                stock = new WarehouseStock
                {
                    BranchId = entry.BranchId,
                    WarehouseId = entry.WarehouseId,
                    ProductId = entry.ProductId,
                    Quantity = entry.Qty,
                    OpeningQuantity = entry.Qty,
                    UnitCost = entry.UnitCost,
                    CreatedAt = DateTime.UtcNow
                };
                db.WarehouseStocks.Add(stock);
                existing.Add(stock);
            }
            else
            {
                stock.BranchId = entry.BranchId;
                stock.OpeningQuantity = entry.Qty;
                stock.Quantity = Math.Max(stock.Quantity, entry.Qty);
                if (entry.UnitCost > 0) stock.UnitCost = entry.UnitCost;
                stock.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    private HashSet<int> GetAllowedBranchIds()
    {
        var ids = _branchContext.AllowedBranchIds
            .Where(id => id > 0)
            .ToHashSet();
        if (ids.Count == 0 && _branchContext.CurrentBranchId is > 0)
            ids.Add(_branchContext.CurrentBranchId.Value);
        return ids;
    }

    private bool IsBranchAllowedForMigration(int branchId)
        => branchId > 0 && GetAllowedBranchIds().Contains(branchId);

    private void ApplyProductFeatureFields(Product product, MigrationNamedBalanceRow row)
    {
        if (ShowProductPharmacyFields)
        {
            if (!string.IsNullOrWhiteSpace(row.ScientificName))
                product.ScientificName = row.ScientificName.Trim();
            if (!string.IsNullOrWhiteSpace(row.UsageInstructions))
                product.UsageInstructions = row.UsageInstructions.Trim();
        }

        if (ShowProductCarFields)
        {
            if (!string.IsNullOrWhiteSpace(row.VehicleType))
                product.VehicleType = row.VehicleType.Trim();
            if (!string.IsNullOrWhiteSpace(row.ChassisNumber))
                product.ChassisNumber = row.ChassisNumber.Trim();
            if (!string.IsNullOrWhiteSpace(row.CarModel))
                product.CarModel = row.CarModel.Trim();
            if (!string.IsNullOrWhiteSpace(row.VehicleColor))
                product.VehicleColor = row.VehicleColor.Trim();
            if (!string.IsNullOrWhiteSpace(row.PlateNumber))
                product.PlateNumber = row.PlateNumber.Trim();
            product.PlateType = AlMuhasib.Core.Helpers.VehiclePlateTypeHelper.Parse(row.PlateTypeText);
            if (int.TryParse(row.PassengerCountText?.Trim(), out var passengers) && passengers >= 0)
                product.PassengerCount = passengers;
        }

        if (ShowProductWeightFields)
        {
            if (row.Weight > 0)
                product.Weight = row.Weight;
            if (!string.IsNullOrWhiteSpace(row.WeightUnit))
                product.WeightUnit = row.WeightUnit.Trim();
        }

        if (ShowProductDiscountFields)
        {
            product.DiscountType = row.ParseDiscountType();
            product.DiscountValue = row.DiscountValue;
            product.DiscountExpiresAt = row.ParseDiscountExpiry();
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

    /// <summary>يُربَط من الواجهة لفرض CommitEdit على الجدول قبل الحفظ.</summary>
    public Action? RequestCommitGridEdits { get; set; }

    private async Task<bool> SaveCustomersAsync()
    {
        // أخرج من وضع التحرير في الجدول حتى تُطبَّق القيم (خصوصاً رصيد الدولار)
        System.Windows.Input.Keyboard.ClearFocus();
        RequestCommitGridEdits?.Invoke();

        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        if (rows.Count == 0) { Warn("أضف عملاء أو اضغط تخطي"); return false; }
        if (rows.Any(r => !r.IsValid))
        {
            Warn("صحّح الصفوف غير الصالحة أولاً");
            return false;
        }

        if (!await EnsureOpeningUsdFxReadyAsync(rows))
            return false;

        var withoutBalance = rows.Where(r => !HasPartyOpeningBalance(r)).ToList();
        var withBalance = rows.Where(HasPartyOpeningBalance).ToList();
        var saved = 0;
        var failed = 0;

        foreach (var row in withoutBalance)
        {
            try
            {
                await EnsureCustomerExistsAsync(row);
                saved++;
            }
            catch (Exception ex)
            {
                failed++;
                StatusMessage = ex.Message;
            }
        }

        try { await _unitOfWork.SaveChangesAsync(); } catch { /* لا سياق نشط */ }

        if (withBalance.Count > 0)
        {
            if (withBalance.Any(r =>
                    r.AmountUsd <= 0
                    && r.CostCurrency == AccountingCurrency.USD
                    && r.Amount > 0
                    && r.FxRate <= 0
                    && InitialUsdToIqd <= 0))
            {
                Warn("بعض أرصدة العملاء بالدولار بدون سعر صرف — صحّحها أولاً");
                return false;
            }

            var requests = withBalance.SelectMany(BuildPartyOpeningRequests).ToList();
            if (requests.Count == 0)
            {
                Warn("لم يُكوَّن أي طلب رصيد آجل — تحقق من مبالغ الدينار/الدولار");
                return false;
            }

            var result = await _openingPartyBalance.CreateCustomerOpeningBalancesBatchAsync(requests);

            saved += result.SuccessCount;
            failed += result.FailedCount;
            if (result.Errors.Count > 0)
            {
                BeautifulMessageDialog.ShowWarning(
                    $"حُفظ {result.SuccessCount} رصيد آجل، وفشل {result.FailedCount}:\n"
                    + string.Join("\n", result.Errors.Take(5)));
                if (result.SuccessCount == 0 && withoutBalance.Count == 0)
                    return false;
            }

            // علّم الصفوف المحفوظة كموجودة حتى لا تُعاد كجديدة فقط
            foreach (var row in withBalance)
                row.IsExisting = true;
        }

        LastImportedCount = saved;
        LastSkippedCount = failed;
        if (saved == 0)
        {
            BeautifulMessageDialog.ShowError(
                failed > 0 ? $"لم يُحفظ أي عميل:\n{StatusMessage}" : "لم يُحفظ أي عميل");
            return false;
        }
        return true;
    }

    private async Task<bool> EnsureOpeningUsdFxReadyAsync(IReadOnlyList<MigrationNamedBalanceRow> rows)
    {
        var needsUsd = EnableMultiCurrency && rows.Any(r =>
            r.AmountUsd > 0
            || (r.Amount > 0 && r.CostCurrency == AccountingCurrency.USD));
        if (!needsUsd)
            return true;

        if (InitialUsdToIqd <= 0)
        {
            try
            {
                var rate = await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(DateTime.Today);
                if (rate > 0)
                    InitialUsdToIqd = rate;
            }
            catch { /* تجاهل — سيظهر التحذير أدناه */ }
        }

        if (InitialUsdToIqd > 0)
            return true;

        Warn("أدخل سعر الصرف الافتتاحي قبل حفظ أرصدة بالدولار");
        return false;
    }

    private static bool HasPartyOpeningBalance(MigrationNamedBalanceRow r)
        => r.Amount > 0 || r.AmountUsd > 0;

    private IEnumerable<OpeningPartyBalanceRequest> BuildPartyOpeningRequests(MigrationNamedBalanceRow r)
    {
        var importRow = new OpeningPartyBalanceImportRow
        {
            PartyName = r.Name.Trim(),
            Phone = r.Phone,
            FileNumber = r.FileNumber,
            Amount = r.Amount,
            AmountUsd = r.AmountUsd,
            Date = r.Date,
            Notes = r.Notes,
            Currency = r.CostCurrency,
            FxRate = r.FxRate > 0 ? r.FxRate : InitialUsdToIqd
        };
        return OpeningPartyBalanceImportExpander.Expand(importRow, InitialUsdToIqd);
    }

    private async Task<bool> SaveSuppliersAsync()
    {
        System.Windows.Input.Keyboard.ClearFocus();
        RequestCommitGridEdits?.Invoke();

        var rows = CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        if (rows.Count == 0) { Warn("أضف موردين أو اضغط تخطي"); return false; }
        if (rows.Any(r => !r.IsValid))
        {
            Warn("صحّح الصفوف غير الصالحة أولاً");
            return false;
        }

        if (!await EnsureOpeningUsdFxReadyAsync(rows))
            return false;

        var withoutBalance = rows.Where(r => !HasPartyOpeningBalance(r)).ToList();
        var withBalance = rows.Where(HasPartyOpeningBalance).ToList();
        var saved = 0;
        var failed = 0;

        foreach (var row in withoutBalance)
        {
            try
            {
                await EnsureSupplierExistsAsync(row);
                saved++;
            }
            catch (Exception ex)
            {
                failed++;
                StatusMessage = ex.Message;
            }
        }

        try { await _unitOfWork.SaveChangesAsync(); } catch { /* لا سياق نشط */ }

        if (withBalance.Count > 0)
        {
            if (withBalance.Any(r =>
                    r.AmountUsd <= 0
                    && r.CostCurrency == AccountingCurrency.USD
                    && r.Amount > 0
                    && r.FxRate <= 0
                    && InitialUsdToIqd <= 0))
            {
                Warn("بعض أرصدة الموردين بالدولار بدون سعر صرف — صحّحها أولاً");
                return false;
            }

            var requests = withBalance.SelectMany(BuildPartyOpeningRequests).ToList();
            if (requests.Count == 0)
            {
                Warn("لم يُكوَّن أي طلب رصيد آجل — تحقق من مبالغ الدينار/الدولار");
                return false;
            }

            var result = await _openingPartyBalance.CreateSupplierOpeningBalancesBatchAsync(requests);

            saved += result.SuccessCount;
            failed += result.FailedCount;
            if (result.Errors.Count > 0)
            {
                BeautifulMessageDialog.ShowWarning(
                    $"حُفظ {result.SuccessCount} رصيد آجل، وفشل {result.FailedCount}:\n"
                    + string.Join("\n", result.Errors.Take(5)));
                if (result.SuccessCount == 0 && withoutBalance.Count == 0)
                    return false;
            }

            foreach (var row in withBalance)
                row.IsExisting = true;
        }

        LastImportedCount = saved;
        LastSkippedCount = failed;
        if (saved == 0)
        {
            BeautifulMessageDialog.ShowError(
                failed > 0 ? $"لم يُحفظ أي مورد:\n{StatusMessage}" : "لم يُحفظ أي مورد");
            return false;
        }
        return true;
    }

    private async Task EnsureCustomerExistsAsync(MigrationNamedBalanceRow row)
    {
        var name = row.Name.Trim();
        var existing = (await _unitOfWork.Customers.FindAsync(c => c.Name == name)).FirstOrDefault();
        if (existing is not null)
        {
            row.SourceId = existing.Id;
            row.IsExisting = true;
            return;
        }

        var branchId = _branchContext.CurrentBranchId
                       ?? throw new InvalidOperationException("يجب اختيار فرع قبل إدخال العملاء");

        var customer = new Customer
        {
            Name = name,
            Phone = string.IsNullOrWhiteSpace(row.Phone) ? null : row.Phone.Trim(),
            FileNumber = string.IsNullOrWhiteSpace(row.FileNumber) ? null : row.FileNumber.Trim(),
            Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim(),
            BranchId = branchId,
            CreatedBy = "معالج النقل"
        };
        await _unitOfWork.Customers.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();
        row.SourceId = customer.Id;
        row.IsExisting = true;
    }

    private async Task EnsureSupplierExistsAsync(MigrationNamedBalanceRow row)
    {
        var name = row.Name.Trim();
        var existing = (await _unitOfWork.Suppliers.FindAsync(s => s.Name == name)).FirstOrDefault();
        if (existing is not null)
        {
            row.SourceId = existing.Id;
            row.IsExisting = true;
            return;
        }

        var branchId = _branchContext.CurrentBranchId
                       ?? throw new InvalidOperationException("يجب اختيار فرع قبل إدخال الموردين");

        var supplier = new Supplier
        {
            Name = name,
            Phone = string.IsNullOrWhiteSpace(row.Phone) ? null : row.Phone.Trim(),
            Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim(),
            BranchId = branchId,
            CreatedBy = "معالج النقل"
        };
        await _unitOfWork.Suppliers.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync();
        row.SourceId = supplier.Id;
        row.IsExisting = true;
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

    private Warehouse? ResolveWarehouse(IEnumerable<Warehouse> warehouses, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return null;
        var key = displayName.Trim();

        // صيغة العرض: "الاسم (الفرع)" — طابق الاسم مع الفرع
        var paren = key.LastIndexOf(" (", StringComparison.Ordinal);
        if (paren > 0 && key.EndsWith(')'))
        {
            var bareName = key[..paren].Trim();
            var branchName = key[(paren + 2)..^1].Trim();
            var branch = AvailableBranches.FirstOrDefault(b =>
                string.Equals(b.Name, branchName, StringComparison.OrdinalIgnoreCase));
            if (branch is not null)
            {
                var byBranch = warehouses.FirstOrDefault(w =>
                    w.BranchId == branch.Id
                    && string.Equals(w.Name, bareName, StringComparison.OrdinalIgnoreCase));
                if (byBranch is not null) return byBranch;
            }

            // إن تعذّرت مطابقة الفرع: الاسم فقط إن كان فريداً
            var nameOnly = warehouses
                .Where(w => string.Equals(w.Name, bareName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return nameOnly.Count == 1 ? nameOnly[0] : null;
        }

        var exact = warehouses
            .Where(w => string.Equals(w.Name, key, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return exact.Count == 1 ? exact[0] : exact.FirstOrDefault();
    }

    private static void Warn(string message) => BeautifulMessageDialog.ShowWarning(message);
}
