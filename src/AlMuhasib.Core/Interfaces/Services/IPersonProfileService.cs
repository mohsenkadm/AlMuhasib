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
