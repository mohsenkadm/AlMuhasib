using AlMuhasib.Core.Enums;

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

    /// <summary>عملة الرصيد الافتتاحي — افتراضي دينار للتوافق مع البيانات القديمة.</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    /// <summary>سعر الصرف (دولار→دينار) عند Currency=USD.</summary>
    public decimal FxRate { get; set; } = 1m;
}

public class OpeningPartyBalanceUpdateRequest
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Notes { get; set; }

    /// <summary>عملة الرصيد — افتراضي دينار للتوافق مع البيانات القديمة.</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    /// <summary>سعر الصرف (دولار→دينار) عند Currency=USD.</summary>
    public decimal FxRate { get; set; } = 1m;
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
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal FxRate { get; set; } = 1m;
    public bool CanModify => PaidAmount <= 0 && !IsFullyPaid;
}

public class OpeningPartyBalanceImportRow
{
    public int RowNumber { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? FileNumber { get; set; }
    public decimal Amount { get; set; }
    /// <summary>رصيد دولار مستقل عند القالب المزدوج — صفر يعني غير مستخدم.</summary>
    public decimal AmountUsd { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string? Notes { get; set; }

    /// <summary>عملة السطر — فارغ/غير معروف يُعامل كدينار (توافق القالب القديم).</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    /// <summary>سعر الصرف عند Currency=USD — مطلوب وصالح (&gt;0).</summary>
    public decimal FxRate { get; set; } = 1m;

    public List<string> Errors { get; set; } = [];
    public bool IsValid => Errors.Count == 0;
    public string ErrorsText => Errors.Count == 0 ? "—" : string.Join(" | ", Errors);
    public bool HasAnyBalance => Amount > 0 || AmountUsd > 0;
}

/// <summary>يبني طلبات إنشاء الرصيد الافتتاحي من صف الاستيراد (دينار و/أو دولار كآجل).</summary>
public static class OpeningPartyBalanceImportExpander
{
    public static IReadOnlyList<OpeningPartyBalanceRequest> Expand(
        OpeningPartyBalanceImportRow row,
        decimal fallbackUsdFxRate)
    {
        if (row is null) return Array.Empty<OpeningPartyBalanceRequest>();
        var name = row.PartyName?.Trim() ?? string.Empty;
        if (name.Length == 0) return Array.Empty<OpeningPartyBalanceRequest>();

        var list = new List<OpeningPartyBalanceRequest>(2);

        // قالب مزدوج: عمود دولار مستقل
        if (row.AmountUsd > 0)
        {
            if (row.Amount > 0)
            {
                list.Add(new OpeningPartyBalanceRequest
                {
                    PartyName = name,
                    Phone = row.Phone,
                    FileNumber = row.FileNumber,
                    Amount = row.Amount,
                    Date = row.Date,
                    Notes = row.Notes,
                    Currency = AccountingCurrency.IQD,
                    FxRate = 1m
                });
            }

            var fx = row.Currency == AccountingCurrency.USD && row.FxRate > 0
                ? row.FxRate
                : fallbackUsdFxRate;
            list.Add(new OpeningPartyBalanceRequest
            {
                PartyName = name,
                Phone = row.Phone,
                FileNumber = row.FileNumber,
                Amount = row.AmountUsd,
                Date = row.Date,
                Notes = row.Notes,
                Currency = AccountingCurrency.USD,
                FxRate = fx
            });
            return list;
        }

        // مبلغ واحد (دينار أو دولار حسب Currency)
        if (row.Amount > 0)
        {
            var currency = row.Currency;
            var fx = currency == AccountingCurrency.USD
                ? (row.FxRate > 0 ? row.FxRate : fallbackUsdFxRate)
                : 1m;
            list.Add(new OpeningPartyBalanceRequest
            {
                PartyName = name,
                Phone = row.Phone,
                FileNumber = row.FileNumber,
                Amount = row.Amount,
                Date = row.Date,
                Notes = row.Notes,
                Currency = currency,
                FxRate = fx
            });
        }

        return list;
    }
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
