using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class RestaurantPosViewModel : ViewModelBase
{
    private readonly IRestaurantMenuService _menuService;
    private readonly IRestaurantOrderService _orderService;
    private readonly IRestaurantTableService _tableService;
    private readonly IHotelCashService _cashService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    public ObservableCollection<RestaurantMenuCategory> Categories { get; } = [];
    public ObservableCollection<RestaurantMenuItem> MenuItems { get; } = [];
    public ObservableCollection<RestaurantOrderLine> CartLines { get; } = [];
    public ObservableCollection<RestaurantTable> Tables { get; } = [];
    public ObservableCollection<ActiveRoomForService> ActiveRooms { get; } = [];
    public ObservableCollection<HotelCashBox> CashBoxes { get; } = [];

    public ObservableCollection<RestaurantMenuItem> FavoriteItems { get; } = [];

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private RestaurantMenuCategory? _selectedCategory;
    [ObservableProperty] private RestaurantOrderType _selectedOrderType = RestaurantOrderType.DineIn;
    [ObservableProperty] private RestaurantTable? _selectedTable;
    [ObservableProperty] private ActiveRoomForService? _selectedRoom;
    [ObservableProperty] private RestaurantOrder? _currentOrder;
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private string _statusMessage = "اختر نوع الطلب وابدأ";
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private int? _paymentCashBoxId;
    [ObservableProperty] private RestaurantPaymentMethod _paymentMethod = RestaurantPaymentMethod.Cash;
    [ObservableProperty] private string _searchText = string.Empty;

    public int CartLineCount => CartLines.Count;
    public string? CurrentOrderNumber => CurrentOrder?.OrderNumber;
    public bool IsDineIn => SelectedOrderType == RestaurantOrderType.DineIn;
    public bool IsRoomService => SelectedOrderType == RestaurantOrderType.RoomService;
    public bool IsTakeaway => SelectedOrderType == RestaurantOrderType.Takeaway;
    public bool HasActiveOrder => CurrentOrder is not null;
    public bool HasMenuItems => MenuItems.Count > 0;
    public bool IsCartEmpty => CartLines.Count == 0;
    public bool ShowReadOnlyBanner => !CanAdd;
    public int OccupiedTablesCount => Tables.Count(t => t.Status == RestaurantTableStatus.Occupied);
    public decimal ChangeDue => PaymentAmount > GrandTotal ? PaymentAmount - GrandTotal : 0;
    public bool ShowCashPaymentFields => !IsRoomService;

    public IReadOnlyList<RestaurantOrderType> OrderTypeOptions { get; } = Enum.GetValues<RestaurantOrderType>().ToList();
    public IReadOnlyList<RestaurantPaymentMethod> PaymentMethodOptions { get; } = Enum.GetValues<RestaurantPaymentMethod>().ToList();

    public RestaurantPosViewModel(
        IRestaurantMenuService menuService,
        IRestaurantOrderService orderService,
        IRestaurantTableService tableService,
        IHotelCashService cashService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _menuService = menuService;
        _orderService = orderService;
        _tableService = tableService;
        _cashService = cashService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "كاشير المطعم";
    }

    public override async Task InitializeAsync()
    {
        IsLoaded = false;
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RestaurantPos);
        if (!CanAdd)
            StatusMessage = "ليس لديك صلاحية إنشاء طلبات — وضع العرض فقط";
        try
        {
            await _menuService.EnsureSeedDataAsync();
            await LoadLookupsAsync();
            await LoadCategoriesAsync();
            await LoadFavoriteItemsAsync();
        }
        finally
        {
            IsLoaded = true;
        }
    }

    partial void OnPaymentAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(ChangeDue));
    }

    partial void OnGrandTotalChanged(decimal value)
    {
        OnPropertyChanged(nameof(ChangeDue));
    }

    partial void OnDiscountAmountChanged(decimal value) => _ = ApplyDiscountAsync();

    [RelayCommand]
    private async Task ApplyDiscountAsync()
    {
        if (CurrentOrder is null) return;
        try
        {
            await _orderService.SetOrderDiscountAsync(CurrentOrder.Id, DiscountAmount);
            await RefreshCartAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task AddItemFromSearchAsync()
    {
        var term = SearchText?.Trim();
        if (string.IsNullOrWhiteSpace(term))
            return;

        var items = await _menuService.GetMenuItemsAsync(null);
        var match = items.FirstOrDefault(i =>
                       !string.IsNullOrWhiteSpace(i.Barcode)
                       && string.Equals(i.Barcode, term, StringComparison.OrdinalIgnoreCase))
                   ?? items.FirstOrDefault(i =>
                       i.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            _toast.ShowWarning("لم يُعثر على الصنف");
            return;
        }

        await AddItemAsync(match);
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void SelectTable(RestaurantTable? table)
    {
        if (table is null || table.Status == RestaurantTableStatus.Occupied)
            return;
        SelectedTable = table;
    }

    private async Task LoadFavoriteItemsAsync()
    {
        FavoriteItems.Clear();
        var items = await _menuService.GetMenuItemsAsync(null);
        foreach (var item in items.Where(i => i.IsActive).Take(6))
            FavoriteItems.Add(item);
    }

    partial void OnSelectedCategoryChanged(RestaurantMenuCategory? value) => _ = LoadMenuItemsAsync();
    partial void OnSearchTextChanged(string value) => _ = LoadMenuItemsAsync();

    partial void OnSelectedOrderTypeChanged(RestaurantOrderType value)
    {
        OnPropertyChanged(nameof(IsDineIn));
        OnPropertyChanged(nameof(IsTakeaway));
        OnPropertyChanged(nameof(IsRoomService));
        OnPropertyChanged(nameof(ShowCashPaymentFields));
    }

    partial void OnCurrentOrderChanged(RestaurantOrder? value)
    {
        OnPropertyChanged(nameof(CurrentOrderNumber));
        OnPropertyChanged(nameof(HasActiveOrder));
    }

    [RelayCommand]
    private async Task LoadLookupsAsync()
    {
        Tables.Clear();
        foreach (var t in await _tableService.GetTablesAsync())
            Tables.Add(t);

        ActiveRooms.Clear();
        foreach (var r in await _orderService.GetActiveRoomsForServiceAsync())
            ActiveRooms.Add(r);

        CashBoxes.Clear();
        foreach (var c in await _cashService.GetCashBoxesAsync())
            CashBoxes.Add(c);

        PaymentCashBoxId ??= CashBoxes.FirstOrDefault()?.Id;
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        Categories.Clear();
        foreach (var c in await _menuService.GetCategoriesAsync())
            Categories.Add(c);
        SelectedCategory = Categories.FirstOrDefault();
    }

    [RelayCommand]
    private async Task LoadMenuItemsAsync()
    {
        MenuItems.Clear();
        var items = await _menuService.GetMenuItemsAsync(SelectedCategory?.Id);
        foreach (var item in items.Where(i => string.IsNullOrWhiteSpace(SearchText)
            || i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || (i.Barcode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)))
            MenuItems.Add(item);

        OnPropertyChanged(nameof(HasMenuItems));
    }

    [RelayCommand]
    private async Task StartNewOrderAsync()
    {
        if (!CanAdd) return;

        try
        {
            int? tableId = SelectedOrderType == RestaurantOrderType.DineIn ? SelectedTable?.Id : null;
            int? reservationId = SelectedOrderType == RestaurantOrderType.RoomService ? SelectedRoom?.ReservationId : null;
            int? roomId = SelectedOrderType == RestaurantOrderType.RoomService ? SelectedRoom?.RoomId : null;

            if (SelectedOrderType == RestaurantOrderType.DineIn && tableId is null)
            {
                _toast.ShowWarning("اختر طاولة للصالة");
                return;
            }

            if (SelectedOrderType == RestaurantOrderType.RoomService && reservationId is null)
            {
                _toast.ShowWarning("اختر غرفة لخدمة الغرف");
                return;
            }

            CurrentOrder = await _orderService.CreateOrderAsync(SelectedOrderType, tableId, reservationId, roomId);
            CartLines.Clear();
            RecalculateTotals();
            StatusMessage = $"طلب جديد: {CurrentOrder.OrderNumber}";
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task AddItemAsync(RestaurantMenuItem? item)
    {
        if (item is null || !CanAdd) return;

        if (CurrentOrder is null)
        {
            await StartNewOrderAsync();
            if (CurrentOrder is null) return;
        }

        try
        {
            await _orderService.AddLineAsync(CurrentOrder.Id, item.Id, 1);
            await RefreshCartAsync();
            StatusMessage = $"أُضيف: {item.Name}";
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task IncreaseLineAsync(RestaurantOrderLine? line)
    {
        if (line is null || CurrentOrder is null) return;
        await _orderService.UpdateLineQuantityAsync(line.Id, line.Quantity + 1);
        await RefreshCartAsync();
    }

    [RelayCommand]
    private async Task DecreaseLineAsync(RestaurantOrderLine? line)
    {
        if (line is null || CurrentOrder is null) return;
        await _orderService.UpdateLineQuantityAsync(line.Id, line.Quantity - 1);
        await RefreshCartAsync();
    }

    [RelayCommand]
    private async Task RemoveLineAsync(RestaurantOrderLine? line)
    {
        if (line is null || CurrentOrder is null) return;
        try
        {
            await _orderService.RemoveLineAsync(line.Id);
            await RefreshCartAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void SelectOrderType(RestaurantOrderType orderType) => SelectedOrderType = orderType;

    [RelayCommand]
    private void ClosePaymentDialog() => IsPaymentDialogOpen = false;

    [RelayCommand]
    private void OpenPaymentDialog()
    {
        if (CurrentOrder is null || CartLines.Count == 0)
        {
            _toast.ShowWarning("لا يوجد طلب للدفع");
            return;
        }

        PaymentAmount = GrandTotal;
        if (SelectedOrderType == RestaurantOrderType.RoomService)
            PaymentMethod = RestaurantPaymentMethod.RoomCharge;
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (CurrentOrder is null) return;

        try
        {
            var payments = new List<RestaurantPaymentRequest>();
            if (SelectedOrderType == RestaurantOrderType.RoomService)
            {
                payments.Add(new RestaurantPaymentRequest
                {
                    Amount = GrandTotal,
                    PaymentMethod = RestaurantPaymentMethod.RoomCharge
                });
            }
            else
            {
                payments.Add(new RestaurantPaymentRequest
                {
                    Amount = PaymentAmount,
                    PaymentMethod = PaymentMethod,
                    HotelCashBoxId = PaymentCashBoxId
                });
            }

            await _orderService.CompleteAndPayAsync(CurrentOrder.Id, payments);
            _toast.ShowSuccess($"تم إغلاق الطلب {CurrentOrder.OrderNumber}");
            IsPaymentDialogOpen = false;
            CurrentOrder = null;
            CartLines.Clear();
            RecalculateTotals();
            StatusMessage = "جاهز لطلب جديد";
            await LoadLookupsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task CancelCurrentOrderAsync()
    {
        if (CurrentOrder is null) return;
        try
        {
            await _orderService.CancelOrderAsync(CurrentOrder.Id);
            CurrentOrder = null;
            CartLines.Clear();
            RecalculateTotals();
            StatusMessage = "تم إلغاء الطلب";
            await LoadLookupsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    private async Task RefreshCartAsync()
    {
        if (CurrentOrder is null) return;
        var order = await _orderService.GetOrderByIdAsync(CurrentOrder.Id);
        if (order is null) return;
        CurrentOrder = order;
        CartLines.Clear();
        foreach (var line in order.Lines)
            CartLines.Add(line);
        SubTotal = order.SubTotal;
        DiscountAmount = order.DiscountAmount;
        GrandTotal = order.TotalAmount;
        OnPropertyChanged(nameof(CartLineCount));
        OnPropertyChanged(nameof(IsCartEmpty));
    }

    private void RecalculateTotals()
    {
        SubTotal = CartLines.Sum(l => l.LineTotal);
        GrandTotal = SubTotal - DiscountAmount;
        OnPropertyChanged(nameof(CartLineCount));
        OnPropertyChanged(nameof(IsCartEmpty));
    }
}
