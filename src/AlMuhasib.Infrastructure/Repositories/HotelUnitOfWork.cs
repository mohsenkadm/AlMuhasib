using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data.Hotel;
using AlMuhasib.Infrastructure.Repositories.Hotel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlMuhasib.Infrastructure.Repositories;

public sealed class HotelUnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;
    private HotelDbContext? _activeContext;
    private IDbContextTransaction? _transaction;

    public HotelUnitOfWork(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private HotelDbContext? GetActiveContext() => _activeContext;

    private IRepository<User>? _users;
    private IRepository<Permission>? _permissions;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<PrintBrandingSettings>? _printBrandingSettings;

    public IRepository<User> Users => _users ??= new HotelRepository<User>(_contextFactory, GetActiveContext);
    public IRepository<Permission> Permissions => _permissions ??= new HotelRepository<Permission>(_contextFactory, GetActiveContext);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new HotelRepository<AuditLog>(_contextFactory, GetActiveContext);
    public IRepository<PrintBrandingSettings> PrintBrandingSettings => _printBrandingSettings ??= new HotelRepository<PrintBrandingSettings>(_contextFactory, GetActiveContext);

    public IRepository<Category> Categories => new UnsupportedHotelRepository<Category>();
    public IRepository<Product> Products => new UnsupportedHotelRepository<Product>();
    public IRepository<Customer> Customers => new UnsupportedHotelRepository<Customer>();
    public IRepository<Driver> Drivers => new UnsupportedHotelRepository<Driver>();
    public IRepository<Supplier> Suppliers => new UnsupportedHotelRepository<Supplier>();
    public IRepository<Warehouse> Warehouses => new UnsupportedHotelRepository<Warehouse>();
    public IRepository<WarehouseStock> WarehouseStocks => new UnsupportedHotelRepository<WarehouseStock>();
    public IRepository<CashBox> CashBoxes => new UnsupportedHotelRepository<CashBox>();
    public IRepository<BankAccount> BankAccounts => new UnsupportedHotelRepository<BankAccount>();
    public IRepository<Investor> Investors => new UnsupportedHotelRepository<Investor>();
    public IRepository<ExpenseType> ExpenseTypes => new UnsupportedHotelRepository<ExpenseType>();
    public IRepository<Invoice> Invoices => new UnsupportedHotelRepository<Invoice>();
    public IRepository<InvoiceItem> InvoiceItems => new UnsupportedHotelRepository<InvoiceItem>();
    public IRepository<InstallmentPlan> InstallmentPlans => new UnsupportedHotelRepository<InstallmentPlan>();
    public IRepository<Installment> Installments => new UnsupportedHotelRepository<Installment>();
    public IRepository<Voucher> Vouchers => new UnsupportedHotelRepository<Voucher>();
    public IRepository<Expense> Expenses => new UnsupportedHotelRepository<Expense>();
    public IRepository<Transfer> Transfers => new UnsupportedHotelRepository<Transfer>();
    public IRepository<InvestorTransaction> InvestorTransactions => new UnsupportedHotelRepository<InvestorTransaction>();
    public IRepository<ProfitDistribution> ProfitDistributions => new UnsupportedHotelRepository<ProfitDistribution>();
    public IRepository<ProfitDistributionDetail> ProfitDistributionDetails => new UnsupportedHotelRepository<ProfitDistributionDetail>();
    public IRepository<CapitalEntry> CapitalEntries => new UnsupportedHotelRepository<CapitalEntry>();
    public IRepository<CustomerAttachment> CustomerAttachments => new UnsupportedHotelRepository<CustomerAttachment>();
    public IRepository<UserTask> UserTasks => new UnsupportedHotelRepository<UserTask>();
    public IRepository<UserNote> UserNotes => new UnsupportedHotelRepository<UserNote>();
    public IRepository<PricingType> PricingTypes => new UnsupportedHotelRepository<PricingType>();
    public IRepository<ProductPrice> ProductPrices => new UnsupportedHotelRepository<ProductPrice>();
    public IRepository<BusinessSettings> BusinessSettings => new UnsupportedHotelRepository<BusinessSettings>();

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
