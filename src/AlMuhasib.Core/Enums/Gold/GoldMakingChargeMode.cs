namespace AlMuhasib.Core.Enums.Gold;

/// <summary>
/// How making/labor charge is derived for a gold line.
/// Fixed = absolute amount; PerGram = rate × weight; PercentOfGold = rate% of gold value.
/// </summary>
public enum GoldMakingChargeMode
{
    Fixed = 0,
    PerGram = 1,
    PercentOfGold = 2
}
