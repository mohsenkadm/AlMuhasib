using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class OtherIncomeViewModel : PagedViewModelBase
{
    private readonly IOtherIncomeService _service;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExchangeRateService _exchangeRateService;
    private bool _isClearing;
    private bool _saveInProgress;
    private int? _editingId;
    private int? _editingCashBoxId;
    private decimal _editingAmount;

    public OtherIncomeViewModel(
        IOtherIncomeService service,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IExchangeRateService exchangeRateService)
    {
        _service = service;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _exchangeRateService = exchangeRateService;
        PageTitle = "الإيرادات الأخرى";
        CurrencyFilters.Add(new CurrencyFilterOption("كل العملات", null));
        CurrencyFilters.Add(new CurrencyFilterOption("دينار", AccountingCurrency.IQD));
        CurrencyFilters.Add(new CurrencyFilterOption("دولار", AccountingCurrency.USD));
        FilterCurrency = CurrencyFilters[0];
    }

    public ObservableCollection<OtherIncomeType> IncomeTypes { get; } = [];
    public ObservableCollection<OtherIncomeType> ActiveIncomeTypes { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];
    public ObservableCollection<OtherIncome> Incomes { get; } = [];
    public ObservableCollection<CurrencyFilterOption> CurrencyFilters { get; } = [];

    [ObservableProperty] private string _newTypeName = string.Empty;
    [ObservableProperty] private OtherIncomeType? _selectedType;
    [ObservableProperty] private string _editTypeName = string.Empty;
    [ObservableProperty] private bool _isEditingType;

    [ObservableProperty] private OtherIncomeType? _formIncomeType;
    [ObservableProperty] private decimal _formAmount;
    [ObservableProperty] private DateTime _formDate = DateTime.Today;
    [ObservableProperty] private CashBox? _formCashBox;
    [ObservableProperty] private string _formNotes = string.Empty;
    [ObservableProperty] private bool _isEditingIncome;
    [ObservableProperty] private string _formTitle = "إضافة إيراد آخر";
    [ObservableProperty] private string _saveButtonText = "حفظ الإيراد";
    [ObservableProperty] private bool _canSaveCurrent;

    [ObservableProperty] private OtherIncomeType? _filterType;
    [ObservableProperty] private CashBox? _filterCashBox;
    [ObservableProperty] private CurrencyFilterOption? _filterCurrency;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private decimal _totalFiltered;

    public bool ShowCashImpact => FormCashBox is not null;

    public string CashImpactCurrency => FormCashBox?.Currency == AccountingCurrency.USD ? "$" : "د.ع";

    public decimal SelectedCashBalance => FormCashBox?.Balance ?? 0;

    public decimal CashMovementAmount => FormAmount > 0 ? FormAmount : 0;

    public decimal CashBalanceAfter
    {
        get
        {
            if (FormCashBox is null) return 0;
            var balance = FormCashBox.Balance;
            if (_editingId is not null && _editingCashBoxId == FormCashBox.Id)
                balance -= _editingAmount;
            return balance + CashMovementAmount;
        }
    }

    public bool CashImpactShortfall => FormCashBox is not null && CashBalanceAfter < 0;

    partial void OnFormCashBoxChanged(CashBox? value) => NotifyCashImpact();

    partial void OnFormAmountChanged(decimal value) => NotifyCashImpact();

    private void NotifyCashImpact()
    {
        OnPropertyChanged(nameof(ShowCashImpact));
        OnPropertyChanged(nameof(CashImpactCurrency));
        OnPropertyChanged(nameof(SelectedCashBalance));
        OnPropertyChanged(nameof(CashMovementAmount));
        OnPropertyChanged(nameof(CashBalanceAfter));
        OnPropertyChanged(nameof(CashImpactShortfall));
    }

    partial void OnSelectedTypeChanged(OtherIncomeType? value)
    {
        if (value is not null)
            EditTypeName = value.Name;
        IsEditingType = false;
    }

    partial void OnFilterTypeChanged(OtherIncomeType? value) { if (!_isClearing) _ = LoadIncomesAsync(); }
    partial void OnFilterCashBoxChanged(CashBox? value) { if (!_isClearing) _ = LoadIncomesAsync(); }
    partial void OnFilterCurrencyChanged(CurrencyFilterOption? value) { if (!_isClearing) _ = LoadIncomesAsync(); }
    partial void OnFilterFromDateChanged(DateTime? value) { if (!_isClearing) _ = LoadIncomesAsync(); }
    partial void OnFilterToDateChanged(DateTime? value) { if (!_isClearing) _ = LoadIncomesAsync(); }

    public override async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, OtherIncomeService.ScreenName);
            RefreshSaveState();
            await _service.EnsureDefaultTypesAsync();
            await LoadTypesAsync();
            await LoadCashBoxesAsync();
            await LoadIncomesAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddTypeAsync()
    {
        if (!CanAdd || string.IsNullOrWhiteSpace(NewTypeName)) return;
        try
        {
            IsBusy = true;
            var created = await _service.AddTypeAsync(NewTypeName.Trim());
            IncomeTypes.Add(created);
            if (created.IsActive)
                ActiveIncomeTypes.Add(created);
            NewTypeName = string.Empty;
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void StartEditType()
    {
        if (SelectedType is null || !CanEdit) return;
        EditTypeName = SelectedType.Name;
        IsEditingType = true;
    }

    [RelayCommand]
    private async Task SaveEditTypeAsync()
    {
        if (SelectedType is null || !CanEdit || string.IsNullOrWhiteSpace(EditTypeName)) return;
        try
        {
            IsBusy = true;
            await _service.UpdateTypeAsync(SelectedType.Id, EditTypeName.Trim());
            SelectedType.Name = EditTypeName.Trim();
            IsEditingType = false;
            await LoadTypesAsync();
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelEditType()
    {
        IsEditingType = false;
        if (SelectedType is not null)
            EditTypeName = SelectedType.Name;
    }

    [RelayCommand]
    private async Task ToggleTypeActiveAsync()
    {
        if (SelectedType is null || !CanEdit) return;
        try
        {
            IsBusy = true;
            var next = !SelectedType.IsActive;
            await _service.SetTypeActiveAsync(SelectedType.Id, next);
            SelectedType.IsActive = next;
            await LoadTypesAsync();
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteTypeAsync()
    {
        if (SelectedType is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm($"هل تريد حذف نوع الإيراد \"{SelectedType.Name}\"؟"))
            return;
        try
        {
            IsBusy = true;
            await _service.DeleteTypeAsync(SelectedType.Id);
            IncomeTypes.Remove(SelectedType);
            ActiveIncomeTypes.Remove(SelectedType);
            SelectedType = null;
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveIncomeAsync()
    {
        if (_saveInProgress || !CanSaveCurrent) return;
        if (FormIncomeType is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع الإيراد");
            return;
        }
        if (FormAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل مبلغاً صحيحاً");
            return;
        }
        if (FormCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة");
            return;
        }

        try
        {
            _saveInProgress = true;
            IsBusy = true;
            var currency = FormCashBox.Currency;
            var fxRate = 1m;
            if (currency == AccountingCurrency.USD)
            {
                fxRate = await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(FormDate);
                if (fxRate <= 0)
                {
                    BeautifulMessageDialog.ShowWarning("سعر الصرف مطلوب للإيراد بالدولار. سجّل سعر الصرف اليومي أولاً.");
                    return;
                }
            }

            var notes = string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes.Trim();
            if (_editingId is int id)
            {
                await _service.UpdateAsync(id, FormIncomeType.Id, FormAmount, FormDate, FormCashBox.Id, notes, currency, fxRate);
                BeautifulMessageDialog.ShowSuccess("تم تعديل الإيراد");
                CancelEdit();
            }
            else
            {
                await _service.AddAsync(FormIncomeType.Id, FormAmount, FormDate, FormCashBox.Id, notes, currency, fxRate);
                FormIncomeType = null;
                FormAmount = 0;
                FormDate = DateTime.Today;
                FormNotes = string.Empty;
                BeautifulMessageDialog.ShowSuccess("تم إضافة الإيراد");
            }

            await LoadIncomesAsync();
            await LoadCashBoxesAsync();
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally
        {
            _saveInProgress = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BeginEdit(OtherIncome? income)
    {
        if (income is null || !CanEdit) return;
        _editingId = income.Id;
        _editingCashBoxId = income.CashBoxId;
        _editingAmount = income.Amount;
        IsEditingIncome = true;
        FormTitle = $"تعديل الإيراد {income.IncomeNumber}";
        SaveButtonText = "حفظ التعديل";
        FormIncomeType = ActiveIncomeTypes.FirstOrDefault(t => t.Id == income.OtherIncomeTypeId)
            ?? IncomeTypes.FirstOrDefault(t => t.Id == income.OtherIncomeTypeId);
        FormAmount = income.Amount;
        FormDate = income.Date;
        FormCashBox = CashBoxes.FirstOrDefault(c => c.Id == income.CashBoxId);
        FormNotes = income.Notes ?? string.Empty;
        RefreshSaveState();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingId = null;
        _editingCashBoxId = null;
        _editingAmount = 0;
        IsEditingIncome = false;
        FormTitle = "إضافة إيراد آخر";
        SaveButtonText = "حفظ الإيراد";
        FormIncomeType = null;
        FormAmount = 0;
        FormDate = DateTime.Today;
        FormNotes = string.Empty;
        RefreshSaveState();
        NotifyCashImpact();
    }

    [RelayCommand]
    private async Task DeleteIncomeAsync(OtherIncome? income)
    {
        if (income is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm(
                $"حذف الإيراد {income.IncomeNumber} بمبلغ {income.Amount:N0}؟ سيُخصم المبلغ من القاصة."))
            return;

        try
        {
            IsBusy = true;
            await _service.DeleteAsync(income.Id);
            if (_editingId == income.Id)
                CancelEdit();
            BeautifulMessageDialog.ShowSuccess("تم حذف الإيراد");
            await LoadCashBoxesAsync();
            await LoadIncomesAsync();
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadIncomesAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        _isClearing = true;
        FilterType = null;
        FilterCashBox = null;
        FilterCurrency = CurrencyFilters[0];
        FilterFromDate = null;
        FilterToDate = null;
        SearchText = string.Empty;
        CurrentPage = 1;
        _isClearing = false;
        await LoadIncomesAsync();
    }

    protected override Task OnPageChangedAsync() => LoadIncomesAsync();

    [RelayCommand]
    private void ExportIncomes()
    {
        if (!CanExport) return;
        var columns = new[] { "الرقم", "النوع", "المبلغ", "العملة", "التاريخ", "القاصة", "ملاحظات" };
        var rows = Incomes.Select(i => new object[]
        {
            i.IncomeNumber,
            i.OtherIncomeType?.Name ?? "",
            i.Amount.ToString("N0"),
            i.CurrencyLabel,
            i.Date.ToString("yyyy/MM/dd"),
            i.CashBox?.Name ?? "",
            i.Notes ?? ""
        }).ToList();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"الإيرادات_الأخرى_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            _exportService.ExportToExcel(dialog.FileName, "الإيرادات الأخرى", columns, rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
    }

    [RelayCommand]
    private void PrintIncomes()
    {
        if (!CanPrint) return;
        var columns = new[] { "الرقم", "النوع", "المبلغ", "العملة", "التاريخ", "القاصة", "ملاحظات" };
        var rows = Incomes.Select(i => new object[]
        {
            i.IncomeNumber,
            i.OtherIncomeType?.Name ?? "",
            i.Amount.ToString("N0"),
            i.CurrencyLabel,
            i.Date.ToString("yyyy/MM/dd"),
            i.CashBox?.Name ?? "",
            i.Notes ?? ""
        }).ToList();
        _exportService.PrintTable("الإيرادات الأخرى", columns, rows);
    }

    private async Task LoadTypesAsync()
    {
        var selectedId = SelectedType?.Id;
        var formId = FormIncomeType?.Id;
        var types = (await _service.GetAllTypesAsync()).ToList();
        IncomeTypes.Clear();
        ActiveIncomeTypes.Clear();
        foreach (var type in types)
        {
            IncomeTypes.Add(type);
            if (type.IsActive)
                ActiveIncomeTypes.Add(type);
        }

        SelectedType = IncomeTypes.FirstOrDefault(t => t.Id == selectedId);
        FormIncomeType = ActiveIncomeTypes.FirstOrDefault(t => t.Id == formId)
            ?? IncomeTypes.FirstOrDefault(t => t.Id == formId);
    }

    private async Task LoadCashBoxesAsync()
    {
        var selectedId = FormCashBox?.Id;
        var filterId = FilterCashBox?.Id;
        var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
        CashBoxes.Clear();
        foreach (var box in cashBoxes)
            CashBoxes.Add(box);
        FormCashBox = CashBoxes.FirstOrDefault(c => c.Id == selectedId);
        FilterCashBox = CashBoxes.FirstOrDefault(c => c.Id == filterId);
    }

    private async Task LoadIncomesAsync()
    {
        try
        {
            var (items, totalCount) = await _service.GetPagedAsync(
                CurrentPage, PageSize,
                FilterType?.Id,
                FilterCashBox?.Id,
                FilterCurrency?.Currency,
                FilterFromDate,
                FilterToDate,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

            Incomes.Clear();
            foreach (var item in items)
                Incomes.Add(item);
            ApplyPaginationStats(totalCount);
            TotalFiltered = items.Sum(i => i.Amount);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
    }

    private void RefreshSaveState() => CanSaveCurrent = IsEditingIncome ? CanEdit : CanAdd;
}

public sealed record CurrencyFilterOption(string Name, AccountingCurrency? Currency)
{
    public override string ToString() => Name;
}
