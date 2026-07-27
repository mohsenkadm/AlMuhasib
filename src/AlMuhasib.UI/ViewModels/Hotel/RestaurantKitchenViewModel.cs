using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class RestaurantKitchenViewModel : ViewModelBase, IDisposable
{
    private readonly IRestaurantOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly DispatcherTimer _autoRefreshTimer;
    private bool _disposed;

    public ObservableCollection<RestaurantOrder> KitchenOrders { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private RestaurantOrder? _selectedOrder;
    [ObservableProperty] private bool _autoRefreshEnabled = true;
    [ObservableProperty] private string _lastRefreshedText = string.Empty;

    public IReadOnlyList<RestaurantKitchenStatus> KitchenStatusOptions { get; } =
        Enum.GetValues<RestaurantKitchenStatus>().ToList();

    public bool HasSelectedOrder => SelectedOrder is not null;

    public RestaurantKitchenViewModel(
        IRestaurantOrderService orderService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "شاشة المطبخ";

        _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _autoRefreshTimer.Tick += OnAutoRefreshTick;
    }

    private async void OnAutoRefreshTick(object? sender, EventArgs e)
    {
        if (AutoRefreshEnabled && !IsBusy)
            await LoadOrdersAsync();
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RestaurantKitchen);
        await LoadOrdersAsync();
        if (AutoRefreshEnabled)
            _autoRefreshTimer.Start();
    }

    partial void OnSelectedOrderChanged(RestaurantOrder? value) => OnPropertyChanged(nameof(HasSelectedOrder));

    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        if (value) _autoRefreshTimer.Start();
        else _autoRefreshTimer.Stop();
    }

    [RelayCommand]
    private void SelectOrder(RestaurantOrder? order) => SelectedOrder = order;

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        IsBusy = true;
        try
        {
            KitchenOrders.Clear();
            var orders = await _orderService.GetKitchenOrdersAsync();
            foreach (var o in orders)
                KitchenOrders.Add(o);

            var selectedId = SelectedOrder?.Id;
            SelectedOrder = selectedId.HasValue
                ? orders.FirstOrDefault(o => o.Id == selectedId.Value)
                : orders.FirstOrDefault();

            UpdateStats(orders);
            LastRefreshedText = $"آخر تحديث: {DateTime.Now:HH:mm:ss}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateStats(IReadOnlyList<RestaurantOrder> orders)
    {
        Stats.Clear();
        Stats.Add(new HotelListStatItem { Label = "انتظار", Value = orders.Count(o => o.KitchenStatus == RestaurantKitchenStatus.Pending).ToString("N0"), AccentColor = "#F57C00" });
        Stats.Add(new HotelListStatItem { Label = "قيد التحضير", Value = orders.Count(o => o.KitchenStatus == RestaurantKitchenStatus.Preparing).ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "جاهز", Value = orders.Count(o => o.KitchenStatus == RestaurantKitchenStatus.Ready).ToString("N0"), AccentColor = "#2E7D32" });
        Stats.Add(new HotelListStatItem { Label = "الإجمالي", Value = orders.Count.ToString("N0"), AccentColor = "#00897B" });
    }

    [RelayCommand]
    private async Task UpdateStatusAsync(RestaurantKitchenStatus status)
    {
        if (SelectedOrder is null) return;
        try
        {
            await _orderService.UpdateKitchenStatusAsync(SelectedOrder.Id, status);
            await LoadOrdersAsync();
            _toast.ShowSuccess("تم تحديث حالة الطلب");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task MarkPreparingAsync() => await UpdateStatusAsync(RestaurantKitchenStatus.Preparing);

    [RelayCommand]
    private async Task MarkReadyAsync() => await UpdateStatusAsync(RestaurantKitchenStatus.Ready);

    [RelayCommand]
    private async Task MarkServedAsync() => await UpdateStatusAsync(RestaurantKitchenStatus.Served);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _autoRefreshTimer.Stop();
        _autoRefreshTimer.Tick -= OnAutoRefreshTick;
    }
}
