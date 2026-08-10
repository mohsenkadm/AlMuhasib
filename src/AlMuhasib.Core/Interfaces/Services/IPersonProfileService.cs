namespace AlMuhasib.Core.Interfaces.Services;

public enum PersonPartyType
{
    Customer,
    Supplier,
    Investor
}

public enum PersonTimelineCategory
{
    Invoice,
    Voucher,
    InstallmentPayment,
    OpeningBalance,
    Deposit,
    Withdrawal,
    ProfitDistribution,
    Other
}

public interface IPersonProfileService
{
    Task<List<PersonLookupItem>> SearchPeopleAsync(string? searchText = null, PersonPartyType? typeFilter = null);
    Task<PersonProfileResult?> GetProfileAsync(PersonPartyType type, int id, DateTime? from = null, DateTime? to = null);
}

public class PersonLookupItem
{
    public PersonPartyType PartyType { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
}

public class PersonProfileResult
{
    public PersonPartyType PartyType { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? FileNumber { get; set; }

    // Customer extras
    public decimal? MaxCreditLimit { get; set; }
    public decimal? MaxInstallmentDebt { get; set; }
    public int? ReliabilityScore { get; set; }
    public string? GuarantorName { get; set; }
    public string? GuarantorPhone { get; set; }

    // Investor extras
    public decimal? TotalDeposit { get; set; }
    public decimal? OpeningBalance { get; set; }
    public decimal? ProfitPercentage { get; set; }

    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }

    public List<PersonTimelineItem> Timeline { get; set; } = [];
    public List<PersonProfileSection> Sections { get; set; } = [];

    /// <summary>Filled only for customers — powers profile analytics tabs.</summary>
    public CustomerProfileInsights? CustomerInsights { get; set; }
}

public class CustomerProfileInsights
{
    public decimal SalesAmount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal NetProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public int InvoiceCount { get; set; }
    public decimal OutstandingBalance { get; set; }

    public List<CustomerProfitMonthPoint> ProfitByMonth { get; set; } = [];
    public List<CustomerProductPurchaseRow> Products { get; set; } = [];
    public List<CustomerAgingBucketRow> AgingBuckets { get; set; } = [];
    public List<CustomerAgingDetailRow> AgingDetails { get; set; } = [];
    public List<CustomerFinancialTxnRow> FinancialTransactions { get; set; } = [];
    public List<CustomerDueItemRow> DueItems { get; set; } = [];
}

public class CustomerProfitMonthPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}

public class CustomerProductPurchaseRow
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public int DealCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? LastDate { get; set; }
    public decimal LastUnitPrice { get; set; }
}

public class CustomerAgingBucketRow
{
    public string BucketName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class CustomerAgingDetailRow
{
    public string SourceType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal RemainingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public class CustomerFinancialTxnRow
{
    public DateTime Date { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string VoucherType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class CustomerDueItemRow
{
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? InvoiceId { get; set; }
}

public class PersonTimelineItem
{
    public DateTime Date { get; set; }
    public PersonTimelineCategory Category { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class PersonProfileSection
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsExpanded { get; set; } = true;
    public List<PersonProfileDetailRow> Rows { get; set; } = [];
}

public class PersonProfileDetailRow
{
    public DateTime? Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string AmountLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
