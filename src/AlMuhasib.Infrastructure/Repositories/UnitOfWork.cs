using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlMuhasib.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IBranchContext? _branchContext;
    private AppDbContext? _activeContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(IDbContextFactory<AppDbContext> contextFactory, IBranchContext? branchContext = null)
    {
        _contextFactory = contextFactory;
        _branchContext = branchContext;
    }

    private AppDbContext? GetActiveContext() => _activeContext;

    private IRepository<TEntity> CreateRepo<TEntity>() where TEntity : BaseEntity =>
        new Repository<TEntity>(_contextFactory, GetActiveContext, _branchContext);

    private IRepository<User>? _users;
    private IRepository<Permission>? _permissions;
    private IRepository<Category>? _categories;
    private IRepository<Product>? _products;
    private IRepository<Customer>? _customers;
    private IRepository<Driver>? _drivers;
    private IRepository<Employee>? _employees;
    private IRepository<SalesRepresentative>? _salesRepresentatives;
    private IRepository<SalesRepCommissionRule>? _salesRepCommissionRules;
    private IRepository<SalesRepCommissionEntry>? _salesRepCommissionEntries;
    private IRepository<SalesRepTarget>? _salesRepTargets;
    private IRepository<SalesRepCollection>? _salesRepCollections;
    private IRepository<Supplier>? _suppliers;
    private IRepository<Warehouse>? _warehouses;
    private IRepository<WarehouseStock>? _warehouseStocks;
    private IRepository<CashBox>? _cashBoxes;
    private IRepository<BankAccount>? _bankAccounts;
    private IRepository<Investor>? _investors;
    private IRepository<ExpenseType>? _expenseTypes;
    private IRepository<Invoice>? _invoices;
    private IRepository<InvoiceItem>? _invoiceItems;
    private IRepository<InstallmentPlan>? _installmentPlans;
    private IRepository<Installment>? _installments;
    private IRepository<Voucher>? _vouchers;
    private IRepository<Expense>? _expenses;
    private IRepository<Transfer>? _transfers;
    private IRepository<InvestorTransaction>? _investorTransactions;
    private IRepository<ProfitDistribution>? _profitDistributions;
    private IRepository<ProfitDistributionDetail>? _profitDistributionDetails;
    private IRepository<CapitalEntry>? _capitalEntries;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<CustomerAttachment>? _customerAttachments;
    private IRepository<PrintBrandingSettings>? _printBrandingSettings;
    private IRepository<UserTask>? _userTasks;
    private IRepository<UserNote>? _userNotes;
    private IRepository<PricingType>? _pricingTypes;
    private IRepository<ProductPrice>? _productPrices;
    private IRepository<BusinessSettings>? _businessSettings;

    public IRepository<User> Users => _users ??= CreateRepo<User>();
    public IRepository<Permission> Permissions => _permissions ??= CreateRepo<Permission>();
    public IRepository<Category> Categories => _categories ??= CreateRepo<Category>();
    public IRepository<Product> Products => _products ??= CreateRepo<Product>();
    public IRepository<Customer> Customers => _customers ??= CreateRepo<Customer>();
    public IRepository<Driver> Drivers => _drivers ??= CreateRepo<Driver>();
    public IRepository<Employee> Employees => _employees ??= CreateRepo<Employee>();
    public IRepository<SalesRepresentative> SalesRepresentatives =>
        _salesRepresentatives ??= CreateRepo<SalesRepresentative>();
    public IRepository<SalesRepCommissionRule> SalesRepCommissionRules =>
        _salesRepCommissionRules ??= CreateRepo<SalesRepCommissionRule>();
    public IRepository<SalesRepCommissionEntry> SalesRepCommissionEntries =>
        _salesRepCommissionEntries ??= CreateRepo<SalesRepCommissionEntry>();
    public IRepository<SalesRepTarget> SalesRepTargets =>
        _salesRepTargets ??= CreateRepo<SalesRepTarget>();
    public IRepository<SalesRepCollection> SalesRepCollections =>
        _salesRepCollections ??= CreateRepo<SalesRepCollection>();
    public IRepository<Supplier> Suppliers => _suppliers ??= CreateRepo<Supplier>();
    public IRepository<Warehouse> Warehouses => _warehouses ??= CreateRepo<Warehouse>();
    public IRepository<WarehouseStock> WarehouseStocks => _warehouseStocks ??= CreateRepo<WarehouseStock>();
    public IRepository<CashBox> CashBoxes => _cashBoxes ??= CreateRepo<CashBox>();
    public IRepository<BankAccount> BankAccounts => _bankAccounts ??= CreateRepo<BankAccount>();
    public IRepository<Investor> Investors => _investors ??= CreateRepo<Investor>();
    public IRepository<ExpenseType> ExpenseTypes => _expenseTypes ??= CreateRepo<ExpenseType>();
    public IRepository<Invoice> Invoices => _invoices ??= CreateRepo<Invoice>();
    public IRepository<InvoiceItem> InvoiceItems => _invoiceItems ??= CreateRepo<InvoiceItem>();
    public IRepository<InstallmentPlan> InstallmentPlans => _installmentPlans ??= CreateRepo<InstallmentPlan>();
    public IRepository<Installment> Installments => _installments ??= CreateRepo<Installment>();
    public IRepository<Voucher> Vouchers => _vouchers ??= CreateRepo<Voucher>();
    public IRepository<Expense> Expenses => _expenses ??= CreateRepo<Expense>();
    public IRepository<Transfer> Transfers => _transfers ??= CreateRepo<Transfer>();
    public IRepository<InvestorTransaction> InvestorTransactions => _investorTransactions ??= CreateRepo<InvestorTransaction>();
    public IRepository<ProfitDistribution> ProfitDistributions => _profitDistributions ??= CreateRepo<ProfitDistribution>();
    public IRepository<ProfitDistributionDetail> ProfitDistributionDetails => _profitDistributionDetails ??= CreateRepo<ProfitDistributionDetail>();
    public IRepository<CapitalEntry> CapitalEntries => _capitalEntries ??= CreateRepo<CapitalEntry>();
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= CreateRepo<AuditLog>();
    public IRepository<CustomerAttachment> CustomerAttachments => _customerAttachments ??= CreateRepo<CustomerAttachment>();
    public IRepository<PrintBrandingSettings> PrintBrandingSettings => _printBrandingSettings ??= CreateRepo<PrintBrandingSettings>();
    public IRepository<UserTask> UserTasks => _userTasks ??= CreateRepo<UserTask>();
    public IRepository<UserNote> UserNotes => _userNotes ??= CreateRepo<UserNote>();
    public IRepository<PricingType> PricingTypes => _pricingTypes ??= CreateRepo<PricingType>();
    public IRepository<ProductPrice> ProductPrices => _productPrices ??= CreateRepo<ProductPrice>();
    public IRepository<BusinessSettings> BusinessSettings => _businessSettings ??= CreateRepo<BusinessSettings>();

    public async Task<int> SaveChangesAsync()
    {
        if (_activeContext is not null)
            return await _activeContext.SaveChangesAsync();
        return 0;
    }

    public async Task BeginTransactionAsync()
    {
        _activeContext = await _contextFactory.CreateDbContextAsync();
        _transaction = await _activeContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        if (_activeContext is not null)
        {
            await _activeContext.DisposeAsync();
            _activeContext = null;
        }
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
        GC.SuppressFinalize(this);
    }
}
