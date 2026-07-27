using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
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

        return Build(baseConnection, systemType);
    }

    public static string Build(IConfiguration configuration, ISystemProfileService systemProfile, INetworkConnectionService? networkConnectionService = null)
    {
        if (systemProfile.IsBranchClient)
        {
            if (networkConnectionService is null || !networkConnectionService.IsBranchConfigured)
                throw new InvalidOperationException("الحاسبة الفرعية تتطلب إعدادات الربط بالحاسبة الرئيسية.");

            return networkConnectionService.BuildConnectionString(systemProfile.ActiveSystem);
        }

        return Build(configuration, systemProfile.ActiveSystem);
    }

    public static string Build(string baseConnection, ApplicationSystemType systemType)
    {
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
