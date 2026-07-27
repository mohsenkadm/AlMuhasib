using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.RealEstate;
using AlMuhasib.Infrastructure.Repositories.RealEstate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlMuhasib.Infrastructure.Repositories;

public sealed class RealEstateUnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;
    private RealEstateDbContext? _activeContext;
    private IDbContextTransaction? _transaction;

    public RealEstateUnitOfWork(IDbContextFactory<RealEstateDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private RealEstateDbContext? GetActiveContext() => _activeContext;

    private IRepository<User>? _users;
    private IRepository<Permission>? _permissions;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<PrintBrandingSettings>? _printBrandingSettings;

    public IRepository<User> Users => _users ??= new RealEstateRepository<User>(_contextFactory, GetActiveContext);
    public IRepository<Permission> Permissions => _permissions ??= new RealEstateRepository<Permission>(_contextFactory, GetActiveContext);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new RealEstateRepository<AuditLog>(_contextFactory, GetActiveContext);
    public IRepository<PrintBrandingSettings> PrintBrandingSettings => _printBrandingSettings ??= new RealEstateRepository<PrintBrandingSettings>(_contextFactory, GetActiveContext);

    public IRepository<Category> Categories => new UnsupportedRealEstateRepository<Category>();
    public IRepository<Product> Products => new UnsupportedRealEstateRepository<Product>();
    public IRepository<Customer> Customers => new UnsupportedRealEstateRepository<Customer>();
    public IRepository<Supplier> Suppliers => new UnsupportedRealEstateRepository<Supplier>();
    public IRepository<Warehouse> Warehouses => new UnsupportedRealEstateRepository<Warehouse>();
    public IRepository<WarehouseStock> WarehouseStocks => new UnsupportedRealEstateRepository<WarehouseStock>();
    public IRepository<CashBox> CashBoxes => new UnsupportedRealEstateRepository<CashBox>();
    public IRepository<BankAccount> BankAccounts => new UnsupportedRealEstateRepository<BankAccount>();
    public IRepository<Investor> Investors => new UnsupportedRealEstateRepository<Investor>();
    public IRepository<ExpenseType> ExpenseTypes => new UnsupportedRealEstateRepository<ExpenseType>();
    public IRepository<Invoice> Invoices => new UnsupportedRealEstateRepository<Invoice>();
    public IRepository<InvoiceItem> InvoiceItems => new UnsupportedRealEstateRepository<InvoiceItem>();
    public IRepository<InstallmentPlan> InstallmentPlans => new UnsupportedRealEstateRepository<InstallmentPlan>();
    public IRepository<Installment> Installments => new UnsupportedRealEstateRepository<Installment>();
    public IRepository<Voucher> Vouchers => new UnsupportedRealEstateRepository<Voucher>();
    public IRepository<Expense> Expenses => new UnsupportedRealEstateRepository<Expense>();
    public IRepository<Transfer> Transfers => new UnsupportedRealEstateRepository<Transfer>();
    public IRepository<InvestorTransaction> InvestorTransactions => new UnsupportedRealEstateRepository<InvestorTransaction>();
    public IRepository<ProfitDistribution> ProfitDistributions => new UnsupportedRealEstateRepository<ProfitDistribution>();
    public IRepository<ProfitDistributionDetail> ProfitDistributionDetails => new UnsupportedRealEstateRepository<ProfitDistributionDetail>();
    public IRepository<CapitalEntry> CapitalEntries => new UnsupportedRealEstateRepository<CapitalEntry>();
    public IRepository<CustomerAttachment> CustomerAttachments => new UnsupportedRealEstateRepository<CustomerAttachment>();
    public IRepository<UserTask> UserTasks => new UnsupportedRealEstateRepository<UserTask>();
    public IRepository<UserNote> UserNotes => new UnsupportedRealEstateRepository<UserNote>();
    public IRepository<PricingType> PricingTypes => new UnsupportedRealEstateRepository<PricingType>();
    public IRepository<ProductPrice> ProductPrices => new UnsupportedRealEstateRepository<ProductPrice>();
    public IRepository<BusinessSettings> BusinessSettings => new UnsupportedRealEstateRepository<BusinessSettings>();

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
