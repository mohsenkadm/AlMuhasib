using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<Category> Categories { get; }
    IRepository<Product> Products { get; }
    IRepository<Customer> Customers { get; }
    IRepository<Driver> Drivers { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<Warehouse> Warehouses { get; }
    IRepository<WarehouseStock> WarehouseStocks { get; }
    IRepository<CashBox> CashBoxes { get; }
    IRepository<BankAccount> BankAccounts { get; }
    IRepository<Investor> Investors { get; }
    IRepository<ExpenseType> ExpenseTypes { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<InvoiceItem> InvoiceItems { get; }
    IRepository<InstallmentPlan> InstallmentPlans { get; }
    IRepository<Installment> Installments { get; }
    IRepository<Voucher> Vouchers { get; }
    IRepository<Expense> Expenses { get; }
    IRepository<Transfer> Transfers { get; }
    IRepository<InvestorTransaction> InvestorTransactions { get; }
    IRepository<ProfitDistribution> ProfitDistributions { get; }
    IRepository<ProfitDistributionDetail> ProfitDistributionDetails { get; }
    IRepository<CapitalEntry> CapitalEntries { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<CustomerAttachment> CustomerAttachments { get; }
    IRepository<PrintBrandingSettings> PrintBrandingSettings { get; }
    IRepository<UserTask> UserTasks { get; }
    IRepository<UserNote> UserNotes { get; }
    IRepository<PricingType> PricingTypes { get; }
    IRepository<ProductPrice> ProductPrices { get; }
    IRepository<BusinessSettings> BusinessSettings { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
