namespace AlMuhasib.Core.Entities;

/// <summary>سعر الصرف اليومي (دولار → دينار) للنظام المحاسبي.</summary>
public class ExchangeRate : BaseEntity
{
    public DateTime RateDate { get; set; } = DateTime.Today;

    /// <summary>كم دينار يساوي دولار واحد.</summary>
    public decimal UsdToIqd { get; set; }

    public string Notes { get; set; } = string.Empty;
}
