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
    private readonly IRestaurantReportService _reportService;
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
    public ObservableCollection<RestaurantOrder> OpenOrders { get; } = [];

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
    [ObservableProperty] private decimal _lastChangeDue;

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
    public int OpenOrdersCount => OpenOrders.Count;
    public decimal ChangeDue =>
        PaymentMethod == RestaurantPaymentMethod.Cash && PaymentAmount > GrandTotal
            ? PaymentAmount - GrandTotal
            : 0;
    public bool ShowCashPaymentFields => !IsRoomService;
    public bool ShowChangeDue => PaymentMethod == RestaurantPaymentMethod.Cash && ChangeDue > 0;
    public bool IsCashPayment => PaymentMethod == RestaurantPaymentMethod.Cash;

    public IReadOnlyList<RestaurantOrderType> OrderTypeOptions { get; } = Enum.GetValues<RestaurantOrderType>().ToList();
    public IReadOnlyList<RestaurantPaymentMethod> PaymentMethodOptions { get; } =
        Enum.GetValues<RestaurantPaymentMethod>()
            .Where(m => m is RestaurantPaymentMethod.Cash or RestaurantPaymentMethod.Card)
            .ToList();

    public RestaurantPosViewModel(
        IRestaurantMenuService menuService,
        IRestaurantOrderService orderService,
        IRestaurantTableService tableService,
        IRestaurantReportService reportService,
        IHotelCashService cashService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _menuService = menuService;
        _orderService = orderService;
        _tableService = tableService;
        _reportService = reportService;
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
            await LoadOpenOrdersAsync();
        }
        finally
        {
            IsLoaded = true;
        }
    }

    partial void OnPaymentAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(ShowChangeDue));
    }

    partial void OnGrandTotalChanged(decimal value)
    {
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(ShowChangeDue));
    }

    partial void OnPaymentMethodChanged(RestaurantPaymentMethod value)
    {
        OnPropertyChanged(nameof(IsCashPayment));
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(ShowChangeDue));
        if (value != RestaurantPaymentMethod.Cash)
            PaymentAmount = GrandTotal;
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
    private async Task SelectTableAsync(RestaurantTable? table)
    {
        if (table is null) return;

        SelectedTable = table;

        if (table.Status == RestaurantTableStatus.Occupied)
        {
            var open = await _orderService.GetOpenOrderByTableAsync(table.Id);
            if (open is not null)
            {
                await ResumeOrderAsync(open);
                return;
            }
        }

        StatusMessage = table.Status == RestaurantTableStatus.Occupied
            ? $"طاولة {table.TableNumber} مشغولة"
            : $"طاولة {table.TableNumber} — جاهز لطلب جديد";
    }

    [RelayCommand]
    private async Task ResumeOrderAsync(RestaurantOrder? order)
    {
        if (order is null) return;

        var full = await _orderService.GetOrderByIdAsync(order.Id) ?? order;
        CurrentOrder = full;
        SelectedOrderType = full.OrderType;
        if (full.RestaurantTableId.HasValue)
            SelectedTable = Tables.FirstOrDefault(t => t.Id == full.RestaurantTableId.Value);

        CartLines.Clear();
        foreach (var line in full.Lines)
            CartLines.Add(line);

        SubTotal = full.SubTotal;
        DiscountAmount = full.DiscountAmount;
        GrandTotal = full.TotalAmount;
        OnPropertyChanged(nameof(CartLineCount));
        OnPropertyChanged(nameof(IsCartEmpty));
        StatusMessage = $"استئناف الطلب {full.OrderNumber}";
    }

    private async Task LoadFavoriteItemsAsync()
    {
        FavoriteItems.Clear();
        try
        {
            var top = await _reportService.GetTopSellingItemsAsync(DateTime.Today.AddDays(-30), DateTime.Today, 6);
            var allItems = await _menuService.GetMenuItemsAsync(null);
            foreach (var topItem in top)
            {
                var match = allItems.FirstOrDefault(i => i.IsActive
                    && string.Equals(i.Name, topItem.ItemName, StringComparison.OrdinalIgnoreCase));
                if (match is not null && FavoriteItems.All(f => f.Id != match.Id))
                    FavoriteItems.Add(match);
            }

            if (FavoriteItems.Count < 6)
            {
                foreach (var item in allItems.Where(i => i.IsActive).Take(6))
                {
                    if (FavoriteItems.All(f => f.Id != item.Id))
                        FavoriteItems.Add(item);
                    if (FavoriteItems.Count >= 6) break;
                }
            }
        }
        catch
        {
            var items = await _menuService.GetMenuItemsAsync(null);
            foreach (var item in items.Where(i => i.IsActive).Take(6))
                FavoriteItems.Add(item);
        }
    }

    partial void OnSelectedCategoryChanged(RestaurantMenuCategory? value) => _ = LoadMenuItemsAsync();
    partial void OnSearchTextChanged(string value) => _ = LoadMenuItemsAsync();

    partial void OnSelectedOrderTypeChanged(RestaurantOrderType value)
    {
        OnPropertyChanged(nameof(IsDineIn));
        OnPropertyChanged(nameof(IsTakeaway));
        OnPropertyChanged(nameof(IsRoomService));
        OnPropertyChanged(nameof(ShowCashPaymentFields));
        if (value != RestaurantOrderType.DineIn)
            SelectedTable = null;
        if (value != RestaurantOrderType.RoomService)
            SelectedRoom = null;
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
        OnPropertyChanged(nameof(OccupiedTablesCount));
    }

    [RelayCommand]
    private async Task LoadOpenOrdersAsync()
    {
        OpenOrders.Clear();
        foreach (var o in await _orderService.GetOpenOrdersAsync())
            OpenOrders.Add(o);
        OnPropertyChanged(nameof(OpenOrdersCount));
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

            if (SelectedOrderType == RestaurantOrderType.DineIn && tableId.HasValue)
            {
                var existing = await _orderService.GetOpenOrderByTableAsync(tableId.Value);
                if (existing is not null)
                {
                    await ResumeOrderAsync(existing);
                    _toast.ShowInfo("تم استئناف الطلب المفتوح على الطاولة");
                    return;
                }
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
            await LoadLookupsAsync();
            await LoadOpenOrdersAsync();
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
        else if (PaymentMethod is RestaurantPaymentMethod.RoomCharge or RestaurantPaymentMethod.Mixed)
            PaymentMethod = RestaurantPaymentMethod.Cash;

        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (CurrentOrder is null) return;

        try
        {
            if (!IsRoomService)
            {
                if (PaymentMethod == RestaurantPaymentMethod.Cash && PaymentAmount + 0.01m < GrandTotal)
                {
                    _toast.ShowWarning("المبلغ المستلم أقل من الإجمالي");
                    return;
                }

                if (PaymentMethod == RestaurantPaymentMethod.Card && Math.Abs(PaymentAmount - GrandTotal) > 0.01m)
                {
                    PaymentAmount = GrandTotal;
                }

                if (PaymentCashBoxId is null)
                {
                    _toast.ShowWarning("اختر صندوق النقد");
                    return;
                }
            }

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
                    Amount = PaymentMethod == RestaurantPaymentMethod.Cash ? PaymentAmount : GrandTotal,
                    PaymentMethod = PaymentMethod,
                    HotelCashBoxId = PaymentCashBoxId
                });
            }

            var result = await _orderService.CompleteAndPayAsync(CurrentOrder.Id, payments);
            LastChangeDue = result.ChangeDue;
            var changeMsg = result.ChangeDue > 0 ? $" — الباقي {result.ChangeDue:N0} د.ع" : string.Empty;
            _toast.ShowSuccess($"تم إغلاق الطلب {result.Order.OrderNumber}{changeMsg}");
            IsPaymentDialogOpen = false;
            CurrentOrder = null;
            CartLines.Clear();
            RecalculateTotals();
            StatusMessage = result.ChangeDue > 0
                ? $"تم الدفع — أعطِ الباقي {result.ChangeDue:N0} د.ع"
                : "جاهز لطلب جديد";
            await LoadLookupsAsync();
            await LoadOpenOrdersAsync();
            await LoadFavoriteItemsAsync();
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
            await LoadOpenOrdersAsync();
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
