using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

/// <summary>إعدادات محل الذهب (صف واحد — Id = 1).</summary>
public class GoldSettings : BaseEntity
{
    public const int SingletonId = 1;

    public decimal MithqalGrams { get; set; } = 5;
    public string ScaleComPort { get; set; } = string.Empty;
    public int ScaleBaudRate { get; set; } = 9600;
    public decimal ScaleStabilityThresholdGrams { get; set; } = 0.01m;
    public bool AllowManualWeightEdit { get; set; } = true;
    public decimal LowStockAlertGrams { get; set; } = 10;
    public int OverdueDaysThreshold { get; set; } = 30;
    public string EnabledKaratsCsv { get; set; } = "24,22,21,18";
    public GoldMakingChargeMode DefaultMakingChargeMode { get; set; } = GoldMakingChargeMode.Fixed;
    /// <summary>True after the first-run gold setup wizard completes.</summary>
    public bool IsConfigured { get; set; }
}
