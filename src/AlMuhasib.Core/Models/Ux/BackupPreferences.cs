namespace AlMuhasib.Core.Models.Ux;

public enum BackupSchedule
{
    Daily,
    Weekly
}

public class BackupPreferences
{
    public bool AutoBackupEnabled { get; set; }
    public BackupSchedule Schedule { get; set; } = BackupSchedule.Daily;
    public string? BackupFolderPath { get; set; }
    public int RetainCount { get; set; } = 7;
    public DateTime? LastAutoBackupAt { get; set; }
}
