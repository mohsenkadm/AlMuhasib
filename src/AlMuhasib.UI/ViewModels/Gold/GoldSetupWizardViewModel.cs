using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSetupWizardViewModel : ViewModelBase
{
    private readonly IGoldSettingsService _settingsService;
    private readonly IGoldCashService _cashService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IGoldOpeningBalanceService _openingService;
    private readonly IGoldPricingService _pricingService;
    private readonly IGoldExpenseService _expenseService;

    public event Action? SetupCompleted;

    [ObservableProperty] private int _currentStep;
    public int TotalSteps => 5;
    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool CanGoBack => CurrentStep > 0;
    public bool IsLastStep => CurrentStep == TotalSteps - 1;

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(StepTitle));
    }

    public string StepTitle => CurrentStep switch
    {
        0 => "١ - إعدادات المثقال والعيارات",
        1 => "٢ - القاصات والأرصدة الافتتاحية",
        2 => "٣ - أرصدة المخزون الافتتاحية",
        3 => "٤ - أسعار المثقال وسعر الصرف",
        4 => "٥ - أنواع المصاريف",
        _ => string.Empty
    };

    // Step 0
    [ObservableProperty] private decimal _mithqalGrams = 5m;
    [ObservableProperty] private bool _enableKarat24 = true;
    [ObservableProperty] private bool _enableKarat22 = true;
    [ObservableProperty] private bool _enableKarat21 = true;
    [ObservableProperty] private bool _enableKarat18 = true;

    // Step 1
    public ObservableCollection<GoldSetupCashBoxRow> CashBoxes { get; } = [];
    [ObservableProperty] private string _newCashBoxName = string.Empty;
    [ObservableProperty] private GoldCurrency _newCashBoxCurrency = GoldCurrency.IQD;
    [ObservableProperty] private decimal _newCashBoxBalance;

    public IReadOnlyList<GoldCurrencyOption> Currencies { get; } =
    [
        new(GoldCurrency.IQD, "دينار عراقي"),
        new(GoldCurrency.USD, "دولار أمريكي")
    ];

    // Step 2
    public ObservableCollection<GoldSetupStockRow> OpeningStockItems { get; } = [];
    [ObservableProperty] private int _newStockKarat = 21;
    [ObservableProperty] private decimal _newStockGrams;
    [ObservableProperty] private decimal _newStockCostPerGram;

    public IReadOnlyList<int> KaratOptions { get; } = [24, 22, 21, 18];

    // Step 3
    public ObservableCollection<GoldSetupPriceRow> MithqalPrices { get; } = [];
    [ObservableProperty] private decimal _fxRate;
    [ObservableProperty] private GoldCurrency _priceCurrency = GoldCurrency.IQD;

    // Step 4
    public ObservableCollection<GoldSetupExpenseTypeRow> ExpenseTypes { get; } = [];
    [ObservableProperty] private string _newExpenseTypeName = string.Empty;

    public GoldSetupWizardViewModel(
        IGoldSettingsService settingsService,
        IGoldCashService cashService,
        IGoldWarehouseService warehouseService,
        IGoldOpeningBalanceService openingService,
        IGoldPricingService pricingService,
        IGoldExpenseService expenseService)
    {
        _settingsService = settingsService;
        _cashService = cashService;
        _warehouseService = warehouseService;
        _openingService = openingService;
        _pricingService = pricingService;
        _expenseService = expenseService;
        PageTitle = "إعداد نظام الذهب";

        CashBoxes.Add(new GoldSetupCashBoxRow
        {
            Name = "صندوق الدينار",
            Currency = GoldCurrency.IQD,
            Balance = 0
        });
        CashBoxes.Add(new GoldSetupCashBoxRow
        {
            Name = "صندوق الدولار",
            Currency = GoldCurrency.USD,
            Balance = 0
        });

        foreach (var karat in KaratOptions)
        {
            MithqalPrices.Add(new GoldSetupPriceRow { KaratValue = karat, PricePerMithqal = 0 });
        }

        ExpenseTypes.Add(new GoldSetupExpenseTypeRow { Name = "إيجار" });
        ExpenseTypes.Add(new GoldSetupExpenseTypeRow { Name = "كهرباء" });
        ExpenseTypes.Add(new GoldSetupExpenseTypeRow { Name = "ماء" });
        ExpenseTypes.Add(new GoldSetupExpenseTypeRow { Name = "رواتب" });
        ExpenseTypes.Add(new GoldSetupExpenseTypeRow { Name = "صيانة" });
        ExpenseTypes.Add(new GoldSetupExpenseTypeRow { Name = "مصاريف متنوعة" });
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep == 0 && !ValidateStep0())
            return;
        if (CurrentStep < TotalSteps - 1)
            CurrentStep++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    private bool ValidateStep0()
    {
        if (MithqalGrams <= 0)
        {
            BeautifulMessageDialog.ShowWarning("وزن المثقال يجب أن يكون أكبر من صفر");
            return false;
        }

        if (!EnableKarat24 && !EnableKarat22 && !EnableKarat21 && !EnableKarat18)
        {
            BeautifulMessageDialog.ShowWarning("فعّل عياراً واحداً على الأقل");
            return false;
        }

        return true;
    }

    [RelayCommand]
    private void AddCashBox()
    {
        var name = NewCashBoxName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (NewCashBoxBalance < 0)
        {
            BeautifulMessageDialog.ShowWarning("الرصيد لا يمكن أن يكون سالباً");
            return;
        }

        CashBoxes.Add(new GoldSetupCashBoxRow
        {
            Name = name,
            Currency = NewCashBoxCurrency,
            Balance = NewCashBoxBalance
        });
        NewCashBoxName = string.Empty;
        NewCashBoxBalance = 0;
    }

    [RelayCommand]
    private void RemoveCashBox(GoldSetupCashBoxRow? row)
    {
        if (row is null) return;
        if (CashBoxes.Count <= 1)
        {
            BeautifulMessageDialog.ShowWarning("يجب الإبقاء على قاصة واحدة على الأقل");
            return;
        }

        CashBoxes.Remove(row);
    }

    [RelayCommand]
    private void AddOpeningStock()
    {
        if (NewStockGrams <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل وزناً أكبر من صفر");
            return;
        }

        if (NewStockCostPerGram < 0)
        {
            BeautifulMessageDialog.ShowWarning("التكلفة لا يمكن أن تكون سالبة");
            return;
        }

        OpeningStockItems.Add(new GoldSetupStockRow
        {
            KaratValue = NewStockKarat,
            GramsOnHand = NewStockGrams,
            CostPerGram = NewStockCostPerGram
        });
        NewStockGrams = 0;
        NewStockCostPerGram = 0;
    }

    [RelayCommand]
    private void RemoveOpeningStock(GoldSetupStockRow? row)
    {
        if (row is not null)
            OpeningStockItems.Remove(row);
    }

    [RelayCommand]
    private void AddExpenseType()
    {
        var name = NewExpenseTypeName?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        ExpenseTypes.Add(new GoldSetupExpenseTypeRow { Name = name });
        NewExpenseTypeName = string.Empty;
    }

    [RelayCommand]
    private void RemoveExpenseType(GoldSetupExpenseTypeRow? row)
    {
        if (row is not null)
            ExpenseTypes.Remove(row);
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (!ValidateStep0())
        {
            CurrentStep = 0;
            return;
        }

        if (CashBoxes.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف قاصة واحدة على الأقل");
            CurrentStep = 1;
            return;
        }

        if (ExpenseTypes.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف نوع مصروف واحد على الأقل");
            CurrentStep = 4;
            return;
        }

        IsBusy = true;
        try
        {
            await _settingsService.EnsureDefaultsAsync();

            var enabled = new List<int>();
            if (EnableKarat24) enabled.Add(24);
            if (EnableKarat22) enabled.Add(22);
            if (EnableKarat21) enabled.Add(21);
            if (EnableKarat18) enabled.Add(18);

            var settings = await _settingsService.GetSettingsAsync();
            settings.MithqalGrams = MithqalGrams;
            settings.EnabledKaratsCsv = string.Join(",", enabled);
            await _settingsService.SaveSettingsAsync(settings);

            var existingBoxes = (await _cashService.GetCashBoxesAsync(activeOnly: false)).ToList();
            var usedIds = new HashSet<int>();

            foreach (var row in CashBoxes)
            {
                var match = existingBoxes.FirstOrDefault(b =>
                        !usedIds.Contains(b.Id) &&
                        b.Currency == row.Currency &&
                        (b.IsDefault || string.Equals(b.Name, row.Name, StringComparison.OrdinalIgnoreCase)))
                    ?? existingBoxes.FirstOrDefault(b =>
                        !usedIds.Contains(b.Id) && b.Currency == row.Currency && b.IsDefault);

                if (match is not null)
                {
                    usedIds.Add(match.Id);
                    match.Name = row.Name.Trim();
                    match.Balance = row.Balance;
                    match.IsActive = true;
                    if (!existingBoxes.Any(b => b.Currency == row.Currency && b.IsDefault && b.Id != match.Id))
                        match.IsDefault = true;
                    await _cashService.UpdateCashBoxAsync(match);
                }
                else
                {
                    var created = await _cashService.CreateCashBoxAsync(new GoldCashBox
                    {
                        Name = row.Name.Trim(),
                        Currency = row.Currency,
                        Balance = row.Balance,
                        IsDefault = !existingBoxes.Any(b => b.Currency == row.Currency && b.IsDefault),
                        IsActive = true
                    });
                    existingBoxes.Add(created);
                    usedIds.Add(created.Id);
                }
            }

            var warehouse = await _warehouseService.EnsureDefaultAsync();
            foreach (var stock in OpeningStockItems.Where(s => s.GramsOnHand > 0))
            {
                await _openingService.SetOpeningStockAsync(new GoldOpeningStockRequest
                {
                    WarehouseId = warehouse.Id,
                    KaratValue = stock.KaratValue,
                    GramsOnHand = stock.GramsOnHand,
                    CostPerGram = stock.CostPerGram > 0 ? stock.CostPerGram : null,
                    Notes = "رصيد افتتاحي من معالج الإعداد"
                });
            }

            foreach (var price in MithqalPrices.Where(p => p.PricePerMithqal > 0))
            {
                await _pricingService.SavePriceAsync(new GoldMithqalPrice
                {
                    PriceDate = DateTime.Today,
                    KaratValue = price.KaratValue,
                    PricePerMithqal = price.PricePerMithqal,
                    Currency = PriceCurrency,
                    Notes = "سعر افتتاحي من معالج الإعداد"
                });
            }

            if (FxRate > 0)
            {
                await _pricingService.SaveFxRateAsync(new GoldFxRate
                {
                    RateDate = DateTime.Today,
                    UsdToIqd = FxRate,
                    Notes = "سعر صرف افتتاحي من معالج الإعداد"
                });
            }

            var existingTypes = await _expenseService.GetExpenseTypesAsync(activeOnly: false);
            var existingNames = new HashSet<string>(
                existingTypes.Select(t => t.Name.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var et in ExpenseTypes)
            {
                var name = et.Name?.Trim();
                if (string.IsNullOrEmpty(name) || existingNames.Contains(name))
                    continue;

                await _expenseService.CreateExpenseTypeAsync(new GoldExpenseType
                {
                    Name = name,
                    IsActive = true
                });
                existingNames.Add(name);
            }

            await _settingsService.MarkConfiguredAsync();
            BeautifulMessageDialog.ShowSuccess("تم إكمال إعداد نظام الذهب بنجاح");
            SetupCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الإعداد: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class GoldSetupCashBoxRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private GoldCurrency _currency = GoldCurrency.IQD;
    [ObservableProperty] private decimal _balance;
}

public partial class GoldSetupStockRow : ObservableObject
{
    [ObservableProperty] private int _karatValue = 21;
    [ObservableProperty] private decimal _gramsOnHand;
    [ObservableProperty] private decimal _costPerGram;
}

public partial class GoldSetupPriceRow : ObservableObject
{
    [ObservableProperty] private int _karatValue;
    [ObservableProperty] private decimal _pricePerMithqal;
}

public partial class GoldSetupExpenseTypeRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
}
