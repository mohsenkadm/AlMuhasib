using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public const int MaxOpenTabs = 8;
    public const int MaxPinnedTabs = 6;

    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly CurrentUserService _currentUserService;
    private readonly IAuthService _authService;
    private readonly IBackupService _backupService;
    private readonly IInvestorRefreshService _investorRefresh;
    private readonly IToastNotificationService _toast;
    private readonly IGlobalSearchService _globalSearchService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly ThemeService _themeService;
    private readonly IRecentActivityService _recentActivity;
    private readonly IAuditLogService _auditLogService;
    private bool _investorsLookupDirty;

    /// <summary>
    /// Raised when the user requests logout. App subscribes to restart the login flow.
    /// </summary>
    public event Action? LogoutRequested;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private string _pageTitle = "لوحة التحكم";

    [ObservableProperty]
    private string _currentGregorianDate = string.Empty;

    [ObservableProperty]
    private string _currentHijriDate = string.Empty;

    [ObservableProperty]
    private string _currentTime = string.Empty;

    [ObservableProperty]
    private string _loggedInUsername = "المسؤول";

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private NavigationMenuItem? _selectedMenuItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearchOpen;

    [ObservableProperty]
    private bool _hasSearchResults;

    [ObservableProperty]
    private bool _isQuickAssistOpen;

    [ObservableProperty]
    private DocumentTab? _selectedTab;

    [ObservableProperty]
    private int _tabContentGeneration;

    public ObservableCollection<DocumentTab> OpenTabs { get; } = [];
    public ObservableCollection<NavigationMenuItem> SearchResults { get; } = [];

    // ── Exit Dialog ──
    [ObservableProperty]
    private bool _isExitDialogOpen;

    [ObservableProperty]
    private bool _isExitBackupInProgress;

    [ObservableProperty]
    private string _exitBackupStatus = string.Empty;

    [ObservableProperty]
    private string _exitBackupPath = string.Empty;

    public bool IsExitConfirmed { get; set; }

    public ObservableCollection<NavigationMenuItem> MenuItems { get; } = [];

    public MainWindowViewModel(INavigationService navigationService, IServiceProvider serviceProvider,
        CurrentUserService currentUserService, IAuthService authService,
        IBackupService backupService, IInvestorRefreshService investorRefresh,
        IToastNotificationService toast,
        IGlobalSearchService globalSearchService,
        IUserPreferencesService userPreferences,
        ThemeService themeService,
        IRecentActivityService recentActivity,
        IAuditLogService auditLogService,
        ISmartAlertService smartAlertService)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _currentUserService = currentUserService;
        _authService = authService;
        _backupService = backupService;
        _investorRefresh = investorRefresh;
        _toast = toast;
        _globalSearchService = globalSearchService;
        _userPreferences = userPreferences;
        _themeService = themeService;
        _recentActivity = recentActivity;
        _auditLogService = auditLogService;
        _smartAlertService = smartAlertService;

        _investorRefresh.InvestorsChanged += (_, _) => _investorsLookupDirty = true;

        InitializeMenu();
        _themeService.ApplyFromPreferences();
        ApplyMenuVisibilityFromPreferences();
        LoadWorkspaceProfile();
        UpdateDateTime();
        StartClock();
        _ = RefreshRecentActivitiesAsync();

        InvoiceNavigationBridge.CopyToSalesInvoiceAsync = CopyToSalesInvoiceAsync;
        InvoiceNavigationBridge.CopyToPurchaseInvoiceAsync = CopyToPurchaseInvoiceAsync;
        InvoiceNavigationBridge.ReturnSalesInvoiceAsync = ReturnSalesInvoiceAsync;
    }

    private async Task CopyToSalesInvoiceAsync(int invoiceId)
    {
        var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == typeof(SalesInvoiceViewModel));
        if (existing?.ViewModel is SalesInvoiceViewModel salesVm)
        {
            ActivateTab(existing);
            await salesVm.CopyFromInvoiceAsync(invoiceId);
            return;
        }

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لفتح فاتورة جديدة.");
            return;
        }

        InvoiceNavigationBridge.PendingSalesCopyInvoiceId = invoiceId;
        await OpenTabAsync(typeof(SalesInvoiceViewModel), "فاتورة مبيعات", PackIconKind.CashRegister, activateIfExists: false);
    }

    private async Task CopyToPurchaseInvoiceAsync(int invoiceId)
    {
        var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == typeof(PurchaseInvoiceViewModel));
        if (existing?.ViewModel is PurchaseInvoiceViewModel purchaseVm)
        {
            ActivateTab(existing);
            await purchaseVm.CopyFromInvoiceAsync(invoiceId);
            return;
        }

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لفتح فاتورة جديدة.");
            return;
        }

        InvoiceNavigationBridge.PendingPurchaseCopyInvoiceId = invoiceId;
        await OpenTabAsync(typeof(PurchaseInvoiceViewModel), "فاتورة مشتريات", PackIconKind.CartArrowDown, activateIfExists: false);
    }

    private async Task ReturnSalesInvoiceAsync(int invoiceId)
    {
        var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == typeof(SalesInvoiceViewModel));
        if (existing?.ViewModel is SalesInvoiceViewModel salesVm)
        {
            ActivateTab(existing);
            await salesVm.LoadAsReturnFromInvoiceAsync(invoiceId);
            return;
        }

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لفتح فاتورة جديدة.");
            return;
        }

        InvoiceNavigationBridge.PendingSalesReturnFromInvoiceId = invoiceId;
        await OpenTabAsync(typeof(SalesInvoiceViewModel), "مرتجع مبيعات", PackIconKind.KeyboardReturn, activateIfExists: false);
    }

    private void InitializeMenu()
    {
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "لوحة التحكم",
            Icon = PackIconKind.ViewDashboard,
            ViewModelType = typeof(DashboardViewModel),
            ScreenName = "Dashboard"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "المنتجات",
            Icon = PackIconKind.PackageVariantClosed,
            ViewModelType = typeof(ProductsViewModel),
            ScreenName = "Products"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "تصنيفات المنتجات",
            Icon = PackIconKind.TagMultiple,
            ViewModelType = typeof(CategoriesViewModel),
            ScreenName = "Categories"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "العملاء",
            Icon = PackIconKind.AccountGroup,
            ViewModelType = typeof(CustomersViewModel),
            ScreenName = "Customers"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "الموردون",
            Icon = PackIconKind.Factory,
            ViewModelType = typeof(SuppliersViewModel),
            ScreenName = "Suppliers"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "فاتورة مشتريات",
            Icon = PackIconKind.CartArrowDown,
            ViewModelType = typeof(PurchaseInvoiceViewModel),
            ScreenName = "PurchaseInvoice"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "فاتورة مبيعات",
            Icon = PackIconKind.CashRegister,
            ViewModelType = typeof(SalesInvoiceViewModel),
            ScreenName = "SaleInvoice"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "بيع سريع (POS)",
            Icon = PackIconKind.PointOfSale,
            ViewModelType = typeof(PosQuickSaleViewModel),
            ScreenName = "SaleInvoice"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "فاتورة أقساط",
            Icon = PackIconKind.CalendarClock,
            ViewModelType = typeof(InstallmentInvoiceViewModel),
            ScreenName = "InstallmentInvoice"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "الأقساط",
            Icon = PackIconKind.CalendarMultipleCheck,
            ViewModelType = typeof(InstallmentsViewModel),
            ScreenName = "Installments"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "أرصدة الأقساط الافتتاحية",
            Icon = PackIconKind.History,
            ViewModelType = typeof(OpeningInstallmentBalanceViewModel),
            ScreenName = "OpeningInstallments"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "السندات",
            Icon = PackIconKind.FileDocumentOutline,
            ViewModelType = typeof(VouchersViewModel),
            ScreenName = "Vouchers"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "المصاريف",
            Icon = PackIconKind.CashMinus,
            ViewModelType = typeof(ExpenseViewModel),
            ScreenName = "Expenses"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "القاصات والمصرف",
            Icon = PackIconKind.Bank,
            ViewModelType = typeof(CashBankViewModel),
            ScreenName = "CashAndBank"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "المستثمرون",
            Icon = PackIconKind.TrendingUp,
            ViewModelType = typeof(InvestorsViewModel),
            ScreenName = "Investors"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "أرصدة المستثمرين الافتتاحية",
            Icon = PackIconKind.AccountCashOutline,
            ViewModelType = typeof(OpeningInvestorsViewModel),
            ScreenName = "OpeningInvestors"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "المخازن",
            Icon = PackIconKind.Warehouse,
            ViewModelType = typeof(WarehousesViewModel),
            ScreenName = "Warehouses"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "الأرصدة الافتتاحية",
            Icon = PackIconKind.PackageVariantClosedPlus,
            ViewModelType = typeof(OpeningStockViewModel),
            ScreenName = "OpeningStock"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "تسوية مخزنية",
            Icon = PackIconKind.TuneVerticalVariant,
            ViewModelType = typeof(StockAdjustmentViewModel),
            ScreenName = "StockAdjustment"
        });
        var reportsGroup = new NavigationMenuItem
        {
            Title = "التقارير",
            Icon = PackIconKind.ChartBar,
            IsGroupHeader = true,
            ScreenName = "Reports"
        };
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير المبيعات", Icon = PackIconKind.CashRegister, ViewModelType = typeof(SalesReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير المشتريات", Icon = PackIconKind.CartArrowDown, ViewModelType = typeof(PurchasesReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير الأرباح", Icon = PackIconKind.TrendingUp, ViewModelType = typeof(ProfitReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "أفضل المنتجات", Icon = PackIconKind.StarCircle, ViewModelType = typeof(TopProductsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "هامش ربح المنتجات", Icon = PackIconKind.ChartPie, ViewModelType = typeof(ProductProfitMarginReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "أعمار ذمم الأقساط", Icon = PackIconKind.TimelineClock, ViewModelType = typeof(InstallmentAgingReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "ملخص الأقساط", Icon = PackIconKind.CalendarMultipleCheck, ViewModelType = typeof(InstallmentsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تفاصيل الأقساط", Icon = PackIconKind.CalendarClock, ViewModelType = typeof(InstallmentDetailReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الأقساط المسددة", Icon = PackIconKind.CheckCircle, ViewModelType = typeof(PaidInstallmentsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الأقساط غير المسددة", Icon = PackIconKind.AlertCircle, ViewModelType = typeof(UnpaidInstallmentsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الأقساط المتأخرة", Icon = PackIconKind.ClockAlert, ViewModelType = typeof(OverdueReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "ملخص العملاء", Icon = PackIconKind.AccountMultiple, ViewModelType = typeof(CustomersOverviewReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "ملخص الموردين", Icon = PackIconKind.TruckDelivery, ViewModelType = typeof(SuppliersOverviewReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "مقارنة الأرباح", Icon = PackIconKind.Compare, ViewModelType = typeof(ProfitComparisonReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "حركة المنتجات", Icon = PackIconKind.SwapVertical, ViewModelType = typeof(ProductMovementReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "كشف حساب عميل", Icon = PackIconKind.AccountCash, ViewModelType = typeof(CustomerStatementViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "كشف حساب مورد", Icon = PackIconKind.Factory, ViewModelType = typeof(SupplierStatementViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير المصاريف", Icon = PackIconKind.CashMinus, ViewModelType = typeof(ExpensesReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الواردات والمصروفات", Icon = PackIconKind.SwapHorizontal, ViewModelType = typeof(IncomeExpenseReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير المخازن", Icon = PackIconKind.Warehouse, ViewModelType = typeof(WarehouseReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "صحة المخزون", Icon = PackIconKind.PackageVariant, ViewModelType = typeof(StockHealthReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير المستثمرين", Icon = PackIconKind.AccountGroup, ViewModelType = typeof(InvestorsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "التدفق النقدي", Icon = PackIconKind.ChartTimelineVariantShimmer, ViewModelType = typeof(CashFlowReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "موازنة يومية", Icon = PackIconKind.ScaleBalance, ViewModelType = typeof(BalanceSheetViewModel), ScreenName = "BalanceSheet", IsSubItem = true });
        MenuItems.Add(reportsGroup);
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "رأس المال",
            Icon = PackIconKind.Cash,
            ViewModelType = typeof(CapitalAdjustmentViewModel),
            ScreenName = "Capital"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "سجل العمليات",
            Icon = PackIconKind.History,
            ViewModelType = typeof(AuditLogViewModel),
            ScreenName = "AuditLog"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "المستخدمون",
            Icon = PackIconKind.AccountMultiple,
            ViewModelType = typeof(UsersViewModel),
            ScreenName = "Users"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "الصلاحيات",
            Icon = PackIconKind.ShieldKey,
            ViewModelType = typeof(PermissionsViewModel),
            ScreenName = "Permissions"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "إعدادات الطباعة",
            Icon = PackIconKind.PrinterSettings,
            ViewModelType = typeof(PrintLayoutSettingsViewModel),
            ScreenName = "Backup"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "النسخ الاحتياطي",
            Icon = PackIconKind.DatabaseCog,
            ViewModelType = typeof(BackupRestoreViewModel),
            ScreenName = "Backup"
        });

        // Mark dashboard as visually selected but do NOT navigate yet
        // Navigation happens from App.OnStartup after permissions are loaded
        MenuItems[0].IsSelected = true;
    }

    private bool _suppressNavigation;
    private bool _isTabSwitchInternal;

    partial void OnSelectedMenuItemChanged(NavigationMenuItem? value)
    {
        if (value is null) return;
        if (_suppressNavigation) return;
        ApplyMenuSelection(value);
    }

    /// <summary>
    /// Select the dashboard menu item visually and sync SelectedMenuItem without triggering a second navigation.
    /// Call this from App.OnStartup after navigation has already occurred.
    /// </summary>
    public void SyncSelectedMenuItem()
    {
        _suppressNavigation = true;
        SelectedMenuItem = MenuItems[0];
        _suppressNavigation = false;
    }

    [RelayCommand]
    private void SelectMenuItem(NavigationMenuItem item)
    {
        SelectedMenuItem = item;
    }

    private void ApplyMenuSelection(NavigationMenuItem item)
    {
        // Group header: toggle expand/collapse, don't navigate
        if (item.IsGroupHeader)
        {
            item.IsExpanded = !item.IsExpanded;
            return;
        }

        // Permission guard: check CanView (Admin always passes)
        if (!string.IsNullOrEmpty(item.ScreenName)
            && item.ScreenName != "Dashboard"
            && !_currentUserService.CanView(item.ScreenName))
        {
            _toast.ShowWarning("ليس لديك صلاحية للوصول إلى هذه الشاشة");
            return;
        }

        // Clear selection across all items including children
        foreach (var m in MenuItems)
        {
            m.IsSelected = m == item;
            foreach (var c in m.Children)
                c.IsSelected = c == item;
        }

        PageTitle = item.Title;

        if (item.ViewModelType is not null)
            _ = OpenTabAsync(item.ViewModelType, item.Title, item.Icon);
    }

    /// <summary>Opens a tab for startup, wizard, or external callers.</summary>
    public async Task OpenTabAsync(Type viewModelType, string title, PackIconKind icon, bool activateIfExists = true)
    {
        if (activateIfExists)
        {
            var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == viewModelType);
            if (existing is not null)
            {
                ActivateTab(existing);
                return;
            }
        }

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لفتح شاشة جديدة.");
            return;
        }

        var scope = _serviceProvider.CreateScope();
        try
        {
            var viewModel = (ViewModelBase)scope.ServiceProvider.GetRequiredService(viewModelType);

            var tab = new DocumentTab
            {
                Title = title,
                Icon = icon,
                ViewModelType = viewModelType,
                ViewModel = viewModel,
                Scope = scope
            };

            OpenTabs.Add(tab);
            ActivateTab(tab);
            UpdateTabCloseStates();
            UpdateTabPinStates();

            var screenName = FlattenMenuItems()
                .FirstOrDefault(m => m.ViewModelType == viewModelType)?.ScreenName
                ?? viewModelType.Name;
            _recentActivity.Record($"فتح: {title}", screenName, screenName, viewModelType);

            await SafeInitializeTabAsync(viewModel);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    partial void OnSelectedTabChanged(DocumentTab? value)
    {
        if (_isTabSwitchInternal || value is null)
            return;

        ApplyActiveTabState(value);
    }

    private void ActivateTab(DocumentTab tab)
    {
        _isTabSwitchInternal = true;
        SelectedTab = tab;
        _isTabSwitchInternal = false;
        ApplyActiveTabState(tab);
    }

    private void ApplyActiveTabState(DocumentTab tab)
    {
        foreach (var t in OpenTabs)
            t.IsSelected = t == tab;

        CurrentViewModel = tab.ViewModel;
        PageTitle = tab.Title;
        TabContentGeneration++;
        OnTabViewModelActivated(tab.ViewModel);
    }

    private void OnTabViewModelActivated(ViewModelBase viewModel)
    {
        if (_investorsLookupDirty && viewModel is IInvestorLookupHost host)
        {
            _investorsLookupDirty = false;
            _ = RefreshInvestorsLookupSafeAsync(host);
        }
    }

    [RelayCommand]
    private void SelectTab(DocumentTab? tab)
    {
        if (tab is null || tab == SelectedTab)
            return;

        ActivateTab(tab);
    }

    [RelayCommand]
    private void CloseTab(DocumentTab? tab)
    {
        if (tab is null || !tab.CanClose)
            return;

        if (tab.ViewModel.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "يوجد تغييرات غير محفوظة. هل تريد إغلاق التبويب بدون حفظ؟",
                "إغلاق التبويب",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            if (result == MessageBoxResult.No)
                return;
        }

        var index = OpenTabs.IndexOf(tab);
        var wasSelected = SelectedTab == tab;

        OpenTabs.Remove(tab);
        tab.Dispose();

        if (OpenTabs.Count == 0)
        {
            _ = OpenTabAsync(typeof(DashboardViewModel), "لوحة التحكم", PackIconKind.ViewDashboard, activateIfExists: false);
            return;
        }

        if (wasSelected && OpenTabs.Count > 0)
        {
            var nextIndex = Math.Min(index, OpenTabs.Count - 1);
            ActivateTab(OpenTabs[nextIndex]);
        }

        UpdateTabCloseStates();
        UpdateTabPinStates();
    }

    /// <summary>يفتح لوحة التحكم ثم التبويبات المثبتة المحفوظة في التفضيلات.</summary>
    public async Task OpenInitialSessionTabsAsync()
    {
        await OpenTabAsync(typeof(DashboardViewModel), "لوحة التحكم", PackIconKind.ViewDashboard, activateIfExists: false);

        foreach (var key in _userPreferences.Current.PinnedMenuScreens)
        {
            if (OpenTabs.Count >= MaxOpenTabs)
                break;

            var menu = FindMenuItemByPreferenceKey(key);
            if (menu?.ViewModelType is null || menu.ViewModelType == typeof(DashboardViewModel))
                continue;
            if (!menu.IsVisible || !CanMenuBeShownByPermissions(menu))
                continue;

            await OpenTabAsync(menu.ViewModelType, menu.Title, menu.Icon, activateIfExists: false);
        }

        UpdateTabPinStates();
        ActivateTab(OpenTabs[0]);
        SyncSelectedMenuItem();
    }

    [RelayCommand]
    private void TogglePinTab(DocumentTab? tab)
    {
        tab ??= SelectedTab;
        if (tab is null || tab.ViewModelType == typeof(DashboardViewModel))
        {
            _toast.ShowWarning("لا يمكن تثبيت لوحة التحكم");
            return;
        }

        var key = tab.ViewModelType.Name;
        var pinned = _userPreferences.Current.PinnedMenuScreens.ToList();

        if (pinned.Contains(key))
        {
            pinned.Remove(key);
            _userPreferences.Update(p => p.PinnedMenuScreens = pinned);
            UpdateTabPinStates();
            _toast.ShowInfo("تم إلغاء تثبيت التبويب");
            return;
        }

        if (pinned.Count >= MaxPinnedTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxPinnedTabs} تبويبات مثبتة");
            return;
        }

        pinned.Add(key);
        _userPreferences.Update(p => p.PinnedMenuScreens = pinned);
        UpdateTabPinStates();
        _toast.ShowSuccess("سيتم فتح هذا التبويب تلقائياً عند تشغيل النظام");
    }

    private void UpdateTabPinStates()
    {
        var pinned = _userPreferences.Current.PinnedMenuScreens;
        foreach (var tab in OpenTabs)
            tab.IsPinned = pinned.Contains(tab.ViewModelType.Name);
    }

    private NavigationMenuItem? FindMenuItemByPreferenceKey(string key) =>
        FlattenMenuItems().FirstOrDefault(m => GetMenuPreferenceKey(m) == key);

    private void UpdateTabCloseStates()
    {
        var onlyDashboard = OpenTabs.Count == 1
                            && OpenTabs[0].ViewModelType == typeof(DashboardViewModel);

        foreach (var tab in OpenTabs)
            tab.CanClose = !onlyDashboard;
    }

    /// <summary>Closes and disposes all tabs (logout).</summary>
    public void CloseAllTabs()
    {
        foreach (var tab in OpenTabs.ToList())
        {
            OpenTabs.Remove(tab);
            tab.Dispose();
        }

        SelectedTab = null;
        CurrentViewModel = null;
    }

    private static async Task SafeInitializeTabAsync(ViewModelBase viewModel)
    {
        try
        {
            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Tabs] InitializeAsync failed for {viewModel.GetType().Name}: {ex}");
            BeautifulMessageDialog.ShowError(
                $"خطأ في تحميل الشاشة:\n\n{ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>
    /// Call after login to load permissions and apply menu visibility.
    /// </summary>
    public async Task ApplyPermissionsAsync()
    {
        if (_currentUserService.UserId is int userId)
        {
            var perms = await _authService.GetUserPermissionsAsync(userId);
            _currentUserService.SetPermissions(perms);
        }

        // Admin sees everything; Users + Permissions are admin-only
        foreach (var item in MenuItems)
        {
            if (item.ScreenName is "Users" or "Permissions" or "AuditLog" or "Capital" or "Backup")
                item.IsVisible = _currentUserService.IsAdmin;
            else if (item.ScreenName == "Dashboard")
                item.IsVisible = true;
            else
                item.IsVisible = _currentUserService.CanView(item.ScreenName);

            // Apply visibility to children
            foreach (var child in item.Children)
                child.IsVisible = _currentUserService.CanView(child.ScreenName);
        }

        ApplyMenuVisibilityFromPreferences();
    }

    private static async Task RefreshInvestorsLookupSafeAsync(IInvestorLookupHost host)
    {
        try
        {
            await host.RefreshInvestorsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Investors] Refresh lookup failed: {ex}");
        }
    }

    private void UpdateDateTime()
    {
        var now = DateTime.Now;
        CurrentGregorianDate = now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        CurrentTime = now.ToString("hh:mm tt", new CultureInfo("ar-IQ"));

        try
        {
            var hijri = new HijriCalendar();
            int hYear = hijri.GetYear(now);
            int hMonth = hijri.GetMonth(now);
            int hDay = hijri.GetDayOfMonth(now);
            CurrentHijriDate = $"{hYear}/{hMonth:D2}/{hDay:D2} هـ";
        }
        catch
        {
            CurrentHijriDate = string.Empty;
        }
    }

    private DispatcherTimer? _clockTimer;

    private void StartClock()
    {
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateDateTime();
        _clockTimer.Start();
    }

    private void StopClock()
    {
        _clockTimer?.Stop();
        _clockTimer = null;
    }

    [RelayCommand]
    private void ToggleSearch()
    {
        if (!IsSearchOpen && !IsSidebarExpanded)
            IsSidebarExpanded = true;

        IsSearchOpen = !IsSearchOpen;
        if (!IsSearchOpen)
        {
            SearchText = string.Empty;
            SearchResults.Clear();
            HasSearchResults = false;
        }
    }

    [RelayCommand]
    private void CloseSearch()
    {
        if (!IsSearchOpen) return;
        IsSearchOpen = false;
        SearchText = string.Empty;
        SearchResults.Clear();
        HasSearchResults = false;
    }

    [RelayCommand]
    private void SelectSearchResult(NavigationMenuItem? item)
    {
        if (item is null) return;
        IsSearchOpen = false;
        SearchText = string.Empty;
        SearchResults.Clear();
        HasSearchResults = false;
        SelectedMenuItem = item;
    }

    partial void OnSearchTextChanged(string value) => UpdateSearchResults();

    partial void OnIsSearchOpenChanged(bool value)
    {
        if (value)
            UpdateSearchResults();
        else
            HasSearchResults = false;
    }

    private void UpdateSearchResults()
    {
        SearchResults.Clear();
        HasSearchResults = false;

        var term = SearchText?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 1)
            return;

        foreach (var item in GetSearchableMenuItems())
        {
            if (item.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
                SearchResults.Add(item);
        }

        HasSearchResults = IsSearchOpen && SearchResults.Count > 0;
    }

    private IEnumerable<NavigationMenuItem> GetSearchableMenuItems()
    {
        foreach (var item in MenuItems)
        {
            if (item.IsVisible && !item.IsGroupHeader && item.ViewModelType is not null)
                yield return item;

            foreach (var child in item.Children)
            {
                if (child.IsVisible && child.ViewModelType is not null)
                    yield return child;
            }
        }
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void ToggleQuickAssist() => IsQuickAssistOpen = !IsQuickAssistOpen;

    [RelayCommand]
    private void CloseQuickAssist() => IsQuickAssistOpen = false;

    [RelayCommand]
    private async Task OpenPrintSettings()
    {
        IsQuickAssistOpen = false;
        await OpenTabAsync(typeof(PrintLayoutSettingsViewModel), "إعدادات الطباعة", PackIconKind.PrinterSettings);
    }

    [RelayCommand]
    private void OpenCalculator()
    {
        IsQuickAssistOpen = false;
        try
        {
            Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر فتح الحاسبة:\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenOnScreenKeyboard()
    {
        IsQuickAssistOpen = false;
        try
        {
            var osk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "osk.exe");
            Process.Start(new ProcessStartInfo(osk) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر فتح لوحة المفاتيح:\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async void GoToDashboard()
    {
        IsQuickAssistOpen = false;
        await OpenTabAsync(typeof(DashboardViewModel), "لوحة التحكم", PackIconKind.ViewDashboard);
        var dashboard = MenuItems.FirstOrDefault(m => m.ViewModelType == typeof(DashboardViewModel));
        if (dashboard is not null)
        {
            _suppressNavigation = true;
            SelectedMenuItem = dashboard;
            _suppressNavigation = false;
        }
    }

    [RelayCommand]
    private async void GoToBackup()
    {
        IsQuickAssistOpen = false;
        var backup = FlattenMenuItems().FirstOrDefault(m => m.ViewModelType == typeof(BackupRestoreViewModel));
        if (backup is not null)
        {
            _suppressNavigation = true;
            SelectedMenuItem = backup;
            _suppressNavigation = false;
            await OpenTabAsync(backup.ViewModelType!, backup.Title, backup.Icon);
        }
    }

    [RelayCommand]
    private void RequestExit() => IsExitDialogOpen = true;

    private IEnumerable<NavigationMenuItem> FlattenMenuItems()
    {
        foreach (var item in MenuItems)
        {
            yield return item;
            foreach (var child in item.Children)
                yield return child;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        StopClock();
        CloseAllTabs();
        _currentUserService.Clear();
        LogoutRequested?.Invoke();
    }

    /// <summary>
    /// Restarts the clock after a new login session.
    /// </summary>
    public void RestartSession()
    {
        UpdateDateTime();
        StartClock();
    }

    [RelayCommand]
    private void NavigateBack()
    {
        if (SelectedTab is null || OpenTabs.Count < 2)
            return;

        var index = OpenTabs.IndexOf(SelectedTab);
        if (index > 0)
            ActivateTab(OpenTabs[index - 1]);
    }

    // ── Exit Dialog Commands ──

    [RelayCommand]
    private void ExitApplication() => CompleteApplicationExit();

    [RelayCommand]
    private async Task ExitWithBackup()
    {
        ExitBackupPath = string.Empty;
        ExitBackupStatus = string.Empty;

        var dialog = new SaveFileDialog
        {
            Title = "اختر مكان حفظ النسخة الاحتياطية",
            Filter = "ملف النسخ الاحتياطي (*.bak)|*.bak",
            FileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
            InitialDirectory = GetBackupDialogInitialDirectory(),
            AddExtension = true,
            DefaultExt = ".bak",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return;

        IsExitBackupInProgress = true;
        ExitBackupStatus = "جاري إنشاء النسخة الاحتياطية...";

        try
        {
            var fullPath = dialog.FileName;
            await _backupService.BackupDatabaseAsync(fullPath);

            ExitBackupPath = fullPath;
            ExitBackupStatus = "تم إنشاء النسخة بنجاح! سيتم إغلاق البرنامج...";
            OpenBackupInExplorer(fullPath);

            await Task.Delay(1200);
            CompleteApplicationExit();
        }
        catch (Exception ex)
        {
            ExitBackupStatus = $"فشل إنشاء النسخة:\n{ex.Message}";
            IsExitBackupInProgress = false;
            Controls.BeautifulMessageDialog.ShowError($"فشل النسخ الاحتياطي:\n\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenExitBackupFolder()
    {
        if (!string.IsNullOrWhiteSpace(ExitBackupPath) && File.Exists(ExitBackupPath))
            OpenBackupInExplorer(ExitBackupPath);
    }

    private static string GetBackupDialogInitialDirectory()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (Directory.Exists(desktop))
            return desktop;

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Directory.Exists(documents) ? documents : @"D:\";
    }

    [RelayCommand]
    private void CancelExit()
    {
        IsExitDialogOpen = false;
        IsExitBackupInProgress = false;
        ExitBackupStatus = string.Empty;
        ExitBackupPath = string.Empty;
    }

    private void CompleteApplicationExit()
    {
        IsExitDialogOpen = false;
        IsExitBackupInProgress = false;
        IsExitConfirmed = true;
        StopClock();
        Application.Current.Shutdown();
    }

    private static void OpenBackupInExplorer(string fullPath)
    {
        try
        {
            var explorerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "explorer.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = explorerPath,
                Arguments = $"/select,\"{fullPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Backup] Could not open explorer: {ex}");
        }
    }
}
