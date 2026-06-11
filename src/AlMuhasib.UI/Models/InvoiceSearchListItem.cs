namespace AlMuhasib.UI.Models;

public sealed class InvoiceSearchListItem
{
    public int Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string PartyName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public decimal NetAmount { get; init; }

    public string DateText => Date.ToString("yyyy/MM/dd");
    public string AmountText => $"{NetAmount:N0} د.ع";
    public string Subtitle => $"{PartyName} • {DateText}";
}
