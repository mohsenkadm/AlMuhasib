using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.Car;
using AlMuhasib.Infrastructure.Repositories.Car;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlMuhasib.Infrastructure.Repositories;

public sealed class CarUnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<CarDbContext> _contextFactory;
    private CarDbContext? _activeContext;
    private IDbContextTransaction? _transaction;

    public CarUnitOfWork(IDbContextFactory<CarDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private CarDbContext? GetActiveContext() => _activeContext;

    private IRepository<User>? _users;
    private IRepository<Permission>? _permissions;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<PrintBrandingSettings>? _printBrandingSettings;

    public IRepository<User> Users => _users ??= new CarRepository<User>(_contextFactory, GetActiveContext);
    public IRepository<Permission> Permissions => _permissions ??= new CarRepository<Permission>(_contextFactory, GetActiveContext);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new CarRepository<AuditLog>(_contextFactory, GetActiveContext);
    public IRepository<PrintBrandingSettings> PrintBrandingSettings => _printBrandingSettings ??= new CarRepository<PrintBrandingSettings>(_contextFactory, GetActiveContext);

    public IRepository<Category> Categories => new UnsupportedCarRepository<Category>();
    public IRepository<Product> Products => new UnsupportedCarRepository<Product>();
    public IRepository<Customer> Customers => new UnsupportedCarRepository<Customer>();
    public IRepository<Supplier> Suppliers => new UnsupportedCarRepository<Supplier>();
    public IRepository<Warehouse> Warehouses => new UnsupportedCarRepository<Warehouse>();
    public IRepository<WarehouseStock> WarehouseStocks => new UnsupportedCarRepository<WarehouseStock>();
    public IRepository<CashBox> CashBoxes => new UnsupportedCarRepository<CashBox>();
    public IRepository<BankAccount> BankAccounts => new UnsupportedCarRepository<BankAccount>();
    public IRepository<Investor> Investors => new UnsupportedCarRepository<Investor>();
    public IRepository<ExpenseType> ExpenseTypes => new UnsupportedCarRepository<ExpenseType>();
    public IRepository<Invoice> Invoices => new UnsupportedCarRepository<Invoice>();
    public IRepository<InvoiceItem> InvoiceItems => new UnsupportedCarRepository<InvoiceItem>();
    public IRepository<InstallmentPlan> InstallmentPlans => new UnsupportedCarRepository<InstallmentPlan>();
    public IRepository<Installment> Installments => new UnsupportedCarRepository<Installment>();
    public IRepository<Voucher> Vouchers => new UnsupportedCarRepository<Voucher>();
    public IRepository<Expense> Expenses => new UnsupportedCarRepository<Expense>();
    public IRepository<Transfer> Transfers => new UnsupportedCarRepository<Transfer>();
    public IRepository<InvestorTransaction> InvestorTransactions => new UnsupportedCarRepository<InvestorTransaction>();
    public IRepository<ProfitDistribution> ProfitDistributions => new UnsupportedCarRepository<ProfitDistribution>();
    public IRepository<ProfitDistributionDetail> ProfitDistributionDetails => new UnsupportedCarRepository<ProfitDistributionDetail>();
    public IRepository<CapitalEntry> CapitalEntries => new UnsupportedCarRepository<CapitalEntry>();
    public IRepository<CustomerAttachment> CustomerAttachments => new UnsupportedCarRepository<CustomerAttachment>();
    public IRepository<UserTask> UserTasks => new UnsupportedCarRepository<UserTask>();
    public IRepository<UserNote> UserNotes => new UnsupportedCarRepository<UserNote>();
    public IRepository<PricingType> PricingTypes => new UnsupportedCarRepository<PricingType>();
    public IRepository<ProductPrice> ProductPrices => new UnsupportedCarRepository<ProductPrice>();
    public IRepository<BusinessSettings> BusinessSettings => new UnsupportedCarRepository<BusinessSettings>();

    public async Task<int> SaveChangesAsync()
    {
        if (_activeContext is null)
            return 0;

        return await _activeContext.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _activeContext ??= await _contextFactory.CreateDbContextAsync();
        _transaction = await _activeContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_activeContext is null || _transaction is null)
            return;

        await _activeContext.SaveChangesAsync();
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
        await _activeContext.DisposeAsync();
        _activeContext = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_activeContext is not null)
        {
            await _activeContext.DisposeAsync();
            _activeContext = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _activeContext?.Dispose();
    }
}
