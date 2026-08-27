namespace AlMuhasib.Core.Enums.Gold;

public static class GoldItemStatusDisplay
{
    public static string ToArabic(GoldItemStatus status) => status switch
    {
        GoldItemStatus.InStock => "متوفر",
        GoldItemStatus.Sold => "مباع",
        GoldItemStatus.Reserved => "محجوز",
        _ => status.ToString()
    };
}
