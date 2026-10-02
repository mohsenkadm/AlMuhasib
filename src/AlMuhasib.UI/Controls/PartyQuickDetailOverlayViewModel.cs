using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Controls;

public partial class PartyQuickDetailOverlayViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy = true;
    [ObservableProperty] private string _name = "جاري التحميل…";
    [ObservableProperty] private string _typeLabel = string.Empty;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _fileNumber;
    [ObservableProperty] private string _balanceText = "—";
    [ObservableProperty] private string _balanceUsdText = "—";
    [ObservableProperty] private string _dealCountText = "—";
    [ObservableProperty] private string _totalDealAmountText = "—";
    [ObservableProperty] private string _totalDealAmountUsdText = "—";
    [ObservableProperty] private string _lastDealText = "—";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _showMultiCurrency;

    public ObservableCollection<PartyQuickProductRow> Products { get; } = [];
    public ObservableCollection<PartyQuickTimelineRow> RecentTimeline { get; } = [];

    public void Apply(PartyQuickDetailResult data, bool showMultiCurrency = false)
    {
        ShowMultiCurrency = showMultiCurrency;
        Name = data.Name;
        TypeLabel = data.TypeLabel;
        Phone = string.IsNullOrWhiteSpace(data.Phone) ? null : data.Phone;
        Address = string.IsNullOrWhiteSpace(data.Address) ? null : data.Address;
        FileNumber = string.IsNullOrWhiteSpace(data.FileNumber) ? null : data.FileNumber;
        BalanceText = $"{data.Balance:N0} د.ع";
        BalanceUsdText = $"$ {data.BalanceUsd:N2}";
        DealCountText = data.DealCount.ToString("N0");
        TotalDealAmountText = $"{data.TotalDealAmount:N0} د.ع";
        TotalDealAmountUsdText = $"$ {data.TotalDealAmountUsd:N2}";
        var lastSuffix = data.LastDealCurrency == AccountingCurrency.USD ? "$" : "د.ع";
        LastDealText = data.LastDealDate is null
            ? "لا يوجد تعامل سابق"
            : $"{data.LastDealDate:yyyy/MM/dd} — {data.LastDealDescription} ({data.LastDealAmount:N2} {lastSuffix})";

        Products.Clear();
        foreach (var p in data.Products)
            Products.Add(p);

        RecentTimeline.Clear();
        foreach (var t in data.RecentTimeline)
            RecentTimeline.Add(t);

        HasData = true;
        IsBusy = false;
        ErrorMessage = null;
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
        IsBusy = false;
        HasData = false;
        Name = "تعذر التحميل";
    }
}
