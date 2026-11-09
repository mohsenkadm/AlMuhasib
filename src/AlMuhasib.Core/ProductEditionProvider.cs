namespace AlMuhasib.Core;

/// <summary>
/// Runtime product edition (Qayd vs Duhok Express). Updated once after system profile load.
/// </summary>
public static class ProductEditionProvider
{
    public const string QaydThermalName = "نظام قيد المحاسبي";
    public const string ExpressThermalName = "اكسبريس";
    public const string QaydSupportPhone = "07505496065";
    public const string ExpressSupportPhone = "07504506695";

    public static bool IsDuhokBranch { get; private set; }

    public static void Update(bool isDuhokBranch) => IsDuhokBranch = isDuhokBranch;

    public static string SupportPhone =>
        IsDuhokBranch ? ExpressSupportPhone : QaydSupportPhone;

    public static string ThermalSystemName =>
        IsDuhokBranch ? ExpressThermalName : QaydThermalName;

    /// <summary>سطر تذييل حراري صغير: اسم النظام + رقم الدعم.</summary>
    public static string ThermalSupportLabel =>
        $"{ThermalSystemName}  {SupportPhone}";
}
