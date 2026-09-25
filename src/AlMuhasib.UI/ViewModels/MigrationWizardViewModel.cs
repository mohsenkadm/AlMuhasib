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
    [ObservableProperty] private int _newRowInt;
    [ObservableProperty] private int _newRowInt2;
    [ObservableProperty] private DateTime _newRowDate = DateTime.Today;
    [ObservableProperty] private string _newRowKind = "Cash";

    public ObservableCollection<MigrationStepInfo> Steps { get; } = [];
    public ObservableCollection<MigrationNamedBalanceRow> CurrentRows { get; } = [];
    public ObservableCollection<string> AvailableCategories { get; } = [];
    public ObservableCollection<string> AvailableProducts { get; } = [];
    public ObservableCollection<string> AvailablePricingTypes { get; } = [];
    public ObservableCollection<string> AvailableWarehouses { get; } = [];

    public int TotalSteps => Steps.Count;
    public MigrationStepInfo? CurrentStepInfo =>
        CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count ? Steps[CurrentStepIndex] : null;
    public MigrationStepKind? CurrentKind => CurrentStepInfo?.Kind;
    public string StepTitle => CurrentStepInfo?.Title ?? "اكتمل";
    public string StepDescription => CurrentStepInfo?.Description ?? "تم إكمال جميع خطوات النقل.";
    public bool CanGoBack => CurrentStepIndex > 0 && !IsCompleted;
    public bool IsLastStep => CurrentStepIndex >= TotalSteps - 1 && TotalSteps > 0;
    public bool CanGoNext => !IsCompleted;
    public bool HasLastResult => LastImportedCount > 0 || LastSkippedCount > 0;
    public bool HasRows => CurrentRows.Count > 0;
    public bool IsCapitalStep => CurrentKind == MigrationStepKind.Capital;
    public bool ShowManualEntryForm => IsManualEntryMode && !IsCapitalStep && !IsCompleted;
    public bool ShowExcelPanel => IsExcelMode && !IsCapitalStep && !IsCompleted;
    public bool ShowDataGrid => !IsCapitalStep && HasRows && !IsCompleted;

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
        BuildSteps();
        await RefreshLookupsAsync();
        NotifyStepProps();
    }

    private void BuildSteps()
    {
        Steps.Clear();
        void Add(MigrationStepKind kind, string title, string shortTitle, string desc, PackIconKind icon, bool optional = true)
            => Steps.Add(new MigrationStepInfo
            {
                Kind = kind,
                Title = title,
                ShortTitle = shortTitle,
                Description = desc,
                Icon = icon,
                IsOptional = optional
            });

        if (_needsCapital)
        {
            Add(MigrationStepKind.Capital,
                "١ — رأس المال والأرباح الافتتاحية", "رأس المال",
                "أدخل رأس المال والأرباح الافتتاحية إن لم تكن محفوظة بعد.",
                PackIconKind.Cash, optional: false);
        }

        Add(MigrationStepKind.CashAndBank, "القاصات والمصرف", "قاصات",
            "أدخل القاصات وحسابات المصرف مع أرصدتها الافتتاحية (يدوياً أو من Excel).",
            PackIconKind.SafeSquareOutline);
        Add(MigrationStepKind.Investors, "المستثمرون وأرصدتهم", "مستثمرون",
            "أدخل المستثمرين حتى لو لم يكونوا موجودين مسبقاً مع الرصيد الافتتاحي.",
            PackIconKind.AccountCash);
        Add(MigrationStepKind.Warehouses, "المخازن", "مخازن",
            "أنشئ المخازن قبل إدخال المنتجات والأرصدة الافتتاحية.",
            PackIconKind.Warehouse, optional: false);
        Add(MigrationStepKind.Categories, "أصناف المنتجات", "أصناف",
            "أدخل تصنيفات المنتجات قبل إضافة المنتجات.",
            PackIconKind.Shape);
        Add(MigrationStepKind.Products, "المنتجات والأرصدة", "منتجات",
            "أدخل المنتجات مع الصنف والسعر المفرد والرصيد الافتتاحي (كمية × تكلفة).",
            PackIconKind.PackageVariant);
        Add(MigrationStepKind.PricingTypes, "أنواع التسعير", "تسعير",
            "أدخل أنواع التسعير (مفرد، جملة، وكيل...).",
            PackIconKind.TagMultiple);
        Add(MigrationStepKind.ProductPricing, "تسعير المنتجات", "أسعار",
            "حدد أسعار البيع/الشراء للمنتجات المضافة حسب نوع التسعير.",
            PackIconKind.TagOutline);
        Add(MigrationStepKind.Customers, "العملاء وأرصدتهم", "عملاء",
            "أدخل العملاء مع أرصدة افتتاحية آجلة — يُنشأ العميل إن لم يكن موجوداً.",
            PackIconKind.AccountGroup);
        Add(MigrationStepKind.Suppliers, "الموردون وأرصدتهم", "موردون",
            "أدخل الموردين مع أرصدة افتتاحية آجلة — يُنشأ المورد إن لم يكن موجوداً.",
            PackIconKind.TruckDelivery);
        Add(MigrationStepKind.ExpenseTypes, "أنواع المصاريف", "مصاريف",
            "أدخل أنواع المصاريف الافتتاحية للنظام.",
            PackIconKind.CashMinus);
        Add(MigrationStepKind.Installments, "الأقساط الافتتاحية", "أقساط",
            "أدخل أرصدة الأقساط الافتتاحية كما في شاشة أرصدة الأقساط.",
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
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsActive = i == value;
            Steps[i].IsCompleted = i < value || _savedSteps.Contains(Steps[i].Kind);
            Steps[i].IsSaved = _savedSteps.Contains(Steps[i].Kind);
        }
        CurrentRows.Clear();
        SelectedFilePath = null;
        LastImportedCount = 0;
        LastSkippedCount = 0;
        StatusMessage = string.Empty;
        ResetNewRowFields();
        StepTransitionToken++;
        NotifyStepProps();
        _ = RefreshLookupsAsync();
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
        OnPropertyChanged(nameof(HasLastResult));
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(IsCapitalStep));
        OnPropertyChanged(nameof(ShowManualEntryForm));
        OnPropertyChanged(nameof(ShowExcelPanel));
        OnPropertyChanged(nameof(ShowDataGrid));
    }

    private void ResetNewRowFields()
    {
        NewRowName = string.Empty;
        NewRowPhone = null;
        NewRowExtra = null;
        NewRowAmount = 0;
        NewRowAmount2 = 0;
        NewRowAmount3 = 0;
        NewRowInt = 0;
        NewRowInt2 = 0;
        NewRowDate = DateTime.Today;
        NewRowKind = "Cash";
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
        }
        catch
        {
            // lookups are best-effort during wizard
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
                row.Kind = NewRowKind;
                row.AccountNumber = NewRowExtra;
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
                row.Barcode = NewRowPhone;
                row.UnitPrice = NewRowAmount;
                row.Amount = NewRowAmount;
                row.Quantity = NewRowAmount2;
                row.UnitCost = NewRowAmount3;
                break;
            case MigrationStepKind.ProductPricing:
                if (string.IsNullOrWhiteSpace(NewRowName) || string.IsNullOrWhiteSpace(NewRowExtra))
                { Warn("أدخل اسم المنتج ونوع التسعير"); return; }
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
        StatusMessage = $"أُضيف صف — الإجمالي {CurrentRows.Count}";
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
                ? "الملف لا يحتوي على بيانات"
                : $"معاينة: {CurrentRows.Count} صف جاهز للحفظ";
            OnPropertyChanged(nameof(HasRows));
            OnPropertyChanged(nameof(ShowDataGrid));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
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
                        AccountNumber = r[3]
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
                        Name = r[0], Barcode = r[1], CategoryName = string.IsNullOrWhiteSpace(r[2]) ? "عام" : r[2],
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
                        ErrorText = r.ErrorsText
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
        ErrorText = r.ErrorsText
    };

    [RelayCommand]
    private async Task SaveCurrentStepAsync()
    {
        if (CurrentKind is null || IsCompleted) return;
        try
        {
            IsBusy = true;
            var ok = await PersistStepAsync(CurrentKind.Value);
            if (!ok) return;

            _savedSteps.Add(CurrentKind.Value);
            if (CurrentStepInfo is not null) CurrentStepInfo.IsSaved = true;
            LastImportedCount = Math.Max(LastImportedCount, CurrentRows.Count);
            OnPropertyChanged(nameof(HasLastResult));
            StatusMessage = "تم حفظ الخطوة بنجاح";
            BeautifulMessageDialog.ShowSuccess(StatusMessage);
            await RefreshLookupsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (IsCompleted || CurrentKind is null) return;

        if (!_savedSteps.Contains(CurrentKind.Value))
        {
            var canSkipEmpty = CurrentStepInfo?.IsOptional == true
                               && !IsCapitalStep
                               && CurrentRows.Count == 0;

            if (canSkipEmpty)
            {
                // allow skip of empty optional step
                _savedSteps.Add(CurrentKind.Value);
            }
            else
            {
                var ok = await PersistStepAsync(CurrentKind.Value);
                if (!ok) return;
                _savedSteps.Add(CurrentKind.Value);
                if (CurrentStepInfo is not null) CurrentStepInfo.IsSaved = true;
            }
        }

        if (IsLastStep)
        {
            Finish();
            return;
        }

        CurrentStepIndex++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0 && !IsCompleted)
            CurrentStepIndex--;
    }

    [RelayCommand]
    private void Finish()
    {
        IsCompleted = true;
        NotifyStepProps();
        BeautifulMessageDialog.ShowSuccess("اكتمل معالج النقل — راجع البيانات في الشاشات المناسبة");
    }

    private async Task<bool> PersistStepAsync(MigrationStepKind kind)
    {
        switch (kind)
        {
            case MigrationStepKind.Capital:
                return await SaveCapitalAsync();
            case MigrationStepKind.CashAndBank:
                return await SaveCashAndBankAsync();
            case MigrationStepKind.Investors:
                return await SaveInvestorsAsync();
            case MigrationStepKind.Warehouses:
                return await SaveWarehousesAsync();
            case MigrationStepKind.Categories:
                return await SaveCategoriesAsync();
            case MigrationStepKind.Products:
                return await SaveProductsAsync();
            case MigrationStepKind.PricingTypes:
                return await SavePricingTypesAsync();
            case MigrationStepKind.ProductPricing:
                return await SaveProductPricingAsync();
            case MigrationStepKind.Customers:
                return await SaveCustomersAsync();
            case MigrationStepKind.Suppliers:
                return await SaveSuppliersAsync();
            case MigrationStepKind.ExpenseTypes:
                return await SaveExpenseTypesAsync();
            case MigrationStepKind.Installments:
                return await SaveInstallmentsAsync();
            default:
                return false;
        }
    }

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
        if (CurrentRows.Count == 0) { Warn("أضف قاصة أو مصرفاً واحداً على الأقل، أو تخطَّ إن كانت موجودة"); return false; }
        var existingCash = (await _unitOfWork.CashBoxes.GetAllAsync()).ToList();
        var existingBank = (await _unitOfWork.BankAccounts.GetAllAsync()).ToList();
        var saved = 0;
        foreach (var row in CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            var name = row.Name.Trim();
            if (string.Equals(row.Kind, "Bank", StringComparison.OrdinalIgnoreCase))
            {
                if (existingBank.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                await _unitOfWork.BankAccounts.AddAsync(new BankAccount
                {
                    Name = name,
                    AccountNumber = row.AccountNumber,
                    Balance = row.Amount
                });
            }
            else
            {
                if (existingCash.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                await _unitOfWork.CashBoxes.AddAsync(new CashBox
                {
                    Name = name,
                    Balance = row.Amount
                });
            }
            saved++;
        }
        await _unitOfWork.SaveChangesAsync();
        LastImportedCount = saved;
        return saved > 0 || existingCash.Count + existingBank.Count > 0;
    }

    private async Task<bool> SaveInvestorsAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف مستثمراً واحداً على الأقل أو تخطَّ الخطوة"); return false; }
        var existing = (await _investorService.GetAllInvestorsAsync()).ToList();
        var openingItems = new List<InvestorOpeningBalanceItem>();
        var saved = 0;

        foreach (var row in CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            var match = existing.FirstOrDefault(i =>
                string.Equals(i.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                match = await _investorService.AddInvestorAsync(
                    row.Name.Trim(), row.Phone, row.ProfitPercentage);
                existing.Add(match);
            }

            openingItems.Add(new InvestorOpeningBalanceItem
            {
                InvestorId = match.Id,
                Name = match.Name,
                Phone = match.Phone,
                ProfitPercentage = match.ProfitPercentage,
                OpeningBalance = row.Amount
            });
            saved++;
        }

        if (openingItems.Count > 0)
            await _investorService.SaveOpeningBalancesAsync(openingItems);

        LastImportedCount = saved;
        return saved > 0;
    }

    private async Task<bool> SaveWarehousesAsync()
    {
        var existingCount = await _unitOfWork.Warehouses.CountAsync();
        if (CurrentRows.Count == 0 && existingCount > 0) return true;
        if (CurrentRows.Count == 0)
        {
            Warn("يجب إدخال مخزن واحد على الأقل قبل متابعة المنتجات والأرصدة");
            return false;
        }

        var existing = (await _unitOfWork.Warehouses.GetAllAsync()).ToList();
        var saved = 0;
        foreach (var row in CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            if (existing.Any(w => string.Equals(w.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue;
            await _unitOfWork.Warehouses.AddAsync(new Warehouse
            {
                Name = row.Name.Trim(),
                Location = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes
            });
            saved++;
        }
        await _unitOfWork.SaveChangesAsync();
        LastImportedCount = saved;
        return true;
    }

    private async Task<bool> SaveCategoriesAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف صنفاً أو تخطَّ الخطوة"); return false; }
        var existing = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var saved = 0;
        foreach (var row in CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
        {
            if (existing.Any(c => string.Equals(c.Name, row.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                continue;
            await _unitOfWork.Categories.AddAsync(new Category { Name = row.Name.Trim() });
            saved++;
        }
        await _unitOfWork.SaveChangesAsync();
        LastImportedCount = saved;
        return true;
    }

    private async Task<bool> SaveProductsAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف منتجات أو تخطَّ الخطوة"); return false; }
        var warehouses = (await _unitOfWork.Warehouses.GetAllAsync()).ToList();
        if (warehouses.Count == 0)
        {
            Warn("لا يوجد مخزن — ارجع لخطوة المخازن أولاً");
            return false;
        }

        var warehouseId = warehouses[0].Id;
        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
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
            foreach (var row in CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
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
                        Barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode,
                        CategoryId = category.Id
                    };
                    await _unitOfWork.Products.AddAsync(product);
                    await _unitOfWork.SaveChangesAsync();
                    products.Add(product);
                }

                if (row.Quantity > 0)
                {
                    var stock = (await _unitOfWork.WarehouseStocks.FindAsync(s =>
                        s.WarehouseId == warehouseId && s.ProductId == product.Id)).FirstOrDefault();
                    if (stock is null)
                    {
                        await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                        {
                            WarehouseId = warehouseId,
                            ProductId = product.Id,
                            Quantity = row.Quantity,
                            OpeningQuantity = row.Quantity,
                            UnitCost = row.UnitCost,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        stock.OpeningQuantity = row.Quantity;
                        stock.Quantity = Math.Max(stock.Quantity, row.Quantity);
                        stock.UnitCost = row.UnitCost > 0 ? row.UnitCost : stock.UnitCost;
                        _unitOfWork.WarehouseStocks.Update(stock);
                    }
                }

                if (defaultPricing is not null && row.UnitPrice > 0)
                {
                    var existingPrice = (await _unitOfWork.ProductPrices.FindAsync(p =>
                        p.ProductId == product.Id && p.PricingTypeId == defaultPricing.Id)).FirstOrDefault();
                    if (existingPrice is null)
                    {
                        await _unitOfWork.ProductPrices.AddAsync(new ProductPrice
                        {
                            ProductId = product.Id,
                            PricingTypeId = defaultPricing.Id,
                            SalePrice = row.UnitPrice,
                            PurchasePrice = row.UnitCost
                        });
                    }
                    else
                    {
                        existingPrice.SalePrice = row.UnitPrice;
                        if (row.UnitCost > 0)
                            existingPrice.PurchasePrice = row.UnitCost;
                        _unitOfWork.ProductPrices.Update(existingPrice);
                    }
                }

                saved++;
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            LastImportedCount = saved;
            return saved > 0;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task<bool> SavePricingTypesAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف نوع تسعير أو تخطَّ الخطوة"); return false; }
        var existing = (await _pricingTypeService.GetActiveAsync()).ToList();
        var saved = 0;
        foreach (var row in CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
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
        LastImportedCount = saved;
        return true;
    }

    private async Task<bool> SaveProductPricingAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف أسعار منتجات أو تخطَّ الخطوة"); return false; }
        var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
        var types = (await _pricingTypeService.GetActiveAsync()).ToList();
        var prices = new List<ProductPrice>();
        var skipped = 0;

        foreach (var row in CurrentRows)
        {
            var product = products.FirstOrDefault(p =>
                string.Equals(p.Name, row.ProductName?.Trim(), StringComparison.OrdinalIgnoreCase));
            var type = types.FirstOrDefault(t =>
                string.Equals(t.Name, row.PricingTypeName?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (product is null || type is null)
            {
                skipped++;
                continue;
            }
            prices.Add(new ProductPrice
            {
                ProductId = product.Id,
                PricingTypeId = type.Id,
                SalePrice = row.SalePrice,
                PurchasePrice = row.PurchasePrice
            });
        }

        if (prices.Count > 0)
            await _productPriceService.UpsertManyAsync(prices);

        LastImportedCount = prices.Count;
        LastSkippedCount = skipped;
        if (prices.Count == 0)
        {
            Warn("لم يُحفظ أي سعر — تأكد من أسماء المنتجات وأنواع التسعير");
            return false;
        }
        return true;
    }

    private async Task<bool> SaveCustomersAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف عملاء أو تخطَّ الخطوة"); return false; }
        var invalid = CurrentRows.Count(r => !r.IsValid || string.IsNullOrWhiteSpace(r.Name) || r.Amount <= 0);
        if (invalid > 0) { Warn($"يوجد {invalid} صف غير صالح"); return false; }

        var requests = CurrentRows.Select(r => new OpeningPartyBalanceRequest
        {
            PartyName = r.Name.Trim(),
            Phone = r.Phone,
            FileNumber = r.FileNumber,
            Amount = r.Amount,
            Date = r.Date,
            Notes = r.Notes
        }).ToList();

        var result = await _openingPartyBalance.CreateCustomerOpeningBalancesBatchAsync(requests);
        LastImportedCount = result.SuccessCount;
        LastSkippedCount = result.FailedCount;
        if (result.FailedCount > 0 && result.SuccessCount == 0)
        {
            BeautifulMessageDialog.ShowError(string.Join("\n", result.Errors.Take(5)));
            return false;
        }
        return result.SuccessCount > 0;
    }

    private async Task<bool> SaveSuppliersAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف موردين أو تخطَّ الخطوة"); return false; }
        var invalid = CurrentRows.Count(r => !r.IsValid || string.IsNullOrWhiteSpace(r.Name) || r.Amount <= 0);
        if (invalid > 0) { Warn($"يوجد {invalid} صف غير صالح"); return false; }

        var requests = CurrentRows.Select(r => new OpeningPartyBalanceRequest
        {
            PartyName = r.Name.Trim(),
            Phone = r.Phone,
            FileNumber = r.FileNumber,
            Amount = r.Amount,
            Date = r.Date,
            Notes = r.Notes
        }).ToList();

        var result = await _openingPartyBalance.CreateSupplierOpeningBalancesBatchAsync(requests);
        LastImportedCount = result.SuccessCount;
        LastSkippedCount = result.FailedCount;
        if (result.FailedCount > 0 && result.SuccessCount == 0)
        {
            BeautifulMessageDialog.ShowError(string.Join("\n", result.Errors.Take(5)));
            return false;
        }
        return result.SuccessCount > 0;
    }

    private async Task<bool> SaveExpenseTypesAsync()
    {
        if (CurrentRows.Count == 0) { Warn("أضف أنواع مصاريف أو تخطَّ الخطوة"); return false; }
        var existing = (await _expenseService.GetAllExpenseTypesAsync()).ToList();
        var saved = 0;
        foreach (var row in CurrentRows.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
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
        if (CurrentRows.Count == 0) { Warn("أضف أرصدة أقساط أو تخطَّ الخطوة"); return false; }
        if (CurrentRows.Any(r => !r.IsValid))
        {
            Warn("صحّح الصفوف غير الصالحة أولاً");
            return false;
        }

        var requests = CurrentRows.Select(r => new OpeningInstallmentBalanceRequest
        {
            CustomerName = r.Name.Trim(),
            FileNumber = r.FileNumber,
            TotalAmount = r.Amount,
            NumberOfInstallments = r.NumberOfInstallments,
            PaidInstallmentsCount = r.PaidInstallmentsCount,
            StartDate = r.Date,
            Notes = r.Notes
        }).ToList();

        var result = await _installmentService.CreateOpeningBalancePlansBatchAsync(requests);
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
