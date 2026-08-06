using System.IO;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Car;
using AlMuhasib.UI.ViewModels.CarTrade;
using AlMuhasib.UI.ViewModels.Hotel;
using AlMuhasib.UI.ViewModels.RealEstate;
using AlMuhasib.UI.ViewModels.Gold;
using AlMuhasib.UI.Modules;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Windows;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AlMuhasib.Core.Models.Updates;

namespace AlMuhasib.UI;

public partial class App : Application
{
    private static readonly string LogFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");

    private readonly SystemProfileService _systemProfile = new();
    private readonly DesktopLicenseService _desktopLicense = new();
    private ServiceProvider? _serviceProvider;
    public IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Application is not initialized.");
    private bool _isLoggingOut;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void EnsureServiceProvider()
    {
        if (_serviceProvider is not null)
            return;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        ScreenPermissionRegistry.Initialize(_serviceProvider.GetRequiredService<SystemModuleRegistry>());
    }

    private static void LogException(string context, Exception ex)
    {
        try
        {
            var entry =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}" +
                $"{ex}{Environment.NewLine}" +
                new string('-', 80) + Environment.NewLine;
            File.AppendAllText(LogFilePath, entry);
        }
        catch
        {
            // ignore logging failures
        }
    }

    private static void ShowFatalError(string title, Exception ex)
    {
        LogException(title, ex);

        var message =
            $"{title}:\n\n" +
            $"{ex.GetType().Name}: {ex.Message}\n\n" +
            (ex.InnerException is { } inner ? $"السبب الداخلي: {inner.Message}\n\n" : string.Empty) +
            $"تم حفظ التفاصيل في:\n{LogFilePath}";

        // Use native MessageBox – does not depend on any WPF resources/themes.
        System.Windows.MessageBox.Show(
            message,
            "قيد - خطأ",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("DispatcherUnhandledException", e.Exception);
        try
        {
            BeautifulMessageDialog.ShowError(
                $"حدث خطأ غير متوقع:\n\n{e.Exception.Message}\n\n{e.Exception.InnerException?.Message}");
        }
        catch
        {
            ShowFatalError("حدث خطأ غير متوقع", e.Exception);
        }
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ShowFatalError("حدث خطأ فادح", ex);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("UnobservedTaskException", e.Exception);
        e.SetObserved();
        Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                BeautifulMessageDialog.ShowWarning(
                    $"حدث خطأ في مهمة خلفية:\n\n{e.Exception.InnerException?.Message ?? e.Exception.Message}");
            }
            catch
            {
                ShowFatalError("حدث خطأ في مهمة خلفية", e.Exception);
            }
        });
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ISystemProfileService>(_systemProfile);
        services.AddSingleton<IDesktopLicenseService>(_desktopLicense);
        services.AddSingleton<SystemModuleRegistry>();

        services.Configure<AppUpdateOptions>(configuration.GetSection(AppUpdateOptions.SectionName));
        services.AddHttpClient();
        services.AddSingleton<IAppUpdateService, AppUpdateService>();

        services.AddInfrastructure(configuration, _systemProfile);

        // Services
        var currentUserService = new CurrentUserService();
        services.AddSingleton<CurrentUserService>(currentUserService);
        services.AddSingleton<ICurrentUserService>(currentUserService);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IInvestorRefreshService, InvestorRefreshService>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<IToastNotificationService, ToastNotificationService>();
        services.AddSingleton<IDeveloperAccessService, DeveloperAccessService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IInvoiceDraftService, InvoiceDraftService>();
        services.AddSingleton<IInvoiceTemplateService, InvoiceTemplateService>();
        services.AddSingleton<IInvoiceQueueService, InvoiceQueueService>();
        services.AddSingleton<INotificationCenterService, NotificationCenterService>();
        services.AddSingleton<IRecentActivityService, RecentActivityService>();
        services.AddSingleton<IRecentExcelExportService, RecentExcelExportService>();
        services.AddSingleton<IFavoriteProductsService, FavoriteProductsService>();
        services.AddSingleton<IOfflineReminderService, OfflineReminderService>();
        services.AddSingleton<BackupSchedulerService>();
        services.AddSingleton<IVoiceRecognitionService, QaydVoiceRecognitionService>();
        services.AddSingleton<VoiceCommandCatalog>();
        services.AddSingleton<VoiceCommandMatcher>();
        services.AddSingleton<VoiceCommandExecutor>();

        // Export service (Shared project) — wrapped to track Excel paths for Open Recent
        services.AddSingleton<AlMuhasib.Shared.Services.ExcelExportService>();
        services.AddSingleton<IExportService>(sp => new TrackingExportService(
            sp.GetRequiredService<AlMuhasib.Shared.Services.ExcelExportService>(),
            sp.GetRequiredService<IRecentExcelExportService>()));
        services.AddSingleton<IWhatsAppShareService, WhatsAppShareService>();
        services.AddSingleton<IHelpSupportService, HelpSupportService>();
        services.AddSingleton<IOpeningInstallmentExcelService, AlMuhasib.Shared.Services.OpeningInstallmentExcelService>();
        services.AddSingleton<IPlatformDeductionExcelService, AlMuhasib.Shared.Services.PlatformDeductionExcelService>();
        services.AddSingleton<IBarcodeLabelService, AlMuhasib.Shared.Services.BarcodeLabelService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<PricingTypesViewModel>();
        services.AddTransient<PackagingTypesViewModel>();
        services.AddTransient<ProductPricingViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<DriversViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<PersonProfileViewModel>();
        services.AddTransient<LoyaltySettingsViewModel>();
        services.AddTransient<LoyaltyAccountsViewModel>();
        services.AddTransient<LoyaltyLedgerViewModel>();
        services.AddTransient<LoyaltySummaryReportViewModel>();
        services.AddTransient<LoyaltyTopCustomersReportViewModel>();
        services.AddTransient<PurchaseInvoiceViewModel>();
        services.AddTransient<SalesInvoiceViewModel>();
        services.AddTransient<PosQuickSaleViewModel>();
        services.AddTransient<BarcodePriceCheckViewModel>();
        services.AddTransient<InstallmentInvoiceViewModel>();
        services.AddTransient<InstallmentsViewModel>();
        services.AddTransient<OpeningInstallmentBalanceViewModel>();
        services.AddTransient<PlatformDeductionSettlementViewModel>();
        services.AddTransient<CashBankViewModel>();
        services.AddTransient<WarehousesViewModel>();
        services.AddTransient<OpeningStockViewModel>();
        services.AddTransient<StockAdjustmentViewModel>();
        services.AddTransient<VouchersViewModel>();
        services.AddTransient<ExpenseViewModel>();
        services.AddTransient<InvestorsViewModel>();
        services.AddTransient<OpeningInvestorsViewModel>();
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
        services.AddTransient<TopProductsReportViewModel>();
        services.AddTransient<ProductProfitMarginReportViewModel>();
        services.AddTransient<MaterialNetProfitReportViewModel>();
        services.AddTransient<LeastProfitMaterialsReportViewModel>();
        services.AddTransient<CustomerNetProfitReportViewModel>();
        services.AddTransient<LeastProfitCustomersReportViewModel>();
        services.AddTransient<InstallmentAgingReportViewModel>();
        services.AddTransient<CustomersOverviewReportViewModel>();
        services.AddTransient<SuppliersOverviewReportViewModel>();
        services.AddTransient<ProfitComparisonReportViewModel>();
        services.AddTransient<ProductMovementReportViewModel>();
        services.AddTransient<StockHealthReportViewModel>();
        services.AddTransient<MinimumQuantityReportViewModel>();
        services.AddTransient<ExpiryReportViewModel>();
        services.AddTransient<InventoryReplenishmentReportViewModel>();
        services.AddTransient<DeletedInvoicesReportViewModel>();
        services.AddTransient<DeletedVouchersReportViewModel>();
        services.AddTransient<DeletedProductsReportViewModel>();
        services.AddTransient<DeletedCustomersReportViewModel>();
        services.AddTransient<DeletedSuppliersReportViewModel>();
        services.AddTransient<DeletedExpensesReportViewModel>();
        services.AddTransient<InvoiceModificationsReportViewModel>();
        services.AddTransient<ProductModificationsReportViewModel>();
        services.AddTransient<InvestorProfitDistributionsReportViewModel>();
        services.AddTransient<CapitalMovementReportViewModel>();
        services.AddTransient<OpeningInstallmentBalancesReportViewModel>();
        services.AddTransient<CompanyFeeReportViewModel>();
        services.AddTransient<InstallmentScheduleReportViewModel>();
        services.AddTransient<SalesByPaymentMethodReportViewModel>();
        services.AddTransient<DailySalesReportViewModel>();
        services.AddTransient<WorkSummaryReportViewModel>();
        services.AddTransient<SalesByWarehouseUserReportViewModel>();
        services.AddTransient<GrossProfitMarginReportViewModel>();
        services.AddTransient<OperatingProfitReportViewModel>();
        services.AddTransient<ReceivablesAgingReportViewModel>();
        services.AddTransient<PayablesAgingReportViewModel>();
        services.AddTransient<CustomerCollectionsReportViewModel>();
        services.AddTransient<OverdueCustomersReportViewModel>();
        services.AddTransient<SupplierPaymentsReportViewModel>();
        services.AddTransient<BankAccountStatementReportViewModel>();
        services.AddTransient<CashBoxMovementReportViewModel>();
        services.AddTransient<CashBalancesSummaryReportViewModel>();
        services.AddTransient<TransfersReportViewModel>();
        services.AddTransient<InventoryValuationReportViewModel>();
        services.AddTransient<StockTakingReportViewModel>();
        services.AddTransient<CogsReportViewModel>();
        services.AddTransient<FinancialPositionSummaryReportViewModel>();
        services.AddTransient<ProfitAndLossReportViewModel>();
        services.AddTransient<StatementOfFinancialPositionReportViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<PermissionsViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<SetupWizardViewModel>();
        services.AddTransient<CapitalAdjustmentViewModel>();
        services.AddTransient<BackupRestoreViewModel>();
        services.AddTransient<CollectionDashboardViewModel>();
        services.AddTransient<BusinessFeaturesSettingsViewModel>();
        services.AddTransient<CustomFieldSettingsViewModel>();
        services.AddTransient<MigrationWizardViewModel>();
        services.AddTransient<WarehouseTransferViewModel>();
        services.AddTransient<PrintLayoutSettingsViewModel>();
        services.AddTransient<CloudSyncSettingsViewModel>();
        services.AddTransient<NetworkConnectionSettingsViewModel>();
        services.AddTransient<SystemUpdateViewModel>();
        services.AddTransient<DeveloperSystemSwitchViewModel>();
        services.AddTransient<HelpVideosViewModel>();

        services.AddTransient<CarDashboardViewModel>();
        services.AddTransient<CarContractFormViewModel>();
        services.AddTransient<CarContractsViewModel>();
        services.AddTransient<CarContractsReportViewModel>();

        services.AddTransient<RealEstateDashboardViewModel>();
        services.AddTransient<RealEstateContractFormViewModel>();
        services.AddTransient<RealEstateContractsViewModel>();
        services.AddTransient<RealEstateDebtsViewModel>();
        services.AddTransient<RealEstatePartiesViewModel>();
        services.AddTransient<RealEstateContractsReportViewModel>();
        services.AddTransient<RealEstateExpensesViewModel>();
        services.AddTransient<RealEstateProfitReportViewModel>();
        services.AddTransient<RealEstateClauseTemplatesViewModel>();

        services.AddTransient<CarTradeDashboardViewModel>();
        services.AddTransient<CarTradeFormViewModel>();
        services.AddTransient<CarTradeListViewModel>();
        services.AddTransient<CarTradeReportsViewModel>();
        services.AddTransient<CarTradePartyStatementViewModel>();

        services.AddTransient<HotelDashboardViewModel>();
        services.AddTransient<HotelReservationsViewModel>();
        services.AddTransient<HotelReservationsCalendarViewModel>();
        services.AddTransient<HotelReservationFormViewModel>();
        services.AddTransient<HotelCheckInOutViewModel>();
        services.AddTransient<HotelRoomsViewModel>();
        services.AddTransient<HotelRoomTypesViewModel>();
        services.AddTransient<HotelFloorsViewModel>();
        services.AddTransient<HotelGuestsViewModel>();
        services.AddTransient<HotelRatePlansViewModel>();
        services.AddTransient<HotelHousekeepingViewModel>();
        services.AddTransient<HotelCashViewModel>();
        services.AddTransient<HotelExpensesViewModel>();
        services.AddTransient<HotelReportsViewModel>();
        services.AddTransient<RestaurantPosViewModel>();
        services.AddTransient<RestaurantMenuViewModel>();
        services.AddTransient<RestaurantInventoryViewModel>();
        services.AddTransient<RestaurantTablesViewModel>();
        services.AddTransient<RestaurantReportsViewModel>();
        services.AddTransient<RestaurantKitchenViewModel>();
        services.AddTransient<HotelSetupWizardViewModel>();
        services.AddSingleton<ICarContractPrintService, CarContractPrintService>();
        services.AddSingleton<IRealEstateContractPrintService, RealEstateContractPrintService>();
        services.AddSingleton<ICarTradePrintService, CarTradePrintService>();
        services.AddSingleton<IHotelInvoicePrintService, HotelInvoicePrintService>();

        services.AddTransient<GoldDashboardViewModel>();
        services.AddTransient<GoldMithqalPricesViewModel>();
        services.AddTransient<GoldFxRatesViewModel>();
        services.AddTransient<GoldItemsViewModel>();
        services.AddTransient<GoldStockViewModel>();
        services.AddTransient<GoldStockAdjustmentViewModel>();
        services.AddTransient<GoldWarehousesViewModel>();
        services.AddTransient<GoldWarehouseTransferViewModel>();
        services.AddTransient<GoldSaleInvoiceViewModel>();
        services.AddTransient<GoldSaleReturnViewModel>();
        services.AddTransient<GoldCreditSalesViewModel>();
        services.AddTransient<GoldCollectionViewModel>();
        services.AddTransient<GoldExchangeInvoiceViewModel>();
        services.AddTransient<GoldPurchaseInvoiceViewModel>();
        services.AddTransient<GoldSuppliersViewModel>();
        services.AddTransient<GoldCustomersViewModel>();
        services.AddTransient<GoldCustomerStatementViewModel>();
        services.AddTransient<GoldOpeningStockViewModel>();
        services.AddTransient<GoldOpeningCustomerBalanceViewModel>();
        services.AddTransient<GoldCashBoxesViewModel>();
        services.AddTransient<GoldVouchersViewModel>();
        services.AddTransient<GoldExpensesViewModel>();
        services.AddTransient<GoldExpenseTypesViewModel>();
        services.AddTransient<GoldNotificationsViewModel>();
        services.AddTransient<GoldSettingsViewModel>();
        services.AddTransient<GoldStockReportViewModel>();
        services.AddTransient<GoldSalesReportViewModel>();
        services.AddTransient<GoldCreditReportViewModel>();
        services.AddTransient<GoldAgingReportViewModel>();
        services.AddTransient<GoldKaratMovementReportViewModel>();
        services.AddTransient<GoldProfitabilityReportViewModel>();
        services.AddTransient<GoldAuditReportViewModel>();
        services.AddTransient<GoldPurchasesReportViewModel>();

        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<HelpVideosWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        SplashWindow? splash = null;

        try
        {
            base.OnStartup(e);

            if (_systemProfile.IsFirstRun)
            {
                var wizard = new SetupWizardHostWindow();
                if (wizard.ShowDialog() != true || wizard.SelectedSystem is null)
                {
                    Shutdown();
                    return;
                }

                _systemProfile.SaveSelection(
                    wizard.SelectedSystem.Value,
                    wizard.SelectedDeploymentMode,
                    wizard.BranchDisplayName);

                // New installs only: start the desktop trial after first successful setup.
                _desktopLicense.StartTrial();
            }
            else
            {
                // Existing configured installs without a license file become Grandfathered (lifetime).
                _desktopLicense.EnsureInitialized(profileIsConfigured: true);
            }

            var licenseStatus = _desktopLicense.GetStatus();
            if (!licenseStatus.IsUsable)
            {
                var activation = new DesktopActivationWindow(_desktopLicense, licenseStatus, allowDismissWhileValid: false);
                if (activation.ShowDialog() != true || !_desktopLicense.IsUsable)
                {
                    Shutdown();
                    return;
                }
            }

            EnsureServiceProvider();

            PrintPreferences.Load();

            splash = new SplashWindow();
            splash.Show();
            splash.SetProgress(0.08);

            var minimumDisplay = Task.Delay(TimeSpan.FromMilliseconds(2600));
            Exception? startupError = null;

            var loadTask = RunStartupLoadAsync(splash, ex => startupError = ex);

            await Task.WhenAll(minimumDisplay, loadTask);

            if (startupError is UpdateShutdownException)
            {
                if (splash is not null)
                {
                    try { await splash.CloseAnimatedAsync(); } catch { /* ignore */ }
                }
                Shutdown();
                return;
            }

            if (startupError is not null)
            {
                await splash.CloseAnimatedAsync();
                ShowFatalError("خطأ في الاتصال بقاعدة البيانات", startupError);
                Shutdown();
                return;
            }

            splash.SetProgress(1);
            await splash.CloseAnimatedAsync();
            splash = null;

            await ShowLoginAndMainWindowAsync();
        }
        catch (Exception ex)
        {
            if (splash is not null)
            {
                try { splash.Close(); } catch { /* ignore */ }
            }

            ShowFatalError("فشل بدء تشغيل التطبيق", ex);
            Shutdown();
        }
    }

    private async Task RunStartupLoadAsync(SplashWindow splash, Action<Exception> onError)
    {
        try
        {
            splash.SetStatus("جاري التحقق من التحديثات...");
            splash.SetProgress(0.12);
            await Task.Yield();

            var updateApplied = await AppUpdateCoordinator.TryStartupUpdateAsync(
                _serviceProvider,
                splash.SetStatus);
            if (updateApplied)
            {
                onError(new UpdateShutdownException());
                return;
            }

            splash.SetStatus("جاري تهيئة الواجهة...");
            splash.SetProgress(0.2);
            await Task.Yield();

            try
            {
                ChartThemeConfig.Apply();
                ChartThemeHooks.Initialize();
            }
            catch (Exception ex)
            {
                LogException("ChartTheme", ex);
            }

            splash.SetStatus("جاري الاتصال بقاعدة البيانات...");
            splash.SetProgress(0.45);

            var isBranchClient = _systemProfile.IsBranchClient;

            await Task.Run(async () =>
            {
                if (isBranchClient)
                {
                    var networkService = new AlMuhasib.Infrastructure.Services.NetworkConnectionService();
                    var test = await networkService.TestCurrentConnectionAsync();
                    if (!test.Success)
                        throw new InvalidOperationException(test.Message);
                }

                using var scope = _serviceProvider.CreateScope();
                var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

                if (!isBranchClient)
                {
                    var pendingMigrations = await migrationService.GetPendingMigrationsAsync();
                    if (pendingMigrations.Count > 0)
                    {
                        splash.SetStatus(
                            pendingMigrations.Count == 1
                                ? "جاري تطبيق تحديث قاعدة البيانات..."
                                : $"جاري تطبيق {pendingMigrations.Count} تحديثات على قاعدة البيانات...");
                        splash.SetProgress(0.52);

                        var applied = await migrationService.ApplyPendingMigrationsAsync();
                        System.Diagnostics.Debug.WriteLine(
                            $"[Startup] Applied migrations: {string.Join(", ", applied)}");
                    }

                    splash.SetStatus("جاري تهيئة الحسابات والإعدادات...");
                    splash.SetProgress(0.62);

                    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    await authService.EnsureAdminAccountAsync();
                }
                else
                {
                    splash.SetStatus("تم التحقق من الاتصال بالحاسبة الرئيسية...");
                    splash.SetProgress(0.62);
                }

                var brandingService = scope.ServiceProvider.GetRequiredService<IPrintBrandingService>();
                await brandingService.RefreshProviderAsync();
            });

            splash.SetStatus("جاري إعداد النظام...");
            splash.SetProgress(0.85);
            await Task.Delay(200);

            splash.SetStatus("اكتمل التحميل");
            splash.SetProgress(0.95);
        }
        catch (Exception ex)
        {
            onError(ex);
        }
    }

    private sealed class UpdateShutdownException : Exception;

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

        try
        {
            if (currentUser.UserId is int uid)
            {
                using var logScope = _serviceProvider.CreateScope();
                var loginLog = logScope.ServiceProvider.GetRequiredService<IUserLoginLogService>();
                await loginLog.LogLoginAsync(uid, currentUser.Username);
            }
        }
        catch { /* ignore login log failures */ }

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

        try
        {
            mainVm.CloseAllTabs();

            // Check if initial setup is needed
            bool needsSetup = false;
            try
            {
                if (!_systemProfile.IsBranchClient && _systemProfile.ActiveSystem == ApplicationSystemType.Accounting)
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    needsSetup = currentUser.IsAdmin && !await uow.CapitalEntries.AnyAsync();
                }
                else if (!_systemProfile.IsBranchClient && _systemProfile.ActiveSystem == ApplicationSystemType.HotelManagement)
                {
                    using var scope = _serviceProvider!.CreateScope();
                    var hotelSettings = scope.ServiceProvider.GetRequiredService<IHotelSettingsService>();
                    needsSetup = currentUser.IsAdmin && !await hotelSettings.IsConfiguredAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Startup] Setup check error: {ex}");
            }

            if (needsSetup && _systemProfile.ActiveSystem == ApplicationSystemType.Accounting)
            {
                await mainVm.OpenTabAsync(typeof(SetupWizardViewModel), "إعداد النظام", PackIconKind.CogOutline, activateIfExists: false);
                if (mainVm.CurrentViewModel is SetupWizardViewModel wizardVm)
                {
                    wizardVm.SetupCompleted += async () =>
                    {
                        mainVm.CloseAllTabs();
                        await mainVm.OpenInitialSessionTabsAsync();
                        mainVm.TryStartFeatureTour();
                        _ = mainVm.InitializeNotificationCenterAsync();
                        _ = mainVm.InitializePersonalWorkspaceAsync();
                    };
                }
            }
            else if (needsSetup && _systemProfile.ActiveSystem == ApplicationSystemType.HotelManagement)
            {
                await mainVm.OpenTabAsync(typeof(HotelSetupWizardViewModel), "إعداد الفندق", PackIconKind.Hotel, activateIfExists: false);
                if (mainVm.CurrentViewModel is not HotelSetupWizardViewModel)
                {
                    await mainVm.OpenInitialSessionTabsAsync();
                }
                else if (mainVm.CurrentViewModel is HotelSetupWizardViewModel hotelWizardVm)
                {
                    hotelWizardVm.SetupCompleted += async () =>
                    {
                        mainVm.CloseAllTabs();
                        await mainVm.OpenInitialSessionTabsAsync();
                        mainVm.TryStartFeatureTour();
                        _ = mainVm.InitializeNotificationCenterAsync();
                        _ = mainVm.InitializePersonalWorkspaceAsync();
                    };
                }
            }
            else
            {
                await mainVm.OpenInitialSessionTabsAsync();
                mainVm.TryStartFeatureTour();
            }

            _serviceProvider.GetRequiredService<BackupSchedulerService>().Start();

            if (_systemProfile.IsMainServer)
            {
                try
                {
                    var hosting = _serviceProvider.GetRequiredService<IMainServerHostingService>();
                    await hosting.StartDiscoveryResponderAsync(
                        _systemProfile.ActiveSystem,
                        _systemProfile.ActiveDatabaseName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Startup] Discovery responder error: {ex}");
                }
            }

            _ = mainVm.InitializeNotificationCenterAsync();
            _ = mainVm.InitializePersonalWorkspaceAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Startup] Navigation error: {ex}");
            BeautifulMessageDialog.ShowWarning(
                $"خطأ في التنقل إلى لوحة التحكم:\n\n{ex.InnerException?.Message ?? ex.Message}");

            try
            {
                await mainVm.OpenInitialSessionTabsAsync();
                _ = mainVm.InitializeNotificationCenterAsync();
                _ = mainVm.InitializePersonalWorkspaceAsync();
            }
            catch { }
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
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
