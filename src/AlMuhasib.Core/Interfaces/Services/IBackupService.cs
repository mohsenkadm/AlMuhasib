namespace AlMuhasib.Core.Interfaces.Services;

public interface IBackupService
{
    /// <summary>
    /// Creates a database backup at the specified file path.
    /// </summary>
    Task<string> BackupDatabaseAsync(string destinationPath);

    /// <summary>
    /// Restores the database from the specified backup file.
    /// </summary>
    Task RestoreDatabaseAsync(string backupFilePath);

    /// <summary>
    /// Gets the default backup directory path.
    /// </summary>
    string GetDefaultBackupDirectory();
}
