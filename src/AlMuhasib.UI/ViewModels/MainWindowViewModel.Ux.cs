using System.Collections.ObjectModel;
using System.Windows.Threading;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels.Car;
using AlMuhasib.UI.ViewModels.Hotel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    private DispatcherTimer? _globalSearchDebounce;
    private CancellationTokenSource? _globalSearchCts;

    [ObservableProperty]
    private bool _isGlobalSearchOpen;

    [ObservableProperty]
    private string _globalSearchQuery = string.Empty;

    [ObservableProperty]
    private bool _isGlobalSearchBusy;

    [ObservableProperty]
    private bool _showKeyboardShortcutsHelp;

    [ObservableProperty]
    private bool _isRecentActivityOpen;

    [ObservableProperty]
    private bool _hasRecentActivities;

    [ObservableProperty]
    private bool _isMenuCustomizerOpen;

    public ObservableCollection<GlobalSearchResultItem> GlobalSearchResults { get; } = [];

    public ObservableCollection<RecentActivityEntry> RecentActivities { get; } = [];
    public ObservableCollection<MenuVisibilityOption> MenuVisibilityOptions { get; } = [];

    public void ApplyMenuVisibilityFromPreferences() => RefreshMenuVisibility();

    private static bool IsFeatureFlagVisible(NavigationMenuItem item, BusinessFeatureFlags flags)
    {
        if (string.Equals(item.ScreenName, "PurchaseReturn", StringComparison.OrdinalIgnoreCase))
            return flags.PurchaseReturns;

        return item.ViewModelType switch
        {
            var t when t == typeof(WarehouseTransferViewModel) => flags.WarehouseTransfers,
            var t when t == typeof(DriversViewModel) => flags.WarehouseInvoiceAndDriver,
            var t when t == typeof(PackagingTypesViewModel) => flags.UnitsOfMeasure,
            var t when t == typeof(PricingTypesViewModel) => flags.ProductPricingEnabled,
            var t when t == typeof(ProductPricingViewModel) => flags.ProductPricingEnabled,
            _ => true
        };
    }

    [RelayCommand]
    private void ToggleMenuCustomizer()
    {
        IsQuickAssistOpen = false;
        IsSmartAssistantOpen = false;
        IsMenuCustomizerOpen = !IsMenuCustomizerOpen;
        if (IsMenuCustomizerOpen)
            BuildMenuVisibilityOptions();
    }

    [RelayCommand]
    private void SaveMenuCustomization()
    {
        var hidden = MenuVisibilityOptions
            .Where(x => !x.IsVisible)
            .Select(x => x.PreferenceKey)
            .Distinct()
            .ToList();

        var pinned = MenuVisibilityOptions
            .Where(x => x.IsVisible && x.IsPinned)
            .Select(x => x.PreferenceKey)
            .Distinct()
            .Take(MaxPinnedTabs)
            .ToList();

        _userPreferences.Update(p =>
        {
            p.HiddenMenuScreens = hidden;
            p.PinnedMenuScreens = pinned;
        });
        ApplyMenuVisibilityFromPreferences();
        UpdateTabPinStates();
        IsMenuCustomizerOpen = false;
        _toast.ShowSuccess("تم حفظ تخصيص القائمة والتبويبات المثبتة");
    }

    [RelayCommand]
    private void ResetMenuCustomization()
    {
        _userPreferences.Update(p =>
        {
            p.HiddenMenuScreens = [];
            p.PinnedMenuScreens = [];
        });
        ApplyMenuVisibilityFromPreferences();
        BuildMenuVisibilityOptions();
        UpdateTabPinStates();
        _toast.ShowSuccess("تمت استعادة القائمة والتبويبات الافتراضية");
    }

    private void BuildMenuVisibilityOptions()
    {
        MenuVisibilityOptions.Clear();
        foreach (var item in FlattenMenuItems())
        {
            if (!IsCustomizableMenuItem(item))
                continue;
            if (!CanMenuBeShownByPermissions(item))
                continue;

            var key = GetMenuPreferenceKey(item);
            MenuVisibilityOptions.Add(new MenuVisibilityOption
            {
                MenuItem = item,
                PreferenceKey = key,
                Title = item.Title,
                Icon = item.Icon,
                IsVisible = item.IsVisible,
                IsPinned = _userPreferences.Current.PinnedMenuScreens.Contains(key)
            });
        }
    }

    private static bool IsCustomizableMenuItem(NavigationMenuItem item) =>
        !item.IsGroupHeader
        && item.ViewModelType is not null
        && item.ViewModelType != typeof(DeveloperSystemSwitchViewModel)
        && item.ScreenName != "Dashboard";

    private static string GetMenuPreferenceKey(NavigationMenuItem item) =>
        // مرتجع المشتريات يشارك ViewModel مع فاتورة المشتريات — نميّزه بـ ScreenName
        string.Equals(item.ScreenName, "PurchaseReturn", StringComparison.OrdinalIgnoreCase)
            ? item.ScreenName
            : item.ViewModelType?.Name ?? item.ScreenName;

    private bool CanMenuBeShownByPermissions(NavigationMenuItem item) =>
        item.ScreenName == ScreenPermissionRegistry.Dashboard
        || item.ViewModelType == typeof(DeveloperSystemSwitchViewModel)
        || _currentUserService.CanView(item.ScreenName);

    [RelayCommand]
    private void OpenGlobalSearch()
    {
        IsVoiceAssistantOpen = false;
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = false;
        IsNotificationPanelOpen = false;
        IsOpenRecentExcelPanelOpen = false;
        IsGlobalSearchOpen = true;
        _ = RefreshGlobalSearchAsync();
    }

    [RelayCommand]
    private void CloseGlobalSearch()
    {
        IsGlobalSearchOpen = false;
        GlobalSearchQuery = string.Empty;
        GlobalSearchResults.Clear();
    }

    partial void OnGlobalSearchQueryChanged(string value)
    {
        _globalSearchDebounce ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(280) };
        _globalSearchDebounce.Stop();
        _globalSearchDebounce.Tick -= GlobalSearchDebounce_Tick;
        _globalSearchDebounce.Tick += GlobalSearchDebounce_Tick;
        _globalSearchDebounce.Start();
    }

    private void GlobalSearchDebounce_Tick(object? sender, EventArgs e)
    {
        _globalSearchDebounce?.Stop();
        _ = RefreshGlobalSearchAsync();
    }

    private async Task RefreshGlobalSearchAsync()
    {
        GlobalSearchResults.Clear();
        var term = GlobalSearchQuery?.Trim() ?? string.Empty;

        foreach (var menu in GetSearchableMenuItems())
        {
            if (!menu.IsVisible || !_currentUserService.CanView(menu.ScreenName))
                continue;
            if (menu.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                GlobalSearchResults.Add(GlobalSearchResultItem.FromMenu(menu));
        }

        if (term.Length < 2)
            return;

        _globalSearchCts?.Cancel();
        _globalSearchCts = new CancellationTokenSource();
        var token = _globalSearchCts.Token;

        try
        {
            IsGlobalSearchBusy = true;
            var hits = await _globalSearchService.SearchAsync(term, 25, token);
            foreach (var hit in hits)
                GlobalSearchResults.Add(GlobalSearchResultItem.FromHit(hit));
        }
        catch (OperationCanceledException) { }
        catch
        {
            // silent — menu results still shown
        }
        finally
        {
            IsGlobalSearchBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectGlobalSearchResult(GlobalSearchResultItem? item)
    {
        if (item is null) return;
        CloseGlobalSearch();

        if (item.MenuItem is not null)
        {
            NavigateMenuItem(item.MenuItem);
            return;
        }

        if (item.EntityHit is null) return;

        if (item.EntityHit.Kind is GlobalSearchKind.Customer or GlobalSearchKind.OverdueCustomer
            && item.EntityHit.EntityId is int customerId)
        {
            await OpenQuickStatementAsync(customerId);
            return;
        }

        if (item.EntityHit.Kind == GlobalSearchKind.Installment)
        {
            await QuickInstallmentsAsync();
            return;
        }

        if (item.EntityHit.EntityId is int hotelEntityId)
        {
            switch (item.EntityHit.Kind)
            {
                case GlobalSearchKind.HotelGuest:
                    HotelNavigationBridge.PendingGuestId = hotelEntityId;
                    break;
                case GlobalSearchKind.HotelRoom:
                    HotelNavigationBridge.PendingRoomId = hotelEntityId;
                    break;
                case GlobalSearchKind.HotelReservation:
                    HotelNavigationBridge.PendingReservationId = hotelEntityId;
                    break;
            }
        }

        var screen = item.EntityHit.ScreenName ?? string.Empty;
        var menu = FlattenMenuItems().FirstOrDefault(m => m.ScreenName == screen);
        if (menu is not null)
            NavigateMenuItem(menu);
        else
            await NavigateByScreenNameAsync(screen);
    }

    private async Task NavigateByScreenNameAsync(string screenName)
    {
        if (!_currentUserService.CanView(screenName))
        {
            _toast.ShowWarning($"ليس لديك صلاحية للوصول إلى: {ScreenPermissionRegistry.GetLabel(screenName)}");
            return;
        }

        var type = ScreenPermissionRegistry.GetDefaultViewModelType(screenName);
        if (type is null) return;

        var menu = FlattenMenuItems().FirstOrDefault(m => m.ScreenName == screenName && m.ViewModelType == type)
                   ?? FlattenMenuItems().FirstOrDefault(m => m.ScreenName == screenName);
        var title = menu?.Title ?? ScreenPermissionRegistry.GetLabel(screenName);
        var icon = menu?.Icon ?? PackIconKind.Application;
        await OpenTabAsync(type, title, icon);
    }

    [RelayCommand]
    private async Task QuickNewCarContractAsync() =>
        await OpenTabAsync(typeof(CarContractFormViewModel), "عقد جديد", PackIconKind.FileDocumentPlus, activateIfExists: false);

    [RelayCommand]
    private async Task QuickCarContractsListAsync() =>
        await OpenTabAsync(typeof(CarContractsViewModel), "العقود", PackIconKind.FormatListBulleted);

    [RelayCommand]
    private async Task QuickNewReservationAsync() =>
        await OpenTabAsync(typeof(HotelReservationFormViewModel), "حجز جديد", PackIconKind.CalendarPlus, activateIfExists: false);

    [RelayCommand]
    private async Task QuickCheckInAsync() =>
        await OpenTabAsync(typeof(HotelCheckInOutViewModel), "تسجيل دخول/خروج", PackIconKind.Login);

    [RelayCommand]
    private async Task QuickNewSaleAsync() =>
        await OpenTabAsync(typeof(SalesInvoiceViewModel), "فاتورة مبيعات", PackIconKind.CashRegister);

    [RelayCommand]
    private async Task QuickPosSaleAsync() =>
        await OpenTabAsync(typeof(PosQuickSaleViewModel), "بيع سريع (POS)", PackIconKind.PointOfSale);

    [RelayCommand]
    private async Task QuickSalesReturnAsync()
    {
        await OpenTabAsync(typeof(SalesReportViewModel), "تقرير المبيعات", PackIconKind.ChartLine);
        _toast.ShowInfo("اختر الفاتورة من الجدول واضغط زر «مرتجع» لإنشاء فاتورة مرتجع");
    }

    [RelayCommand]
    private async Task QuickNewPurchaseAsync() =>
        await OpenTabAsync(typeof(PurchaseInvoiceViewModel), "فاتورة مشتريات", PackIconKind.CartArrowDown);

    [RelayCommand]
    private async Task QuickReceiptVoucherAsync() =>
        await OpenVouchersAsync(VoucherType.Receipt);

    [RelayCommand]
    private async Task QuickPaymentVoucherAsync() =>
        await OpenVouchersAsync(VoucherType.Payment);

    [RelayCommand]
    private async Task QuickInstallmentsAsync() =>
        await OpenTabAsync(typeof(InstallmentsViewModel), "الأقساط", PackIconKind.CalendarClock);

    [RelayCommand]
    private async Task QuickInstallmentInvoiceAsync() =>
        await OpenTabAsync(typeof(InstallmentInvoiceViewModel), "فاتورة أقساط", PackIconKind.CalendarClock);

    private async Task OpenVouchersAsync(VoucherType type)
    {
        VouchersViewModel.PendingInitialType = type;
        await OpenTabAsync(typeof(VouchersViewModel), "السندات", PackIconKind.FileDocument);
    }

    [ObservableProperty]
    private bool _isSoundEnabled = true;

    [RelayCommand]
    private void ToggleSound()
    {
        IsSoundEnabled = !IsSoundEnabled;
        _sound.SetEnabled(IsSoundEnabled);

        if (IsSoundEnabled)
            _toast.ShowSuccess("تم تفعيل الأصوات");
        else
            _toast.ShowInfo("تم إيقاف الأصوات");
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        _toast.ShowSuccess(_userPreferences.Current.IsDarkTheme ? "تم تفعيل الوضع الليلي" : "تم تفعيل الوضع النهاري");
    }

    [RelayCommand]
    private void IncreaseFontSize() => _themeService.SetFontScale(_userPreferences.Current.FontScale + 0.05);

    [RelayCommand]
    private void DecreaseFontSize() => _themeService.SetFontScale(_userPreferences.Current.FontScale - 0.05);

    [RelayCommand]
    private void ToggleKeyboardShortcutsHelp() =>
        ShowKeyboardShortcutsHelp = !ShowKeyboardShortcutsHelp;

    [RelayCommand]
    private async Task ToggleRecentActivityAsync()
    {
        IsRecentActivityOpen = !IsRecentActivityOpen;
        if (IsRecentActivityOpen)
            await RefreshRecentActivitiesAsync();
    }

    public async Task RefreshRecentActivitiesAsync()
    {
        if (_recentActivity.Count == 0)
            await SeedRecentActivitiesFromAuditAsync();

        RecentActivities.Clear();
        foreach (var entry in _recentActivity.GetRecent(20))
            RecentActivities.Add(entry);

        HasRecentActivities = RecentActivities.Count > 0;
    }

    private async Task SeedRecentActivitiesFromAuditAsync()
    {
        try
        {
            var result = await _auditLogService.QueryAsync(page: 1, pageSize: 25);
            var entries = result.Rows
                .OrderBy(r => r.Timestamp)
                .Select(r => new RecentActivityEntry(
                    r.Timestamp,
                    $"{r.ActionDisplay} — {r.EntityName}",
                    $"المعرف {r.EntityId} · {r.Username}",
                    r.EntityName,
                    null));

            _recentActivity.SeedIfEmpty(entries);
        }
        catch
        {
            // لا تعطل الواجهة إذا تعذّر تحميل السجل
        }
    }

    public void RecordActivity(string title, string detail, string screenName, Type? viewModelType = null) =>
        _recentActivity.Record(title, detail, screenName, viewModelType);

    public async Task ExecuteDailyTaskAsync(SmartAlertAction action)
    {
        switch (action)
        {
            case SmartAlertAction.OpenInstallments:
                await QuickInstallmentsAsync();
                break;
            case SmartAlertAction.OpenCollectionDashboard:
                await OpenTabAsync(typeof(CollectionDashboardViewModel), "لوحة التحصيل", PackIconKind.CashMultiple);
                break;
            case SmartAlertAction.OpenOverdueReport:
                await OpenTabAsync(typeof(OverdueReportViewModel), "الأقساط المتأخرة", PackIconKind.ClockAlert);
                break;
            case SmartAlertAction.OpenUnpaidSales:
                await OpenTabAsync(typeof(SalesReportViewModel), "تقرير المبيعات", PackIconKind.ChartLine);
                break;
            case SmartAlertAction.OpenUnpaidPurchases:
                await OpenTabAsync(typeof(PurchasesReportViewModel), "تقرير المشتريات", PackIconKind.ChartBar);
                break;
            case SmartAlertAction.OpenProducts:
                await OpenTabAsync(typeof(ProductsViewModel), "المنتجات", PackIconKind.PackageVariantClosed);
                break;
            case SmartAlertAction.OpenWarehouseReport:
                await OpenTabAsync(typeof(WarehouseReportViewModel), "تقرير المخازن", PackIconKind.Warehouse);
                break;
            case SmartAlertAction.OpenStockHealthReport:
                await OpenTabAsync(typeof(StockHealthReportViewModel), "صحة المخزون", PackIconKind.PackageVariant);
                break;
            case SmartAlertAction.OpenExpiryReport:
                await OpenTabAsync(typeof(ExpiryReportViewModel), "تقرير الصلاحية", PackIconKind.CalendarClock);
                break;
            case SmartAlertAction.OpenVouchers:
                await OpenVouchersAsync(VoucherType.Receipt);
                break;
            case SmartAlertAction.OpenSalesInvoiceQueue:
                await OpenInvoiceQueueAsync(InvoiceQueueKind.Sales);
                break;
            case SmartAlertAction.OpenPurchaseInvoiceQueue:
                await OpenInvoiceQueueAsync(InvoiceQueueKind.Purchase);
                break;
            case SmartAlertAction.OpenInstallmentInvoiceQueue:
                await OpenInvoiceQueueAsync(InvoiceQueueKind.Installment);
                break;
            case SmartAlertAction.OpenHotelCheckInOut:
                await OpenTabAsync(typeof(HotelCheckInOutViewModel), "تسجيل دخول/خروج", PackIconKind.Login);
                break;
            case SmartAlertAction.OpenHotelRooms:
                await OpenTabAsync(typeof(HotelRoomsViewModel), "الغرف", PackIconKind.Door);
                break;
            case SmartAlertAction.OpenHotelHousekeeping:
                await OpenTabAsync(typeof(HotelHousekeepingViewModel), "النظافة", PackIconKind.Broom);
                break;
        }
    }

    private async Task OpenInvoiceQueueAsync(InvoiceQueueKind kind)
    {
        Type vmType = kind switch
        {
            InvoiceQueueKind.Sales => typeof(SalesInvoiceViewModel),
            InvoiceQueueKind.Purchase => typeof(PurchaseInvoiceViewModel),
            InvoiceQueueKind.Installment => typeof(InstallmentInvoiceViewModel),
            _ => typeof(SalesInvoiceViewModel)
        };

        string title = kind switch
        {
            InvoiceQueueKind.Sales => "فاتورة مبيعات",
            InvoiceQueueKind.Purchase => "فاتورة مشتريات",
            InvoiceQueueKind.Installment => "فاتورة أقساط",
            _ => "فاتورة"
        };

        PackIconKind icon = kind switch
        {
            InvoiceQueueKind.Sales => PackIconKind.CashRegister,
            InvoiceQueueKind.Purchase => PackIconKind.CartArrowDown,
            InvoiceQueueKind.Installment => PackIconKind.CalendarClock,
            _ => PackIconKind.FileDocumentOutline
        };

        var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == vmType);
        if (existing is null)
        {
            InvoiceNavigationBridge.PendingOpenQueueKind = kind;
            await OpenTabAsync(vmType, title, icon);
            return;
        }

        ActivateTab(existing);
        switch (existing.ViewModel)
        {
            case SalesInvoiceViewModel sales:
                sales.OpenQueuePickerFromExternal();
                break;
            case PurchaseInvoiceViewModel purchase:
                purchase.OpenQueuePickerFromExternal();
                break;
            case InstallmentInvoiceViewModel installment:
                installment.OpenQueuePickerFromExternal();
                break;
        }
    }
}
