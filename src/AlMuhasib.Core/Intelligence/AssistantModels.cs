namespace AlMuhasib.Core.Intelligence;

/// <summary>نوايا محاسبية معتمدة. لا تُترجم إلى SQL حر.</summary>
public enum AccountingIntent
{
    Unknown = 0,
    Help,
    Unsafe,
    Unsupported,
    SalesSummary,
    SalesComparison,
    TopCustomers,
    CustomerDebt,
    CustomerPurchases,
    TopProducts,
    ExpensesSummary,
    ExpenseComparison,
    CashBalance,
    PurchasesSummary,
    ProfitSummary,
    ProfitComparison,
    FinancialSummary,
    LowStock,
    SupplierDebt,
    CustomerCollections,
    SupplierPayments
}

public enum AssistantDataDomain
{
    None = 0,
    Sales,
    Purchases,
    Expenses,
    Profit,
    CustomerBalances,
    Suppliers,
    Cash,
    Inventory,
    FinancialReports
}

public sealed class ConversationContext
{
    public AccountingIntent? LastIntent { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? PeriodLabel { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}

public sealed class Understanding
{
    public AccountingIntent Intent { get; init; }
    public string NormalizedText { get; init; } = string.Empty;
    public bool IsFollowUp { get; init; }
    public bool IsComparison { get; init; }
}

public sealed class QueryEntities
{
    public bool HasPeriod { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? PeriodLabel { get; set; }
    public bool HasComparePeriod { get; set; }
    public DateTime? CompareFrom { get; set; }
    public DateTime? CompareTo { get; set; }
    public string? CompareLabel { get; set; }
    public string? CustomerNameQuery { get; set; }
    public bool UseContextCustomer { get; set; }
    public int TopCount { get; set; } = 5;
}

/// <summary>خطة قراءة معتمدة. لا تحمل نص SQL.</summary>
public sealed class QueryPlan
{
    public AccountingIntent Intent { get; init; }
    public AssistantDataDomain Domain { get; init; }
    public string NormalizedText { get; init; } = string.Empty;
    public bool IsFollowUp { get; init; }
    public string ReportName { get; init; } = string.Empty;
    public string PeriodLabel { get; init; } = string.Empty;
    public string? CompareLabel { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public DateTime? CompareFrom { get; init; }
    public DateTime? CompareTo { get; init; }
    public DateTime? AsOf { get; init; }
    public int? CustomerId { get; init; }
    public string? CustomerNameQuery { get; init; }
    public string? ContextCustomerName { get; init; }
    public int TopCount { get; init; } = 5;
}

public sealed class AssistantMetricCard
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Hint { get; set; }
}

public sealed class AssistantChartPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class AssistantAnswer
{
    public string Text { get; set; } = string.Empty;
    public string PlainText { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public bool UpdatesContext { get; set; }
    public AccountingIntent Intent { get; set; }
    public string? SourceReport { get; set; }
    public string? PeriodLabel { get; set; }
    public string? BranchLabel { get; set; }
    public string? ConnectionLabel { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.Now;
    public List<AssistantMetricCard> Metrics { get; set; } = [];
    public List<string> TableHeaders { get; set; } = [];
    public List<List<string>> TableRows { get; set; } = [];
    public List<AssistantChartPoint> Chart { get; set; } = [];
    public int? FocusCustomerId { get; set; }
    public string? FocusCustomerName { get; set; }

    public void Stamp(string? branch, string? connection)
    {
        BranchLabel = branch;
        ConnectionLabel = connection;
        ExtractedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(branch))
            PlainText += "\nالفرع: " + branch;
        if (!string.IsNullOrWhiteSpace(connection))
            PlainText += "\nالاتصال: " + connection;
        PlainText += "\nوقت الاستخراج: " + ExtractedAt.ToString("yyyy/MM/dd HH:mm");
    }
}

public sealed class SalesSnapshot
{
    public decimal NetIqd { get; init; }
    public decimal GrossIqd { get; init; }
    public decimal ReturnsIqd { get; init; }
    public decimal NetUsd { get; init; }
    public decimal CashIqd { get; init; }
    public decimal CreditIqd { get; init; }
    public decimal InstallmentIqd { get; init; }
    public int InvoiceCount { get; init; }
    public bool ShowReturnSplit { get; init; }
    public IReadOnlyList<AssistantChartPoint> Daily { get; init; } = [];
}

public interface IAssistantAccessGuard
{
    bool CanOpen { get; }
    bool CanAccess(AssistantDataDomain domain);
}

public interface IQaidIntelligenceEngine
{
    Task<AssistantAnswer> AskAsync(string question, CancellationToken cancellationToken = default);
    void ResetSession();
}
