using System.Windows.Media;
using AlMuhasib.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>صف معاينة تعديل سعر البيع الجماعي.</summary>
public partial class BulkPriceUpdateRow : ObservableObject
{
    public int ProductId { get; init; }
    public int? ProductPriceId { get; set; }
    public string ProductName { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int CategoryId { get; init; }

    /// <summary>سعر الشراء الحالي (يُحفظ كما هو عند الـ Upsert).</summary>
    public decimal PurchasePriceIqd { get; set; }
    public decimal PurchasePriceUsd { get; set; }

    /// <summary>أسعار البيع غير المعدَّلة للعملة الأخرى.</summary>
    public decimal OtherCurrencySalePrice { get; set; }

    public event Action? SelectionOrPriceChanged;

    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private decimal _currentPrice;
    [ObservableProperty] private decimal _newPrice;
    [ObservableProperty] private bool _isManualOverride;
    [ObservableProperty] private AccountingCurrency _currency = AccountingCurrency.IQD;

    private bool _suppressNewPriceNotify;

    public decimal Difference => NewPrice - CurrentPrice;
    public bool HasChange => Math.Abs(Difference) > 0.0000001m;
    public bool IsIncrease => Difference > 0;
    public bool IsDecrease => Difference < 0;
    public string SignText => IsIncrease ? "+" : IsDecrease ? "−" : "=";
    public string DifferenceText =>
        Difference == 0
            ? "0"
            : $"{(Difference > 0 ? "+" : "")}{Difference.ToString(Currency == AccountingCurrency.USD ? "N2" : "N0")}";

    public string CurrentPriceText =>
        CurrentPrice.ToString(Currency == AccountingCurrency.USD ? "N2" : "N0");

    public string NewPriceText =>
        NewPrice.ToString(Currency == AccountingCurrency.USD ? "N2" : "N0");

    public Brush NewPriceBrush =>
        IsIncrease ? BrushesIncrease :
        IsDecrease ? BrushesDecrease :
        BrushesNeutral;

    public Brush DifferenceBrush => NewPriceBrush;

    private static readonly SolidColorBrush BrushesIncrease = CreateFrozenBrush("#2E7D32");
    private static readonly SolidColorBrush BrushesDecrease = CreateFrozenBrush("#C62828");
    private static readonly SolidColorBrush BrushesNeutral = CreateFrozenBrush("#546E7A");

    private static SolidColorBrush CreateFrozenBrush(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
        brush.Freeze();
        return brush;
    }

    partial void OnIsSelectedChanged(bool value) => SelectionOrPriceChanged?.Invoke();

    partial void OnCurrentPriceChanged(decimal value) => NotifyPriceDerived();

    partial void OnCurrencyChanged(AccountingCurrency value) => NotifyPriceDerived();

    partial void OnNewPriceChanged(decimal value)
    {
        if (!_suppressNewPriceNotify && !_isApplyingCalculated)
            IsManualOverride = true;
        NotifyPriceDerived();
        if (!_suppressNewPriceNotify)
            SelectionOrPriceChanged?.Invoke();
    }

    private bool _isApplyingCalculated;

    public void SetCalculatedNewPrice(decimal price)
    {
        _isApplyingCalculated = true;
        _suppressNewPriceNotify = true;
        try
        {
            NewPrice = price;
            IsManualOverride = false;
        }
        finally
        {
            _suppressNewPriceNotify = false;
            _isApplyingCalculated = false;
        }

        NotifyPriceDerived();
        SelectionOrPriceChanged?.Invoke();
    }

    public void ResetToCurrent()
    {
        SetCalculatedNewPrice(CurrentPrice);
    }

    private void NotifyPriceDerived()
    {
        OnPropertyChanged(nameof(Difference));
        OnPropertyChanged(nameof(HasChange));
        OnPropertyChanged(nameof(IsIncrease));
        OnPropertyChanged(nameof(IsDecrease));
        OnPropertyChanged(nameof(SignText));
        OnPropertyChanged(nameof(DifferenceText));
        OnPropertyChanged(nameof(CurrentPriceText));
        OnPropertyChanged(nameof(NewPriceText));
        OnPropertyChanged(nameof(NewPriceBrush));
        OnPropertyChanged(nameof(DifferenceBrush));
    }
}
