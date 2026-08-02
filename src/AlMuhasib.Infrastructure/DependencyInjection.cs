using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Infrastructure.Data.Car;
using AlMuhasib.Infrastructure.Data.CarTrade;
using AlMuhasib.Infrastructure.Data.Gold;
using AlMuhasib.Infrastructure.Data.Hotel;
using AlMuhasib.Infrastructure.Data.RealEstate;
using AlMuhasib.Infrastructure.Repositories;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.Infrastructure.Services.Car;
using AlMuhasib.Infrastructure.Services.CarTrade;
using AlMuhasib.Infrastructure.Services.Gold;
using AlMuhasib.Infrastructure.Services.Hotel;
using AlMuhasib.Infrastructure.Services.Hotel.Restaurant;
using AlMuhasib.Infrastructure.Services.RealEstate;
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
        var networkConnectionService = new NetworkConnectionService();
        services.AddSingleton<INetworkConnectionService>(networkConnectionService);
        services.AddSingleton<IMainServerHostingService, MainServerHostingService>();
        services.AddSingleton<ISqlServerInstanceDiscoveryService, SqlServerInstanceDiscoveryService>();
        services.AddSingleton<IAppSettingsConnectionStore, AppSettingsConnectionStore>();

        var connectionString = SystemConnectionStrings.Build(configuration, systemProfile, networkConnectionService);
        var isBranchClient = systemProfile.IsBranchClient;

        switch (systemProfile.ActiveSystem)
        {
            case ApplicationSystemType.CarContracts:
                RegisterCarInfrastructure(services, connectionString, isBranchClient);
                break;
            case ApplicationSystemType.HotelManagement:
                RegisterHotelInfrastructure(services, connectionString, isBranchClient);
                break;
            case ApplicationSystemType.CarTrading:
                RegisterCarTradeInfrastructure(services, connectionString, isBranchClient);
                break;
            case ApplicationSystemType.RealEstateContracts:
                RegisterRealEstateInfrastructure(services, connectionString, isBranchClient);
                break;
            case ApplicationSystemType.GoldShop:
                RegisterGoldShopInfrastructure(services, connectionString, isBranchClient);
                break;
            default:
                RegisterAccountingInfrastructure(services, connectionString, isBranchClient);
                break;
        }

        services.AddSingleton<IBackupService, BackupService>();
        return services;
    }

    private static void RegisterAccountingInfrastructure(IServiceCollection services, string connectionString, bool isBranchClient)
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
        services.AddScoped<IPricingTypeService, PricingTypeService>();
        services.AddScoped<IPackagingTypeService, PackagingTypeService>();
        services.AddScoped<IProductPriceService, ProductPriceService>();
        services.AddScoped<IBusinessSettingsService, BusinessSettingsService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<ICashBankService, CashBankService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IInvestorService, InvestorService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISupervisoryReportService, SupervisoryReportService>();
        services.AddScoped<IPersonProfileService, PersonProfileService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAccountingValidationService, AccountingValidationService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        if (isBranchClient)
            services.AddSingleton<IDatabaseMigrationService, NoOpDatabaseMigrationService>();
        else
            services.AddSingleton<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<IGlobalSearchService, GlobalSearchService>();
        services.AddScoped<ISmartAlertService, SmartAlertService>();
        services.AddScoped<ICollectionDashboardService, CollectionDashboardService>();
        services.AddScoped<ICustomerStatementQuickService, CustomerStatementQuickService>();
        services.AddScoped<ICustomerCreditService, CustomerCreditService>();
        services.AddScoped<ILocalQueryService, LocalQueryService>();
        services.AddScoped<IWarehouseTransferService, WarehouseTransferService>();
        services.AddScoped<IProductUnitService, ProductUnitService>();
        services.AddScoped<IProductBatchService, ProductBatchService>();
        services.AddScoped<IProductSerialService, ProductSerialService>();
        services.AddScoped<IProductSizeService, ProductSizeService>();
        services.AddScoped<IProductColorService, ProductColorService>();
        services.AddScoped<IUserLoginLogService, UserLoginLogService>();
        services.AddScoped<IDataImportService, DataImportService>();
        services.AddScoped<IDemoDataService, DemoDataService>();
        services.AddScoped<IUserTaskService, UserTaskService>();
        services.AddScoped<IUserNoteService, UserNoteService>();
        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService<AppDbContext>>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddHttpClient("CloudSync");
    }

    private static void RegisterCarInfrastructure(IServiceCollection services, string connectionString, bool isBranchClient)
    {
        services.AddDbContextFactory<CarDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(CarDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, CarUnitOfWork>();
        services.AddScoped<IAuthService, CarAuthService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        if (isBranchClient)
            services.AddSingleton<IDatabaseMigrationService, NoOpDatabaseMigrationService>();
        else
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
        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService<CarDbContext>>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, CarSyncService>();
        services.AddHttpClient("CloudSync");
    }

    private static void RegisterHotelInfrastructure(IServiceCollection services, string connectionString, bool isBranchClient)
    {
        services.AddDbContextFactory<HotelDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(HotelDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, HotelUnitOfWork>();
        services.AddScoped<IAuthService, HotelAuthService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        if (isBranchClient)
            services.AddSingleton<IDatabaseMigrationService, NoOpDatabaseMigrationService>();
        else
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

        services.AddScoped<IRestaurantInventoryService, RestaurantInventoryService>();
        services.AddScoped<IRestaurantMenuService, RestaurantMenuService>();
        services.AddScoped<IRestaurantOrderService, RestaurantOrderService>();
        services.AddScoped<IRestaurantTableService, RestaurantTableService>();
        services.AddScoped<IRestaurantReportService, RestaurantReportService>();

        services.AddScoped<IUserLoginLogService, NoOpUserLoginLogService>();
        services.AddScoped<ISmartAlertService, HotelSmartAlertBridge>();
        services.AddScoped<IUserTaskService, NoOpUserTaskService>();
        services.AddScoped<IUserNoteService, NoOpUserNoteService>();
        services.AddScoped<ICustomerStatementQuickService, NoOpCustomerStatementQuickService>();

        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService<HotelDbContext>>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, HotelSyncService>();
        services.AddHttpClient("CloudSync");
    }

    private static void RegisterCarTradeInfrastructure(IServiceCollection services, string connectionString, bool isBranchClient)
    {
        services.AddDbContextFactory<CarTradeDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(CarTradeDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, CarTradeUnitOfWork>();
        services.AddScoped<IAuthService, CarTradeAuthService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        if (isBranchClient)
            services.AddSingleton<IDatabaseMigrationService, NoOpDatabaseMigrationService>();
        else
            services.AddSingleton<IDatabaseMigrationService, CarTradeDatabaseMigrationService>();
        services.AddScoped<ICarTradeService, CarTradeService>();
        services.AddScoped<ICarTradeReportService, CarTradeReportService>();
        services.AddScoped<IGlobalSearchService, CarTradeGlobalSearchService>();
        services.AddScoped<IAuditLogService, CarTradeAuditLogService>();
        services.AddScoped<IUserLoginLogService, NoOpUserLoginLogService>();
        services.AddScoped<ISmartAlertService, NoOpSmartAlertService>();
        services.AddScoped<IUserTaskService, NoOpUserTaskService>();
        services.AddScoped<IUserNoteService, NoOpUserNoteService>();
        services.AddScoped<ICustomerStatementQuickService, NoOpCustomerStatementQuickService>();
        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService<CarTradeDbContext>>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, CarTradeSyncService>();
        services.AddHttpClient("CloudSync");
    }

    private static void RegisterRealEstateInfrastructure(IServiceCollection services, string connectionString, bool isBranchClient)
    {
        services.AddDbContextFactory<RealEstateDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(RealEstateDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, RealEstateUnitOfWork>();
        services.AddScoped<IAuthService, RealEstateAuthService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        if (isBranchClient)
            services.AddSingleton<IDatabaseMigrationService, NoOpDatabaseMigrationService>();
        else
            services.AddSingleton<IDatabaseMigrationService, RealEstateDatabaseMigrationService>();
        services.AddScoped<IRealEstateContractService, RealEstateContractService>();
        services.AddScoped<IRealEstateContractReportService, RealEstateContractReportService>();
        services.AddScoped<IRealEstateClauseTemplateService, RealEstateClauseTemplateService>();
        services.AddScoped<IRealEstatePartyService, RealEstatePartyService>();
        services.AddScoped<IRealEstateExpenseService, RealEstateExpenseService>();
        services.AddScoped<IGlobalSearchService, RealEstateGlobalSearchService>();
        services.AddScoped<IAuditLogService, RealEstateAuditLogService>();
        services.AddScoped<IUserLoginLogService, NoOpUserLoginLogService>();
        services.AddScoped<ISmartAlertService, NoOpSmartAlertService>();
        services.AddScoped<IUserTaskService, NoOpUserTaskService>();
        services.AddScoped<IUserNoteService, NoOpUserNoteService>();
        services.AddScoped<ICustomerStatementQuickService, NoOpCustomerStatementQuickService>();
        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService<RealEstateDbContext>>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, RealEstateSyncService>();
        services.AddHttpClient("CloudSync");
    }

    private static void RegisterGoldShopInfrastructure(IServiceCollection services, string connectionString, bool isBranchClient)
    {
        services.AddDbContextFactory<GoldDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly(typeof(GoldDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, GoldUnitOfWork>();
        services.AddScoped<IAuthService, GoldAuthService>();
        services.AddScoped<IPrintBrandingService, PrintBrandingService>();
        if (isBranchClient)
            services.AddSingleton<IDatabaseMigrationService, NoOpDatabaseMigrationService>();
        else
            services.AddSingleton<IDatabaseMigrationService, GoldDatabaseMigrationService>();
        services.AddScoped<IGoldSettingsService, GoldSettingsService>();
        services.AddScoped<IGoldPricingService, GoldPricingService>();
        services.AddScoped<IGoldInventoryService, GoldInventoryService>();
        services.AddScoped<IGoldCustomerService, GoldCustomerService>();
        services.AddScoped<IGoldCashService, GoldCashService>();
        services.AddScoped<IGoldSaleService, GoldSaleService>();
        services.AddScoped<IGoldPurchaseService, GoldPurchaseService>();
        services.AddScoped<IGoldDashboardService, GoldDashboardService>();
        services.AddScoped<IGoldReportService, GoldReportService>();
        services.AddScoped<IGlobalSearchService, GoldGlobalSearchService>();
        services.AddScoped<IAuditLogService, GoldAuditLogService>();
        services.AddScoped<IUserLoginLogService, NoOpUserLoginLogService>();
        services.AddScoped<ISmartAlertService, NoOpSmartAlertService>();
        services.AddScoped<IUserTaskService, NoOpUserTaskService>();
        services.AddScoped<IUserNoteService, NoOpUserNoteService>();
        services.AddScoped<ICustomerStatementQuickService, NoOpCustomerStatementQuickService>();
        services.AddScoped<ICloudSyncSettingsService, CloudSyncSettingsService<GoldDbContext>>();
        services.AddScoped<SyncApiClient>();
        services.AddSingleton<ISyncService, GoldSyncService>();
        services.AddHttpClient("CloudSync");
    }
}
