using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.Gold;
using AlMuhasib.Infrastructure.Repositories.Gold;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlMuhasib.Infrastructure.Repositories;

public sealed class GoldUnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;
    private GoldDbContext? _activeContext;
    private IDbContextTransaction? _transaction;

    public GoldUnitOfWork(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private GoldDbContext? GetActiveContext() => _activeContext;

    private IRepository<User>? _users;
    private IRepository<Permission>? _permissions;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<PrintBrandingSettings>? _printBrandingSettings;

    public IRepository<User> Users => _users ??= new GoldRepository<User>(_contextFactory, GetActiveContext);
    public IRepository<Permission> Permissions => _permissions ??= new GoldRepository<Permission>(_contextFactory, GetActiveContext);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new GoldRepository<AuditLog>(_contextFactory, GetActiveContext);
    public IRepository<PrintBrandingSettings> PrintBrandingSettings => _printBrandingSettings ??= new GoldRepository<PrintBrandingSettings>(_contextFactory, GetActiveContext);

    public IRepository<Category> Categories => new UnsupportedGoldRepository<Category>();
    public IRepository<Product> Products => new UnsupportedGoldRepository<Product>();
    public IRepository<Customer> Customers => new UnsupportedGoldRepository<Customer>();
    public IRepository<Driver> Drivers => new UnsupportedGoldRepository<Driver>();
    public IRepository<Supplier> Suppliers => new UnsupportedGoldRepository<Supplier>();
    public IRepository<Warehouse> Warehouses => new UnsupportedGoldRepository<Warehouse>();
    public IRepository<WarehouseStock> WarehouseStocks => new UnsupportedGoldRepository<WarehouseStock>();
    public IRepository<CashBox> CashBoxes => new UnsupportedGoldRepository<CashBox>();
    public IRepository<BankAccount> BankAccounts => new UnsupportedGoldRepository<BankAccount>();
    public IRepository<Investor> Investors => new UnsupportedGoldRepository<Investor>();
    public IRepository<ExpenseType> ExpenseTypes => new UnsupportedGoldRepository<ExpenseType>();
    public IRepository<Invoice> Invoices => new UnsupportedGoldRepository<Invoice>();
    public IRepository<InvoiceItem> InvoiceItems => new UnsupportedGoldRepository<InvoiceItem>();
    public IRepository<InstallmentPlan> InstallmentPlans => new UnsupportedGoldRepository<InstallmentPlan>();
    public IRepository<Installment> Installments => new UnsupportedGoldRepository<Installment>();
    public IRepository<Voucher> Vouchers => new UnsupportedGoldRepository<Voucher>();
    public IRepository<Expense> Expenses => new UnsupportedGoldRepository<Expense>();
    public IRepository<Transfer> Transfers => new UnsupportedGoldRepository<Transfer>();
    public IRepository<InvestorTransaction> InvestorTransactions => new UnsupportedGoldRepository<InvestorTransaction>();
    public IRepository<ProfitDistribution> ProfitDistributions => new UnsupportedGoldRepository<ProfitDistribution>();
    public IRepository<ProfitDistributionDetail> ProfitDistributionDetails => new UnsupportedGoldRepository<ProfitDistributionDetail>();
    public IRepository<CapitalEntry> CapitalEntries => new UnsupportedGoldRepository<CapitalEntry>();
    public IRepository<CustomerAttachment> CustomerAttachments => new UnsupportedGoldRepository<CustomerAttachment>();
    public IRepository<UserTask> UserTasks => new UnsupportedGoldRepository<UserTask>();
    public IRepository<UserNote> UserNotes => new UnsupportedGoldRepository<UserNote>();
    public IRepository<PricingType> PricingTypes => new UnsupportedGoldRepository<PricingType>();
    public IRepository<ProductPrice> ProductPrices => new UnsupportedGoldRepository<ProductPrice>();
    public IRepository<BusinessSettings> BusinessSettings => new UnsupportedGoldRepository<BusinessSettings>();

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
