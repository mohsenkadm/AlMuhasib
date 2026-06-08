using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Infrastructure.Repositories;
using AlMuhasib.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
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
        services.AddSingleton<IBackupService, BackupService>();
        services.AddScoped<IGlobalSearchService, GlobalSearchService>();
        services.AddScoped<ISmartAlertService, SmartAlertService>();
        services.AddScoped<IDemoDataService, DemoDataService>();
        services.AddScoped<IUserTaskService, UserTaskService>();
        services.AddScoped<IUserNoteService, UserNoteService>();

        return services;
    }
}
