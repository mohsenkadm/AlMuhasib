using AlMuhasib.Core.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AlMuhasib.Infrastructure;

public static class SystemConnectionStrings
{
    public const string AccountingDatabase = "AlMuhasibDb";
    public const string CarContractsDatabase = "AlMuhasibCarContractsDb";
    public const string HotelsDatabase = "AlMuhasibHotelsDb";
    public const string CarTradingDatabase = "AlMuhasibCarTradingDb";

    public static string Build(IConfiguration configuration, ApplicationSystemType systemType)
    {
        var baseConnection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var databaseName = systemType switch
        {
            ApplicationSystemType.CarContracts => CarContractsDatabase,
            ApplicationSystemType.HotelManagement => HotelsDatabase,
            ApplicationSystemType.CarTrading => CarTradingDatabase,
            _ => AccountingDatabase
        };

        if (LocalDatabasePathResolver.HasAttachDbFilename(baseConnection))
        {
            var custom = new SqlConnectionStringBuilder(baseConnection)
            {
                InitialCatalog = databaseName
            };
            return custom.ConnectionString;
        }

        if (LocalDatabasePathResolver.UsesLocalDb(baseConnection))
            return LocalDatabasePathResolver.BuildLocalDbConnectionString(databaseName);

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }
}
