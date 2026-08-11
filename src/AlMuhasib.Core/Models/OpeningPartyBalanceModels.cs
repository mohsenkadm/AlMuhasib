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
