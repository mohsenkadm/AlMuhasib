using System.IO;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    public IServiceProvider Services => _serviceProvider;
    private bool _isLoggingOut;

    public App()
    {
        // Global exception handlers so no exception is silently swallowed
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[DispatcherUnhandled] {e.Exception}");
        BeautifulMessageDialog.ShowError(
            $"حدث خطأ غير متوقع:\n\n{e.Exception.Message}\n\n{e.Exception.InnerException?.Message}");
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppDomainUnhandled] {ex}");
            BeautifulMessageDialog.ShowError(
                $"حدث خطأ فادح:\n\n{ex.Message}\n\n{ex.InnerException?.Message}");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[UnobservedTask] {e.Exception}");
        e.SetObserved();
        Current?.Dispatcher.BeginInvoke(() =>
        {
            BeautifulMessageDialog.ShowWarning(
                $"حدث خطأ في مهمة خلفية:\n\n{e.Exception.InnerException?.Message ?? e.Exception.Message}");
        });
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Infrastructure (EF Core + Repositories + AuthService)
        services.AddInfrastructure(configuration);

        // Services
        var currentUserService = new CurrentUserService();
        services.AddSingleton<CurrentUserService>(currentUserService);
        services.AddSingleton<ICurrentUserService>(currentUserService);
        services.AddSingleton<INavigationService, NavigationService>();

        // Export service (Shared project)
        services.AddSingleton<IExportService, AlMuhasib.Shared.Services.ExcelExportService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<PurchaseInvoiceViewModel>();
        services.AddTransient<SalesInvoiceViewModel>();
        services.AddTransient<InstallmentInvoiceViewModel>();
        services.AddTransient<InstallmentsViewModel>();
        services.AddTransient<CashBankViewModel>();
        services.AddTransient<WarehousesViewModel>();
        services.AddTransient<VouchersViewModel>();
        services.AddTransient<ExpenseViewModel>();
        services.AddTransient<InvestorsViewModel>();
        services.AddTransient<SalesReportViewModel>();
        services.AddTransient<PurchasesReportViewModel>();
        services.AddTransient<ProfitReportViewModel>();
        services.AddTransient<InstallmentsReportViewModel>();
        services.AddTransient<InstallmentDetailReportViewModel>();
        services.AddTransient<PaidInstallmentsReportViewModel>();
        services.AddTransient<UnpaidInstallmentsReportViewModel>();
        services.AddTransient<OverdueReportViewModel>();
        services.AddTransient<CustomerStatementViewModel>();
        services.AddTransient<SupplierStatementViewModel>();
        services.AddTransient<ExpensesReportViewModel>();
        services.AddTransient<IncomeExpenseReportViewModel>();
        services.AddTransient<WarehouseReportViewModel>();
        services.AddTransient<InvestorsReportViewModel>();
        services.AddTransient<CashFlowReportViewModel>();
        services.AddTransient<BalanceSheetViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<PermissionsViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<SetupWizardViewModel>();
        services.AddTransient<CapitalAdjustmentViewModel>();
        services.AddTransient<BackupRestoreViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Apply LiveCharts2 global theme
            ChartThemeConfig.Apply();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Startup] ChartTheme error: {ex}");
        }

        // Apply pending migrations and seed admin account
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            await authService.EnsureAdminAccountAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(
                $"خطأ في الاتصال بقاعدة البيانات:\n\n{ex.InnerException?.Message ?? ex.Message}");
            Shutdown();
            return;
        }

        await ShowLoginAndMainWindowAsync();
    }

    /// <summary>
    /// Shows the login dialog. On success, creates and shows the main window
    /// with the dashboard (or setup wizard) and subscribes to logout so the
    /// cycle can restart.
    /// </summary>
    private async Task ShowLoginAndMainWindowAsync()
    {
        // Show login window
        var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
        var result = loginWindow.ShowDialog();

        if (result != true)
        {
            Shutdown();
            return;
        }

        // ── Post-login: create and show main window FIRST ───────
        MainWindow mainWindow;
        try
        {
            mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(
                $"خطأ في إنشاء النافذة الرئيسية:\n\n{ex.InnerException?.Message ?? ex.Message}");
            Shutdown();
            return;
        }

        // Set and show main window immediately so the user sees something
        MainWindow = mainWindow;
        mainWindow.Closed += OnMainWindowClosed;
        mainWindow.Show();

        // Now safely load permissions, check setup, and navigate
        var mainVm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        var currentUser = _serviceProvider.GetRequiredService<CurrentUserService>();
        mainVm.LoggedInUsername = currentUser.Username;

        // Unsubscribe first to avoid duplicate handlers on re-login
        mainVm.LogoutRequested -= OnLogoutRequested;
        mainVm.LogoutRequested += OnLogoutRequested;

        // Restart the clock (stopped during logout, only started once in constructor)
        mainVm.RestartSession();

        try
        {
            await mainVm.ApplyPermissionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Startup] ApplyPermissions error: {ex}");
            BeautifulMessageDialog.ShowWarning(
                $"خطأ في تحميل الصلاحيات:\n\n{ex.InnerException?.Message ?? ex.Message}");
        }

        var nav = _serviceProvider.GetRequiredService<INavigationService>();

        try
        {
            // Check if initial setup is needed (no capital entries exist)
            bool needsSetup = false;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                needsSetup = currentUser.IsAdmin && !await uow.CapitalEntries.AnyAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Startup] Setup check error: {ex}");
            }

            if (needsSetup)
            {
                // Show setup wizard first
                nav.NavigateTo<SetupWizardViewModel>();
                var wizardVm = (SetupWizardViewModel)mainVm.CurrentViewModel!;
                wizardVm.SetupCompleted += () =>
                {
                    nav.NavigateTo<DashboardViewModel>();
                    mainVm.SyncSelectedMenuItem();
                };
            }
            else
            {
                nav.NavigateTo<DashboardViewModel>();
                mainVm.SyncSelectedMenuItem();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Startup] Navigation error: {ex}");
            BeautifulMessageDialog.ShowWarning(
                $"خطأ في التنقل إلى لوحة التحكم:\n\n{ex.InnerException?.Message ?? ex.Message}");

            // Last resort: try to navigate to dashboard anyway
            try { nav.NavigateTo<DashboardViewModel>(); } catch { }
        }
    }

    private async void OnLogoutRequested()
    {
        _isLoggingOut = true;

        // Mark exit confirmed so close dialog doesn't appear during logout
        var mainVm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainVm.IsExitConfirmed = true;

        // Close current main window
        MainWindow?.Close();
        MainWindow = null;

        _isLoggingOut = false;
        mainVm.IsExitConfirmed = false;

        // Restart the login → dashboard cycle
        await ShowLoginAndMainWindowAsync();
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window w)
            w.Closed -= OnMainWindowClosed;

        // If not logging out, the user closed the window normally → exit app
        if (!_isLoggingOut)
            Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
