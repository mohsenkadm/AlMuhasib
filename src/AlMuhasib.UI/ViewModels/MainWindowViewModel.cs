using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly CurrentUserService _currentUserService;
    private readonly IAuthService _authService;
    private readonly IBackupService _backupService;

    public SnackbarMessageQueue SnackbarQueue { get; } = new(TimeSpan.FromSeconds(3));

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

    // ── Exit Dialog ──
    [ObservableProperty]
    private bool _isExitDialogOpen;

    [ObservableProperty]
    private bool _isExitBackupInProgress;

    [ObservableProperty]
    private string _exitBackupStatus = string.Empty;

    public bool IsExitConfirmed { get; set; }

    public ObservableCollection<NavigationMenuItem> MenuItems { get; } = [];

    public MainWindowViewModel(INavigationService navigationService, CurrentUserService currentUserService, IAuthService authService, IBackupService backupService)
    {
        _navigationService = navigationService;
        _currentUserService = currentUserService;
        _authService = authService;
        _backupService = backupService;

        _navigationService.CurrentViewModelChanged += OnNavigationViewModelChanged;

        InitializeMenu();
        UpdateDateTime();
        StartClock();
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
            Title = "المخازن",
            Icon = PackIconKind.Warehouse,
            ViewModelType = typeof(WarehousesViewModel),
            ScreenName = "Warehouses"
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
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "ملخص الأقساط", Icon = PackIconKind.CalendarMultipleCheck, ViewModelType = typeof(InstallmentsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تفاصيل الأقساط", Icon = PackIconKind.CalendarClock, ViewModelType = typeof(InstallmentDetailReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الأقساط المسددة", Icon = PackIconKind.CheckCircle, ViewModelType = typeof(PaidInstallmentsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الأقساط غير المسددة", Icon = PackIconKind.AlertCircle, ViewModelType = typeof(UnpaidInstallmentsReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الأقساط المتأخرة", Icon = PackIconKind.ClockAlert, ViewModelType = typeof(OverdueReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "كشف حساب عميل", Icon = PackIconKind.AccountCash, ViewModelType = typeof(CustomerStatementViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "كشف حساب مورد", Icon = PackIconKind.Factory, ViewModelType = typeof(SupplierStatementViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير المصاريف", Icon = PackIconKind.CashMinus, ViewModelType = typeof(ExpensesReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "الواردات والمصروفات", Icon = PackIconKind.SwapHorizontal, ViewModelType = typeof(IncomeExpenseReportViewModel), ScreenName = "Reports", IsSubItem = true });
        reportsGroup.Children.Add(new NavigationMenuItem { Title = "تقرير المخازن", Icon = PackIconKind.Warehouse, ViewModelType = typeof(WarehouseReportViewModel), ScreenName = "Reports", IsSubItem = true });
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
            SnackbarQueue.Enqueue("ليس لديك صلاحية للوصول إلى هذه الشاشة");
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
        {
            _navigationService.NavigateTo(item.ViewModelType);
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
    }

    private void OnNavigationViewModelChanged(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
        PageTitle = viewModel.PageTitle;
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
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clockTimer.Tick += (_, _) => UpdateDateTime();
        _clockTimer.Start();
    }

    private void StopClock()
    {
        _clockTimer?.Stop();
        _clockTimer = null;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void Logout()
    {
        StopClock();
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
        if (_navigationService.CanGoBack)
            _navigationService.GoBack();
    }

    // ── Exit Dialog Commands ──

    [RelayCommand]
    private void ExitApplication()
    {
        IsExitDialogOpen = false;
        IsExitConfirmed = true;
        StopClock();
        Application.Current.MainWindow?.Close();
    }

    [RelayCommand]
    private async Task ExitWithBackup()
    {
        IsExitBackupInProgress = true;
        ExitBackupStatus = "جاري إنشاء النسخة الاحتياطية...";

        try
        {
            var defaultDir = _backupService.GetDefaultBackupDirectory();
            var fileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var fullPath = Path.Combine(defaultDir, fileName);
            await _backupService.BackupDatabaseAsync(fullPath);

            ExitBackupStatus = "تم إنشاء النسخة بنجاح!";
            await Task.Delay(800);

            IsExitDialogOpen = false;
            IsExitConfirmed = true;
            StopClock();
            Application.Current.MainWindow?.Close();
        }
        catch (Exception ex)
        {
            ExitBackupStatus = $"فشل: {ex.Message}";
            IsExitBackupInProgress = false;
        }
    }

    [RelayCommand]
    private void CancelExit()
    {
        IsExitDialogOpen = false;
        IsExitBackupInProgress = false;
        ExitBackupStatus = string.Empty;
    }
}
