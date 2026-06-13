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
    private readonly ISoundService _sound;
    private readonly IGlobalSearchService _globalSearchService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly ThemeService _themeService;
    private readonly IRecentActivityService _recentActivity;
    private readonly IAuditLogService _auditLogService;
    private readonly IHelpSupportService _helpSupport;
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

    [ObservableProperty]
    private bool _isReportFlyoutOpen;

    [ObservableProperty]
    private string _activeReportCategoryTitle = string.Empty;

    [ObservableProperty]
    private PackIconKind _activeReportCategoryIcon;

    [ObservableProperty]
    private string _activeReportCategoryAccent = "#1565C0";

    [ObservableProperty]
    private NavigationMenuItem? _activeReportCategory;

    public ObservableCollection<ReportMenuEntry> ReportFlyoutItems { get; } = [];

    public MainWindowViewModel(INavigationService navigationService, IServiceProvider serviceProvider,
        CurrentUserService currentUserService, IAuthService authService,
        IBackupService backupService, IInvestorRefreshService investorRefresh,
        IToastNotificationService toast,
        ISoundService sound,
        IGlobalSearchService globalSearchService,
        IUserPreferencesService userPreferences,
        ThemeService themeService,
        IRecentActivityService recentActivity,
        IAuditLogService auditLogService,
        ISmartAlertService smartAlertService,
        IHelpSupportService helpSupport,
        INotificationCenterService notificationCenter,
        IUserTaskService userTaskService,
        IUserNoteService userNoteService,
        ICustomerStatementQuickService customerStatementQuick,
        IOfflineReminderService offlineReminder)
    {
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _currentUserService = currentUserService;
        _authService = authService;
        _backupService = backupService;
        _investorRefresh = investorRefresh;
        _toast = toast;
        _sound = sound;
        _globalSearchService = globalSearchService;
        _userPreferences = userPreferences;
        _themeService = themeService;
        _isSoundEnabled = _userPreferences.Current.SoundEnabled;
        _recentActivity = recentActivity;
        _auditLogService = auditLogService;
        _smartAlertService = smartAlertService;
        _helpSupport = helpSupport;
        _notificationCenter = notificationCenter;
        _userTaskService = userTaskService;
        _userNoteService = userNoteService;
        _customerStatementQuick = customerStatementQuick;

        offlineReminder.ReminderRaised += OnOfflineReminderRaised;
        offlineReminder.Start();

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
        InvoiceNavigationBridge.EditSalesInvoiceAsync = EditSalesInvoiceAsync;
        InvoiceNavigationBridge.EditPurchaseInvoiceAsync = EditPurchaseInvoiceAsync;
        InvoiceNavigationBridge.EditInstallmentInvoiceAsync = EditInstallmentInvoiceAsync;
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

    private async Task EditInstallmentInvoiceAsync(int invoiceId)
    {
        var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == typeof(InstallmentInvoiceViewModel));
        if (existing?.ViewModel is InstallmentInvoiceViewModel installmentVm)
        {
            ActivateTab(existing);
            await installmentVm.LoadInvoiceForEditAsync(invoiceId);
            return;
        }

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لفتح فاتورة الأقساط.");
            return;
        }

        InvoiceNavigationBridge.PendingInstallmentEditInvoiceId = invoiceId;
        await OpenTabAsync(typeof(InstallmentInvoiceViewModel), "فاتورة أقساط", PackIconKind.CalendarClock, activateIfExists: false);
    }

    private async Task EditSalesInvoiceAsync(int invoiceId)
    {
        var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == typeof(SalesInvoiceViewModel));
        if (existing?.ViewModel is SalesInvoiceViewModel salesVm)
        {
            ActivateTab(existing);
            await salesVm.LoadInvoiceForEditAsync(invoiceId);
            return;
        }

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لفتح فاتورة المبيعات.");
            return;
        }

        InvoiceNavigationBridge.PendingSalesEditInvoiceId = invoiceId;
        await OpenTabAsync(typeof(SalesInvoiceViewModel), "فاتورة مبيعات", PackIconKind.CashRegister, activateIfExists: false);
    }

    private async Task EditPurchaseInvoiceAsync(int invoiceId)
    {
        var existing = OpenTabs.FirstOrDefault(t => t.ViewModelType == typeof(PurchaseInvoiceViewModel));
        if (existing?.ViewModel is PurchaseInvoiceViewModel purchaseVm)
        {
            ActivateTab(existing);
            await purchaseVm.LoadInvoiceForEditAsync(invoiceId);
            return;
        }

        if (OpenTabs.Count >= MaxOpenTabs)
        {
            _toast.ShowWarning($"الحد الأقصى {MaxOpenTabs} تبويبات. أغلِق تبويباً لفتح فاتورة المشتريات.");
            return;
        }

        InvoiceNavigationBridge.PendingPurchaseEditInvoiceId = invoiceId;
        await OpenTabAsync(typeof(PurchaseInvoiceViewModel), "فاتورة مشتريات", PackIconKind.CartArrowDown, activateIfExists: false);
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
            Title = "لوحة التحصيل",
            Icon = PackIconKind.CashMultiple,
            ViewModelType = typeof(CollectionDashboardViewModel),
            ScreenName = "Installments"
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
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "التقارير",
            IsMenuSectionLabel = true,
            ScreenName = ScreenPermissionRegistry.Reports
        });

        foreach (var category in ReportMenuCatalog.CreateCategoryMenuItems())
            MenuItems.Add(category);

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
            Title = "معالج النقل",
            Icon = PackIconKind.DatabaseImport,
            ViewModelType = typeof(MigrationWizardViewModel),
            ScreenName = "DataImport"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "نقل مخازن",
            Icon = PackIconKind.TruckDelivery,
            ViewModelType = typeof(WarehouseTransferViewModel),
            ScreenName = "Warehouses"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "إعدادات الميزات",
            Icon = PackIconKind.TuneVariant,
            ViewModelType = typeof(BusinessFeaturesSettingsViewModel),
            ScreenName = "BusinessFeatures"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "إعدادات الطباعة",
            Icon = PackIconKind.PrinterSettings,
            ViewModelType = typeof(PrintLayoutSettingsViewModel),
            ScreenName = "PrintSettings"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "النسخ الاحتياطي",
            Icon = PackIconKind.DatabaseCog,
            ViewModelType = typeof(BackupRestoreViewModel),
            ScreenName = "Backup"
        });
        MenuItems.Add(new NavigationMenuItem
        {
            Title = "المزامنة السحابية",
            Icon = PackIconKind.CloudSync,
            ViewModelType = typeof(CloudSyncSettingsViewModel),
            ScreenName = "CloudSync"
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
        if (item.IsMenuSectionLabel)
            return;

        if (item.IsReportCategory)
        {
            ToggleReportFlyout(item);
            return;
        }

        // Group header: toggle expand/collapse, don't navigate
        if (item.IsGroupHeader)
        {
            item.IsExpanded = !item.IsExpanded;
            return;
        }

        CloseReportFlyout();

        if (item.ViewModelType is not null && !TryAuthorizeScreen(item.ViewModelType, out _))
            return;

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

    public void ToggleReportFlyout(NavigationMenuItem category)
    {
        if (!category.IsReportCategory || !category.IsVisible)
            return;

        if (IsReportFlyoutOpen && ActiveReportCategory == category)
        {
            CloseReportFlyout();
            return;
        }

        ShowReportFlyout(category);
    }

    private void ShowReportFlyout(NavigationMenuItem category)
    {
        ActiveReportCategory = category;
        ActiveReportCategoryTitle = category.Title;
        ActiveReportCategoryIcon = category.Icon;
        ActiveReportCategoryAccent = category.CategoryAccentColor;

        ReportFlyoutItems.Clear();
        foreach (var entry in ReportMenuCatalog.GetVisibleReports(category))
            ReportFlyoutItems.Add(entry);

        if (ReportFlyoutItems.Count == 0)
            return;

        foreach (var m in MenuItems)
        {
            m.IsSelected = m == category;
            foreach (var c in m.Children)
                c.IsSelected = false;
        }

        IsReportFlyoutOpen = true;
    }

    [RelayCommand]
    private void CloseReportFlyout()
    {
        IsReportFlyoutOpen = false;
        ActiveReportCategory = null;

        foreach (var m in MenuItems.Where(i => i.IsReportCategory))
            m.IsSelected = false;
    }

    [RelayCommand]
    private async Task OpenReportFromFlyoutAsync(ReportMenuEntry? entry)
    {
        if (entry?.ViewModelType is null)
            return;

        if (!TryAuthorizeScreen(entry.ViewModelType, out _))
            return;

        CloseReportFlyout();
        PageTitle = entry.Title;
        await OpenTabAsync(entry.ViewModelType, entry.Title, entry.Icon);
    }

    public bool TryAuthorizeScreen(Type viewModelType, out string? deniedMessage)
    {
        if (viewModelType == typeof(SetupWizardViewModel))
        {
            deniedMessage = null;
            return true;
        }

        var screenName = ScreenPermissionRegistry.GetScreenName(viewModelType);
        if (_currentUserService.CanView(screenName))
        {
            deniedMessage = null;
            return true;
        }

        deniedMessage = $"ليس لديك صلاحية للوصول إلى: {ScreenPermissionRegistry.GetLabel(screenName)}";
        _toast.ShowWarning(deniedMessage);
        return false;
    }

    public void RefreshMenuVisibility()
    {
        var hidden = _userPreferences.Current.HiddenMenuScreens;
        var flags = _userPreferences.Current.FeatureFlags;

        foreach (var item in FlattenMenuItems())
        {
            if (item.IsMenuSectionLabel)
                continue;

            if (item.IsReportCategory)
            {
                foreach (var child in item.Children)
                {
                    if (child.ViewModelType is null)
                        continue;

                    var childPermitted = _currentUserService.CanView(child.ScreenName);
                    var childFeatureOk = IsFeatureFlagVisible(child, flags);
                    var childPrefOk = !IsCustomizableMenuItem(child) || !hidden.Contains(GetMenuPreferenceKey(child));
                    child.IsVisible = childPermitted && childFeatureOk && childPrefOk;
                }

                item.IsVisible = item.Children.Any(c => c.IsVisible);
                continue;
            }

            if (item.ViewModelType is null)
                continue;

            if (item.ScreenName == ScreenPermissionRegistry.Dashboard)
            {
                item.IsVisible = true;
                continue;
            }

            var permitted = _currentUserService.CanView(item.ScreenName);
            var featureOk = IsFeatureFlagVisible(item, flags);
            var prefOk = !IsCustomizableMenuItem(item) || !hidden.Contains(GetMenuPreferenceKey(item));
            item.IsVisible = permitted && featureOk && prefOk;
        }

        var reportsSection = MenuItems.FirstOrDefault(i => i.IsMenuSectionLabel && i.ScreenName == ScreenPermissionRegistry.Reports);
        if (reportsSection is not null)
            reportsSection.IsVisible = MenuItems.Any(i => i.IsReportCategory && i.IsVisible);

        foreach (var group in MenuItems.Where(i => i.IsGroupHeader))
            group.IsVisible = group.Children.Any(c => c.IsVisible);
    }

    private void ResetMenuVisibilityOnLogout()
    {
        foreach (var item in FlattenMenuItems())
        {
            if (item.IsMenuSectionLabel)
            {
                item.IsVisible = false;
                continue;
            }

            if (item.IsReportCategory)
            {
                item.IsVisible = false;
                foreach (var child in item.Children)
                    child.IsVisible = false;
                continue;
            }

            if (item.ViewModelType is not null && item.ScreenName != ScreenPermissionRegistry.Dashboard)
                item.IsVisible = false;
        }

        foreach (var group in MenuItems.Where(i => i.IsGroupHeader))
            group.IsVisible = false;

        CloseReportFlyout();

        var dashboard = MenuItems.FirstOrDefault(m => m.ScreenName == ScreenPermissionRegistry.Dashboard);
        if (dashboard is not null)
            dashboard.IsVisible = true;
    }

    /// <summary>Opens a tab for startup, wizard, or external callers.</summary>
    public async Task OpenTabAsync(Type viewModelType, string title, PackIconKind icon, bool activateIfExists = true)
    {
        if (!TryAuthorizeScreen(viewModelType, out _))
            return;

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

            var screenName = ScreenPermissionRegistry.GetScreenName(viewModelType);
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
            var (normalized, shouldSave, infoMessage, warningMessage) =
                PermissionCatalogHelper.NormalizeForLogin(_currentUserService.IsAdmin, perms);

            if (shouldSave)
            {
                await _authService.SaveUserPermissionsAsync(userId, normalized);
                perms = normalized;
            }

            _currentUserService.SetPermissions(perms);

            if (!string.IsNullOrEmpty(infoMessage))
                _toast.ShowInfo(infoMessage);

            if (!string.IsNullOrEmpty(warningMessage))
                _toast.ShowWarning(warningMessage);
        }

        RefreshMenuVisibility();
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
            if (item.IsMenuSectionLabel)
                continue;

            if (item.IsVisible && !item.IsGroupHeader && !item.IsReportCategory && item.ViewModelType is not null)
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
        if (!IsSidebarExpanded)
            CloseReportFlyout();
    }

    [RelayCommand]
    private void ToggleQuickAssist() => IsQuickAssistOpen = !IsQuickAssistOpen;

    [RelayCommand]
    private void CloseQuickAssist() => IsQuickAssistOpen = false;

    [RelayCommand]
    private void OpenHelpWhatsApp() => _helpSupport.OpenWhatsAppSupport();

    [RelayCommand]
    private void OpenHelpVideos()
    {
        IsQuickAssistOpen = false;
        _helpSupport.ShowVideosWindow(Application.Current.MainWindow);
    }

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
        ResetPersonalWorkspaceSession();
        ResetMenuVisibilityOnLogout();
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
        ResetNotificationSession();
        ResetPersonalWorkspaceSession();
        _ = InitializePersonalWorkspaceAsync();
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
