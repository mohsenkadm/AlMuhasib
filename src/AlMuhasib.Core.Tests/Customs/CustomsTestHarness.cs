using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using AlMuhasib.Infrastructure.Services.Customs;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AlMuhasib.Core.Tests.Customs;

internal sealed class CustomsFakeUser : ICurrentUserService
{
    private readonly HashSet<string> _view = new(StringComparer.Ordinal);
    private readonly HashSet<string> _add = new(StringComparer.Ordinal);
    private readonly HashSet<string> _edit = new(StringComparer.Ordinal);
    private readonly HashSet<string> _delete = new(StringComparer.Ordinal);
    private readonly HashSet<string> _print = new(StringComparer.Ordinal);
    private readonly HashSet<string> _export = new(StringComparer.Ordinal);

    public string Username { get; set; } = "tester";
    public int? UserId { get; set; } = 1;
    public UserRole Role { get; set; } = UserRole.Admin;
    public bool IsAdmin { get; set; } = true;

    public void GrantAllCustomsScreens()
    {
        foreach (var screen in AllCustomsScreens())
            Grant(screen, view: true, add: true, edit: true, delete: true, print: true, export: true);
    }

    public void Grant(
        string screen,
        bool view = false,
        bool add = false,
        bool edit = false,
        bool delete = false,
        bool print = false,
        bool export = false)
    {
        Set(_view, screen, view);
        Set(_add, screen, add);
        Set(_edit, screen, edit);
        Set(_delete, screen, delete);
        Set(_print, screen, print);
        Set(_export, screen, export);
    }

    public bool CanView(string screenName) => IsAdmin || _view.Contains(screenName);
    public bool CanAdd(string screenName) => IsAdmin || _add.Contains(screenName);
    public bool CanEdit(string screenName) => IsAdmin || _edit.Contains(screenName);
    public bool CanDelete(string screenName) => IsAdmin || _delete.Contains(screenName);
    public bool CanPrint(string screenName) => IsAdmin || _print.Contains(screenName);
    public bool CanExport(string screenName) => IsAdmin || _export.Contains(screenName);
    public bool CanEditPrice(string screenName) => IsAdmin || CanEdit(screenName);
    public bool IsViewOnly(string screenName) =>
        !IsAdmin && CanView(screenName) && !CanAdd(screenName) && !CanEdit(screenName) && !CanDelete(screenName);

    private static void Set(HashSet<string> set, string screen, bool enabled)
    {
        if (enabled) set.Add(screen);
        else set.Remove(screen);
    }

    private static IEnumerable<string> AllCustomsScreens() =>
    [
        CustomsScreenNames.Dashboard,
        CustomsScreenNames.Customers,
        CustomsScreenNames.Invoices,
        CustomsScreenNames.Receipts,
        CustomsScreenNames.Statement,
        CustomsScreenNames.Reports,
        CustomsScreenNames.ProfitReport,
        CustomsScreenNames.Expenses
    ];
}

internal sealed class CustomsDbFactory : IDbContextFactory<CustomsDbContext>
{
    private readonly DbContextOptions<CustomsDbContext> _options;
    private readonly ICurrentUserService _user;
    private readonly bool _sqliteRowVersion;

    public CustomsDbFactory(
        DbContextOptions<CustomsDbContext> options,
        ICurrentUserService user,
        bool sqliteRowVersion = false)
    {
        _options = options;
        _user = user;
        _sqliteRowVersion = sqliteRowVersion;
    }

    public CustomsDbContext CreateDbContext() =>
        _sqliteRowVersion
            ? new CustomsSqliteTestDbContext(_options, _user)
            : new CustomsDbContext(_options, _user);
}

/// <summary>
/// SQLite test context: IsRowVersion() is SQL Server-oriented; force client-supplied RowVersion bytes.
/// </summary>
internal sealed class CustomsSqliteTestDbContext : CustomsDbContext
{
    public CustomsSqliteTestDbContext(DbContextOptions<CustomsDbContext> options, ICurrentUserService? user)
        : base(options, user)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.RowVersion))
                .ValueGeneratedNever();
        }
    }
}

internal sealed class FakeExportService : IExportService
{
    public int PrintInvoiceCalls { get; private set; }
    public int ExportInvoicePdfCalls { get; private set; }
    public int ExportVoucherPdfCalls { get; private set; }
    public int PrintTableCalls { get; private set; }
    public int ExportStatementPdfCalls { get; private set; }
    public int ExportExcelCalls { get; private set; }
    public InvoicePrintModel? LastInvoice { get; private set; }
    public VoucherPrintModel? LastVoucher { get; private set; }

    public void PrintInvoice(InvoicePrintModel model)
    {
        PrintInvoiceCalls++;
        LastInvoice = model;
    }

    public string ExportInvoiceToPdf(InvoicePrintModel model)
    {
        ExportInvoicePdfCalls++;
        LastInvoice = model;
        return Path.Combine(Path.GetTempPath(), $"customs-invoice-{Guid.NewGuid():N}.pdf");
    }

    public string ExportVoucherToPdf(VoucherPrintModel model)
    {
        ExportVoucherPdfCalls++;
        LastVoucher = model;
        return Path.Combine(Path.GetTempPath(), $"customs-voucher-{Guid.NewGuid():N}.pdf");
    }

    public void PrintTable(string title, string[] columns, IList<object[]> rows, IList<string>? summaryLines = null) =>
        PrintTableCalls++;

    public int PrintStatementCalls { get; private set; }
    public StatementPrintModel? LastStatement { get; private set; }

    public string ExportStatementToPdf(StatementPrintModel model)
    {
        ExportStatementPdfCalls++;
        LastStatement = model;
        return Path.Combine(Path.GetTempPath(), $"customs-statement-{Guid.NewGuid():N}.pdf");
    }

    public void PrintStatement(StatementPrintModel model)
    {
        PrintStatementCalls++;
        LastStatement = model;
    }

    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1") => [];
    public Task ExportToExcelFileAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1") =>
        Task.CompletedTask;
    public void ExportToExcel(string filePath, string sheetName, string[] columns, IList<object[]> rows) =>
        ExportExcelCalls++;
    public void ExportToExcel(
        string filePath,
        IReadOnlyList<(string SheetName, string[] Columns, IList<object[]> Rows)> sheets) { }
    public string ExportInstallmentPaymentReceiptToPdf(InstallmentPaymentReceiptPrintModel model) =>
        throw new NotSupportedException();
    public string ExportInvestorTransactionToPdf(InvestorTransactionPrintModel model) =>
        throw new NotSupportedException();
    public void PrintThermalReceipt(InvoicePrintModel model) => throw new NotSupportedException();
    public void PrintThermalReceipt(InvoicePrintModel model, bool silent) => throw new NotSupportedException();
    public string ExportInstallmentContractToPdf(InvoicePrintModel model) => throw new NotSupportedException();
    public void PrintInstallmentSchedule(InvoicePrintModel model) => throw new NotSupportedException();
    public void PrintInstallmentPlanDetail(InstallmentPlanDetailPrintModel model) => throw new NotSupportedException();
    public void PrintInstallmentMultiPlanDetail(IReadOnlyList<InstallmentPlanDetailPrintModel> plans, string title) =>
        throw new NotSupportedException();
    public void PrintInstallmentPlansSummary(InstallmentPlansSummaryPrintModel model) =>
        throw new NotSupportedException();
}

internal sealed class CustomsTestHarness : IAsyncDisposable
{
    public CustomsFakeUser User { get; }
    public CustomsDbFactory Factory { get; }
    public CustomsPermissionGuard Permissions { get; }
    public CustomsCustomerService Customers { get; }
    public CustomsInvoiceService Invoices { get; }
    public CustomsReceiptService Receipts { get; }
    public CustomsAttachmentService Attachments { get; }
    public CustomsReportService Reports { get; }
    public CustomsDashboardService Dashboard { get; }
    public CustomsPrintService Print { get; }
    public FakeExportService Export { get; }

    private readonly SqliteConnection? _sqliteConnection;

    private CustomsTestHarness(CustomsFakeUser user, bool useSqliteTransactions)
    {
        User = user;
        DbContextOptions<CustomsDbContext> options;
        if (useSqliteTransactions)
        {
            // Shared in-memory SQLite supports real transactions (InMemory does not roll back).
            _sqliteConnection = new SqliteConnection($"Data Source=customs-tx-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
            _sqliteConnection.Open();
            options = new DbContextOptionsBuilder<CustomsDbContext>()
                .UseSqlite(_sqliteConnection)
                .Options;
            using var bootstrap = new CustomsSqliteTestDbContext(options, user);
            bootstrap.Database.EnsureCreated();
            // AuditLog.UserId FK is enforced on SQLite (InMemory is lax) — seed the current test user.
            if (user.UserId is int uid && !bootstrap.Users.Any(u => u.Id == uid))
            {
                bootstrap.Users.Add(new User
                {
                    Id = uid,
                    Username = user.Username,
                    PasswordHash = "test",
                    FullName = user.Username,
                    Role = user.Role,
                    IsActive = true,
                    RowVersion = [1]
                });
                bootstrap.SaveChanges();
            }
        }
        else
        {
            // Receipt services use real transactions; InMemory ignores them — suppress the warning as error.
            options = new DbContextOptionsBuilder<CustomsDbContext>()
                .UseInMemoryDatabase($"customs-tests-{Guid.NewGuid():N}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        Factory = new CustomsDbFactory(options, user, sqliteRowVersion: useSqliteTransactions);
        Permissions = new CustomsPermissionGuard(user);
        Export = new FakeExportService();
        Customers = new CustomsCustomerService(Factory, Permissions);
        Invoices = new CustomsInvoiceService(Factory, Permissions);
        Receipts = new CustomsReceiptService(Factory, Permissions);
        Attachments = new CustomsAttachmentService(Factory, Permissions);
        Reports = new CustomsReportService(Factory, Permissions);
        Dashboard = new CustomsDashboardService(Factory, Permissions);
        Print = new CustomsPrintService(Export, Permissions);
    }

    public static CustomsTestHarness CreateAdmin()
    {
        var user = new CustomsFakeUser
        {
            IsAdmin = true,
            Role = UserRole.Admin,
            Username = "admin"
        };
        return new CustomsTestHarness(user, useSqliteTransactions: false);
    }

    /// <summary>SQLite shared-memory harness for transaction/rollback coverage (C-01).</summary>
    public static CustomsTestHarness CreateAdminTransactional()
    {
        var user = new CustomsFakeUser
        {
            IsAdmin = true,
            Role = UserRole.Admin,
            Username = "admin",
            UserId = 1
        };
        return new CustomsTestHarness(user, useSqliteTransactions: true);
    }

    public static CustomsTestHarness CreateUser(Action<CustomsFakeUser>? configure = null)
    {
        var user = new CustomsFakeUser
        {
            IsAdmin = false,
            Role = UserRole.User,
            Username = "clerk"
        };
        configure?.Invoke(user);
        return new CustomsTestHarness(user, useSqliteTransactions: false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqliteConnection is not null)
            await _sqliteConnection.DisposeAsync();
    }

    public async Task<CustomsCustomer> SeedCustomerAsync(string name = "عميل اختبار", string phone = "07700000000")
    {
        return await Customers.CreateAsync(new CustomsCustomer
        {
            Name = name,
            Phone = phone,
            CustomerType = CustomsCustomerType.Individual,
            IsActive = true,
            CreatedBy = User.Username,
            RowVersion = [1]
        });
    }

    public async Task<int> SeedExpenseTypeAsync(string name = "رسوم حدود")
    {
        await using var db = Factory.CreateDbContext();
        var type = new CustomsExpenseType
        {
            Name = name,
            IsActive = true,
            CreatedBy = User.Username,
            RowVersion = [1]
        };
        db.CustomsExpenseTypes.Add(type);
        await db.SaveChangesAsync();
        return type.Id;
    }

    public static CustomsInvoiceSaveRequest InvoiceRequest(
        int customerId,
        AccountingCurrency currency,
        decimal vehiclePrice,
        decimal discount = 0m,
        decimal? fxRate = null,
        int? expenseTypeId = null,
        decimal expenseAmount = 0m,
        AccountingCurrency? expenseCurrency = null,
        string? vehiclePlate = null)
    {
        var request = new CustomsInvoiceSaveRequest
        {
            CustomerId = customerId,
            InvoiceDate = DateTime.Today,
            ServiceType = CustomsServiceType.Clearance,
            Currency = currency,
            FxRate = currency == AccountingCurrency.USD ? fxRate ?? 1500m : 1m,
            DiscountAmount = discount,
            UserName = "tester",
            Vehicles =
            [
                new CustomsInvoiceVehicleDraft
                {
                    IraqiPlate = vehiclePlate ?? "بغداد 123",
                    CargoType = "بضائع عامة",
                    VehiclePrice = vehiclePrice,
                    Status = CustomsVehicleStatus.InProgress
                }
            ]
        };

        if (expenseTypeId is int typeId && expenseAmount > 0)
        {
            var expCurrency = expenseCurrency ?? currency;
            request.Expenses.Add(new CustomsInvoiceExpenseDraft
            {
                ExpenseTypeId = typeId,
                Description = "مصروف اختبار",
                Amount = expenseAmount,
                Currency = expCurrency,
                FxRate = expCurrency == AccountingCurrency.USD ? fxRate ?? 1500m : 1m,
                ExpenseDate = DateTime.Today
            });
        }

        return request;
    }

    public static CustomsReceiptSaveRequest ReceiptRequest(
        int customerId,
        int invoiceId,
        decimal amount,
        AccountingCurrency currency,
        decimal? fxRate = null) =>
        ReceiptRequest(
            customerId,
            amount,
            currency,
            [(invoiceId, amount)],
            fxRate);

    public static CustomsReceiptSaveRequest ReceiptRequest(
        int customerId,
        decimal amount,
        AccountingCurrency currency,
        IReadOnlyList<(int InvoiceId, decimal Amount)> allocations,
        decimal? fxRate = null) =>
        new()
        {
            CustomerId = customerId,
            ReceiptDate = DateTime.Today,
            Amount = amount,
            Currency = currency,
            FxRate = currency == AccountingCurrency.USD ? fxRate ?? 1500m : 1m,
            PaymentMethod = CustomsPaymentMethod.Cash,
            ReceiverName = "صندوق",
            UserName = "tester",
            Allocations = allocations
                .Select(a => new CustomsReceiptAllocationDraft
                {
                    InvoiceId = a.InvoiceId,
                    Amount = a.Amount
                })
                .ToList()
        };
}
