namespace AlMuhasib.Core.Models.Ux;

public class MainServerSettings
{
    public bool AllowBranchConnections { get; set; }
    public bool DiscoveryEnabled { get; set; } = true;
    public int DiscoveryPort { get; set; } = 40777;
    public int SqlPort { get; set; } = 1433;
    public string? SqlInstance { get; set; } = "SQLEXPRESS";
    public string PairingCode { get; set; } = string.Empty;
    public string ServerLabel { get; set; } = "قيد - الحاسبة الرئيسية";
    public string BranchSqlUsername { get; set; } = "QaydBranchUser";
    public string BranchSqlPasswordEncrypted { get; set; } = string.Empty;
    public bool SqlExpressConfigured { get; set; }
    public DateTime? ConfiguredAt { get; set; }
}
