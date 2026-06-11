using System.IO;
using System.Windows.Threading;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public sealed class BackupSchedulerService
{
    private readonly IBackupService _backupService;
    private readonly IUserPreferencesService _preferences;
    private readonly DispatcherTimer _timer;

    public BackupSchedulerService(IBackupService backupService, IUserPreferencesService preferences)
    {
        _backupService = backupService;
        _preferences = preferences;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _timer.Tick += async (_, _) => await TryAutoBackupAsync();
    }

    public void Start() => _timer.Start();

    private async Task TryAutoBackupAsync()
    {
        var backup = _preferences.Current.Backup;
        if (!backup.AutoBackupEnabled) return;

        var folder = backup.BackupFolderPath ?? _backupService.GetDefaultBackupDirectory();
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var now = DateTime.Now;
        if (backup.LastAutoBackupAt.HasValue)
        {
            var elapsed = now - backup.LastAutoBackupAt.Value;
            if (backup.Schedule == BackupSchedule.Daily && elapsed.TotalHours < 20)
                return;
            if (backup.Schedule == BackupSchedule.Weekly && elapsed.TotalDays < 6)
                return;
        }

        try
        {
            await _backupService.BackupDatabaseAsync(folder);
            _preferences.Update(p => p.Backup.LastAutoBackupAt = now);
            PruneOldBackups(folder, backup.RetainCount);
        }
        catch
        {
            // silent scheduled backup failure
        }
    }

    private static void PruneOldBackups(string folder, int retainCount)
    {
        var files = Directory.GetFiles(folder, "*.bak")
            .OrderByDescending(f => f)
            .Skip(Math.Max(1, retainCount))
            .ToList();
        foreach (var file in files)
        {
            try { File.Delete(file); } catch { /* ignore */ }
        }
    }
}
