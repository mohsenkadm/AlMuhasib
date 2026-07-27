using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

/// <summary>
/// Reads/writes the DefaultConnection (and related installation settings) in appsettings.json.
/// </summary>
public interface IAppSettingsConnectionStore
{
    string AppSettingsPath { get; }

    /// <summary>
    /// Builds a DefaultConnection string for the selected local SQL data source.
    /// </summary>
    string BuildDefaultConnectionString(SqlServerInstanceInfo instance, string databaseName = "AlMuhasibDb");

    /// <summary>
    /// Builds a DefaultConnection string from a raw data source value.
    /// </summary>
    string BuildDefaultConnectionString(string dataSource, bool isLocalDb, string databaseName = "AlMuhasibDb");

    /// <summary>
    /// Persists DefaultConnection into appsettings.json so migrations and runtime always use it.
    /// </summary>
    void SaveDefaultConnection(string connectionString);

    /// <summary>
    /// Saves the selected instance as DefaultConnection.
    /// </summary>
    void SaveSelectedInstance(SqlServerInstanceInfo instance, string databaseName = "AlMuhasibDb");
}
