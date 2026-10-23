using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Customs;

public enum CustomsStatementTransactionType
{
    All = 0,
    Invoice = 1,
    Receipt = 2
}

public sealed class CustomsStatementInvoiceOption
{
    public int? Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class CustomsStatementResult
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public decimal OpeningIqd { get; set; }
    public decimal OpeningUsd { get; set; }
    public decimal TotalDebitIqd { get; set; }
    public decimal TotalCreditIqd { get; set; }
    public decimal TotalDebitUsd { get; set; }
    public decimal TotalCreditUsd { get; set; }
    public decimal BalanceIqd { get; set; }
    public decimal BalanceUsd { get; set; }
    public int TransactionCount { get; set; }
    public List<CustomsStatementRow> Rows { get; set; } = [];
}

public sealed class CustomsStatementRow
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string TransactionTypeLabel { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public int? RelatedInvoiceId { get; set; }
    public string? RelatedInvoiceNumber { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public string CurrencyLabel => Currency == AccountingCurrency.USD ? "$" : "د.ع";
}
