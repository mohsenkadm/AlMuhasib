namespace AlMuhasib.Core.Models;

/// <summary>بادئة ملاحظات فواتير الرصيد الافتتاحي الآجل (عملاء/موردين).</summary>
public static class OpeningCreditBalanceMarkers
{
    public const string NotesPrefix = "رصيد افتتاحي — آجل";

    public static bool IsOpeningCreditBalance(string? notes)
        => !string.IsNullOrEmpty(notes)
           && notes.StartsWith(NotesPrefix, StringComparison.Ordinal);

    public static string BuildNotes(string? userNotes)
    {
        if (string.IsNullOrWhiteSpace(userNotes))
            return NotesPrefix;
        return $"{NotesPrefix} | {userNotes.Trim()}";
    }

    public static string ExtractUserNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes) || !IsOpeningCreditBalance(notes))
            return string.Empty;

        var separator = " | ";
        var index = notes.IndexOf(separator, StringComparison.Ordinal);
        if (index < 0)
            return string.Empty;

        return notes[(index + separator.Length)..].Trim();
    }
}

public class OpeningPartyBalanceRequest
{
    public int? PartyId { get; set; }
    public string? PartyName { get; set; }
    public string? Phone { get; set; }
    public string? FileNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class OpeningPartyBalanceUpdateRequest
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class OpeningPartyBalanceQuery
{
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool UnpaidOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class OpeningPartyBalanceListItem
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FileNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserNotes { get; set; } = string.Empty;
    public bool IsFullyPaid { get; set; }
    public bool CanModify => PaidAmount <= 0 && !IsFullyPaid;
}

public class OpeningPartyBalanceImportRow
{
    public int RowNumber { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FileNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public List<string> Errors { get; set; } = [];
    public bool IsValid => Errors.Count == 0;
    public string ErrorsText => Errors.Count == 0 ? "—" : string.Join(" | ", Errors);
}

public class OpeningPartyBalanceBatchResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class OpeningPartyBalancePagedResult
{
    public IReadOnlyList<OpeningPartyBalanceListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
