using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Infrastructure.Data.Car;
using AlMuhasib.Infrastructure.Data.Hotel;
using AlMuhasib.Infrastructure.Repositories;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.Infrastructure.Services.Hotel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        ISystemProfileService systemProfile)
    {
        var connectionString = SystemConnectionStrings.Build(configuration, systemProfile.ActiveSystem);

        switch (systemProfile.ActiveSystem)
        {
            case ApplicationSystemType.CarContracts:
                RegisterCarInfrastructure(services, connectionString);
                break;
            case ApplicationSystemType.HotelManagement:
                RegisterHotelInfrastructure(services, connectionString);
                break;
            default:
                RegisterAccountingInfrastructure(services, connectionString);
                break;
        }

        services.AddSingleton<IBackupService, BackupService>();
        return services;
    }

    private static void RegisterAccountingInfrastructure(IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<ICashBankService, CashBankService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IInvestorService, InvestorService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAccountingValidationService, AccountingValidationService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        services.AddSingleton<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<IGlobalSearchService, GlobalSearchService>();
        services.AddScoped<ISmartAlertService, SmartAlertService>();
        services.AddScoped<ICollectionDashboardService, CollectionDashboardService>();
        services.AddScoped<ICustomerStatementQuickService, CustomerStatementQuickService>();
        services.AddScoped<ICustomerCreditService, CustomerCreditService>();
        services.AddScoped<ILocalQueryService, LocalQueryService>();
        services.AddScoped<IWarehouseTransferService, WarehouseTransferService>();
        services.AddScoped<IUserLoginLogService, UserLoginLogService>();
        services.AddScoped<IDataImportService, DataImportService>();
        services.AddScoped<IDemoDataService, DemoDataService>();
        services.AddScoped<IUserTaskService, UserTaskService>();
        services.AddScoped<IUserNoteService, UserNoteService>();
        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddHttpClient("CloudSync");
    }

    private static void RegisterCarInfrastructure(IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<CarDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(CarDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, CarUnitOfWork>();
        services.AddScoped<IAuthService, CarAuthService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        services.AddSingleton<IDatabaseMigrationService, CarDatabaseMigrationService>();
        services.AddScoped<ICarContractService, CarContractService>();
        services.AddScoped<ICarContractReportService, CarContractReportService>();
        services.AddScoped<IGlobalSearchService, CarGlobalSearchService>();
        services.AddScoped<IAuditLogService, CarAuditLogService>();
        services.AddScoped<IUserLoginLogService, NoOpUserLoginLogService>();
        services.AddScoped<ISmartAlertService, NoOpSmartAlertService>();
        services.AddScoped<IUserTaskService, NoOpUserTaskService>();
        services.AddScoped<IUserNoteService, NoOpUserNoteService>();
        services.AddScoped<ICustomerStatementQuickService, NoOpCustomerStatementQuickService>();
    }

    private static void RegisterHotelInfrastructure(IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<HotelDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(HotelDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, HotelUnitOfWork>();
        services.AddScoped<IAuthService, HotelAuthService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        services.AddSingleton<IDatabaseMigrationService, HotelDatabaseMigrationService>();
        services.AddScoped<IGlobalSearchService, HotelGlobalSearchService>();
        services.AddScoped<IHotelGlobalSearchService, HotelGlobalSearchService>();
        services.AddScoped<IAuditLogService, HotelAuditLogService>();

        services.AddScoped<IHotelSettingsService, HotelSettingsService>();
        services.AddScoped<IHotelMasterDataService, HotelMasterDataService>();
        services.AddScoped<IGuestService, HotelGuestService>();
        services.AddScoped<IReservationService, HotelReservationService>();
        services.AddScoped<ICheckInOutService, HotelCheckInOutService>();
        services.AddScoped<IReservationPaymentService, HotelReservationPaymentService>();
        services.AddScoped<IHotelCashService, HotelCashService>();
        services.AddScoped<IHotelExpenseService, HotelExpenseService>();
        services.AddScoped<IRatePlanService, HotelRatePlanService>();
        services.AddScoped<IHousekeepingService, HotelHousekeepingService>();
        services.AddScoped<IHotelDashboardService, HotelDashboardService>();
        services.AddScoped<IHotelReportService, HotelReportService>();
        services.AddScoped<IHotelSmartAlertService, HotelSmartAlertService>();

        services.AddScoped<IUserLoginLogService, NoOpUserLoginLogService>();
        services.AddScoped<ISmartAlertService, HotelSmartAlertBridge>();
        services.AddScoped<IUserTaskService, NoOpUserTaskService>();
        services.AddScoped<IUserNoteService, NoOpUserNoteService>();
        services.AddScoped<ICustomerStatementQuickService, NoOpCustomerStatementQuickService>();

        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, HotelSyncService>();
        services.AddHttpClient("CloudSync");
    }
}
