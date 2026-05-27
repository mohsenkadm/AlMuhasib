using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core;

/// <summary>نسبة الشركة — تُطبَّق فقط على أقساط نوع «منصة»</summary>
public static class CompanyFeeHelper
{
    public const decimal DefaultPercentage = 0.08m;

    public static bool AppliesTo(InstallmentType installmentType)
        => installmentType == InstallmentType.Platform;

    public static decimal CalculateAmount(decimal netAmount, decimal? percentage = null)
        => Math.Round(netAmount * (percentage ?? DefaultPercentage));

    public static (decimal Percentage, decimal Amount) ResolveForInstallment(
        decimal netAmount, InstallmentType installmentType)
    {
        if (!AppliesTo(installmentType))
            return (0, 0);

        var pct = DefaultPercentage;
        return (pct, CalculateAmount(netAmount, pct));
    }
}
