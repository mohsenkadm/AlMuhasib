using System.Collections.ObjectModel;
using System.Windows.Media;
using AlMuhasib.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public sealed partial class IraqiCurrencyChangeViewModel : ObservableObject
{
    private readonly Stack<decimal> _paidHistory = new();

    public IraqiCurrencyChangeViewModel(decimal invoiceTotal, bool allowApplyPaid = false)
    {
        InvoiceTotal = Math.Max(0m, invoiceTotal);
        AllowApplyPaid = allowApplyPaid;

        foreach (var denom in IraqiCurrencyHelper.Denominations)
        {
            var (primary, secondary, accent) = IraqiCurrencyHelper.GetColors(denom);
            Denominations.Add(new CurrencyDenominationItem(
                denom,
                IraqiCurrencyHelper.FormatLabel(denom),
                primary,
                secondary,
                accent));
        }

        Recalc();
    }

    public ObservableCollection<CurrencyDenominationItem> Denominations { get; } = [];
    public ObservableCollection<CurrencyBreakdownItem> ChangeBreakdown { get; } = [];
    public ObservableCollection<CurrencyBreakdownItem> PaidBreakdown { get; } = [];

    [ObservableProperty] private decimal _invoiceTotal;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private decimal _changeAmount;
    [ObservableProperty] private decimal _shortfallAmount;
    [ObservableProperty] private bool _showChangeDue;
    [ObservableProperty] private bool _showShortfall;
    [ObservableProperty] private bool _hasPaid;
    [ObservableProperty] private bool _allowApplyPaid;
    [ObservableProperty] private bool _pulseChange;

    public bool Applied { get; private set; }

    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void AddDenomination(CurrencyDenominationItem? item)
    {
        if (item is null) return;
        _paidHistory.Push(item.Value);
        PaidAmount += item.Value;
        Recalc();
    }

    [RelayCommand]
    private void ClearPaid()
    {
        PaidAmount = 0;
        _paidHistory.Clear();
        Recalc();
    }

    [RelayCommand]
    private void UndoLast()
    {
        if (_paidHistory.Count == 0) return;
        PaidAmount = Math.Max(0m, PaidAmount - _paidHistory.Pop());
        Recalc();
    }

    [RelayCommand]
    private void ApplyPaid()
    {
        if (!AllowApplyPaid) return;
        Applied = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();

    private void Recalc()
    {
        HasPaid = PaidAmount > 0;
        ChangeAmount = PaidAmount > InvoiceTotal ? PaidAmount - InvoiceTotal : 0;
        ShortfallAmount = PaidAmount < InvoiceTotal ? InvoiceTotal - PaidAmount : 0;
        ShowChangeDue = ChangeAmount > 0;
        ShowShortfall = HasPaid && ShortfallAmount > 0;

        RebuildBreakdown(ChangeBreakdown, ChangeAmount);
        RebuildBreakdown(PaidBreakdown, PaidAmount);

        // Trigger pulse animation for change display
        PulseChange = false;
        PulseChange = ShowChangeDue;
    }

    private static void RebuildBreakdown(ObservableCollection<CurrencyBreakdownItem> target, decimal amount)
    {
        target.Clear();
        foreach (var entry in IraqiCurrencyHelper.BreakDown(amount))
        {
            var (primary, secondary, accent) = IraqiCurrencyHelper.GetColors(entry.Denomination);
            target.Add(new CurrencyBreakdownItem(
                entry.Denomination,
                IraqiCurrencyHelper.FormatLabel(entry.Denomination),
                entry.Count,
                entry.Total,
                primary,
                secondary,
                accent));
        }
    }
}

public sealed class CurrencyDenominationItem
{
    public CurrencyDenominationItem(decimal value, string label, string primary, string secondary, string accent)
    {
        Value = value;
        Label = label;
        PrimaryBrush = BrushFrom(primary);
        SecondaryBrush = BrushFrom(secondary);
        AccentBrush = BrushFrom(accent);
    }

    public decimal Value { get; }
    public string Label { get; }
    public Brush PrimaryBrush { get; }
    public Brush SecondaryBrush { get; }
    public Brush AccentBrush { get; }
    public string DisplayAmount => $"{Label} د.ع";

    private static SolidColorBrush BrushFrom(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

public sealed class CurrencyBreakdownItem
{
    public CurrencyBreakdownItem(
        decimal denomination,
        string label,
        int count,
        decimal total,
        string primary,
        string secondary,
        string accent)
    {
        Denomination = denomination;
        Label = label;
        Count = count;
        Total = total;
        PrimaryBrush = BrushFrom(primary);
        SecondaryBrush = BrushFrom(secondary);
        AccentBrush = BrushFrom(accent);
    }

    public decimal Denomination { get; }
    public string Label { get; }
    public int Count { get; }
    public decimal Total { get; }
    public Brush PrimaryBrush { get; }
    public Brush SecondaryBrush { get; }
    public Brush AccentBrush { get; }
    public string CountLabel => $"× {Count}";
    public string TotalLabel => $"{Total:N0} د.ع";

    private static SolidColorBrush BrushFrom(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
