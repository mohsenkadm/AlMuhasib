using AlMuhasib.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace AlMuhasib.Infrastructure;

public static class SystemConnectionStrings
{
    public const string AccountingDatabase = "AlMuhasibDb";
    public const string CarContractsDatabase = "AlMuhasibCarContractsDb";
    public const string HotelsDatabase = "AlMuhasibHotelsDb";

    public static string Build(IConfiguration configuration, ApplicationSystemType systemType)
    {
        var baseConnection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var builder = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = systemType switch
            {
                ApplicationSystemType.CarContracts => CarContractsDatabase,
                ApplicationSystemType.HotelManagement => HotelsDatabase,
                _ => AccountingDatabase
            }
        };

        return builder.ConnectionString;
    }
}
