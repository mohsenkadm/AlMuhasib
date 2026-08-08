using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlMuhasib.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private AppDbContext? _activeContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private AppDbContext? GetActiveContext() => _activeContext;

    private IRepository<User>? _users;
    private IRepository<Permission>? _permissions;
    private IRepository<Category>? _categories;
    private IRepository<Product>? _products;
    private IRepository<Customer>? _customers;
    private IRepository<Driver>? _drivers;
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

    public IRepository<User> Users => _users ??= new Repository<User>(_contextFactory, GetActiveContext);
    public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_contextFactory, GetActiveContext);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_contextFactory, GetActiveContext);
    public IRepository<Product> Products => _products ??= new Repository<Product>(_contextFactory, GetActiveContext);
    public IRepository<Customer> Customers => _customers ??= new Repository<Customer>(_contextFactory, GetActiveContext);
    public IRepository<Driver> Drivers => _drivers ??= new Repository<Driver>(_contextFactory, GetActiveContext);
    public IRepository<SalesRepresentative> SalesRepresentatives =>
        _salesRepresentatives ??= new Repository<SalesRepresentative>(_contextFactory, GetActiveContext);
    public IRepository<SalesRepCommissionRule> SalesRepCommissionRules =>
        _salesRepCommissionRules ??= new Repository<SalesRepCommissionRule>(_contextFactory, GetActiveContext);
    public IRepository<SalesRepCommissionEntry> SalesRepCommissionEntries =>
        _salesRepCommissionEntries ??= new Repository<SalesRepCommissionEntry>(_contextFactory, GetActiveContext);
    public IRepository<SalesRepTarget> SalesRepTargets =>
        _salesRepTargets ??= new Repository<SalesRepTarget>(_contextFactory, GetActiveContext);
    public IRepository<SalesRepCollection> SalesRepCollections =>
        _salesRepCollections ??= new Repository<SalesRepCollection>(_contextFactory, GetActiveContext);
    public IRepository<Supplier> Suppliers => _suppliers ??= new Repository<Supplier>(_contextFactory, GetActiveContext);
    public IRepository<Warehouse> Warehouses => _warehouses ??= new Repository<Warehouse>(_contextFactory, GetActiveContext);
    public IRepository<WarehouseStock> WarehouseStocks => _warehouseStocks ??= new Repository<WarehouseStock>(_contextFactory, GetActiveContext);
    public IRepository<CashBox> CashBoxes => _cashBoxes ??= new Repository<CashBox>(_contextFactory, GetActiveContext);
    public IRepository<BankAccount> BankAccounts => _bankAccounts ??= new Repository<BankAccount>(_contextFactory, GetActiveContext);
    public IRepository<Investor> Investors => _investors ??= new Repository<Investor>(_contextFactory, GetActiveContext);
    public IRepository<ExpenseType> ExpenseTypes => _expenseTypes ??= new Repository<ExpenseType>(_contextFactory, GetActiveContext);
    public IRepository<Invoice> Invoices => _invoices ??= new Repository<Invoice>(_contextFactory, GetActiveContext);
    public IRepository<InvoiceItem> InvoiceItems => _invoiceItems ??= new Repository<InvoiceItem>(_contextFactory, GetActiveContext);
    public IRepository<InstallmentPlan> InstallmentPlans => _installmentPlans ??= new Repository<InstallmentPlan>(_contextFactory, GetActiveContext);
    public IRepository<Installment> Installments => _installments ??= new Repository<Installment>(_contextFactory, GetActiveContext);
    public IRepository<Voucher> Vouchers => _vouchers ??= new Repository<Voucher>(_contextFactory, GetActiveContext);
    public IRepository<Expense> Expenses => _expenses ??= new Repository<Expense>(_contextFactory, GetActiveContext);
    public IRepository<Transfer> Transfers => _transfers ??= new Repository<Transfer>(_contextFactory, GetActiveContext);
    public IRepository<InvestorTransaction> InvestorTransactions => _investorTransactions ??= new Repository<InvestorTransaction>(_contextFactory, GetActiveContext);
    public IRepository<ProfitDistribution> ProfitDistributions => _profitDistributions ??= new Repository<ProfitDistribution>(_contextFactory, GetActiveContext);
    public IRepository<ProfitDistributionDetail> ProfitDistributionDetails => _profitDistributionDetails ??= new Repository<ProfitDistributionDetail>(_contextFactory, GetActiveContext);
    public IRepository<CapitalEntry> CapitalEntries => _capitalEntries ??= new Repository<CapitalEntry>(_contextFactory, GetActiveContext);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_contextFactory, GetActiveContext);
    public IRepository<CustomerAttachment> CustomerAttachments => _customerAttachments ??= new Repository<CustomerAttachment>(_contextFactory, GetActiveContext);
    public IRepository<PrintBrandingSettings> PrintBrandingSettings => _printBrandingSettings ??= new Repository<PrintBrandingSettings>(_contextFactory, GetActiveContext);
    public IRepository<UserTask> UserTasks => _userTasks ??= new Repository<UserTask>(_contextFactory, GetActiveContext);
    public IRepository<UserNote> UserNotes => _userNotes ??= new Repository<UserNote>(_contextFactory, GetActiveContext);
    public IRepository<PricingType> PricingTypes => _pricingTypes ??= new Repository<PricingType>(_contextFactory, GetActiveContext);
    public IRepository<ProductPrice> ProductPrices => _productPrices ??= new Repository<ProductPrice>(_contextFactory, GetActiveContext);
    public IRepository<BusinessSettings> BusinessSettings => _businessSettings ??= new Repository<BusinessSettings>(_contextFactory, GetActiveContext);

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
