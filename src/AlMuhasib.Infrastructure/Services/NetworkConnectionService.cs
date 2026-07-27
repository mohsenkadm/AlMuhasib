using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Security;
using Microsoft.Data.SqlClient;

namespace AlMuhasib.Infrastructure.Services;

public sealed class NetworkConnectionService : INetworkConnectionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private NetworkConnectionProfile? _current;

    public NetworkConnectionService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "network-connection.json");
        _current = LoadFromDisk();
    }

    public NetworkConnectionProfile? Current => _current;
    public bool IsBranchConfigured => _current?.IsConfigured == true;

    public string BuildConnectionString(ApplicationSystemType systemType)
    {
        if (_current is null || !_current.IsConfigured)
            throw new InvalidOperationException("إعدادات الربط بالحاسبة الرئيسية غير مكتملة.");

        var profileCopy = CloneProfile(_current);
        if (string.IsNullOrWhiteSpace(profileCopy.DatabaseName))
            profileCopy.DatabaseName = GetDatabaseName(systemType);

        return BuildConnectionString(profileCopy);
    }

    public string BuildConnectionString(NetworkConnectionProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.MainServerHost))
            throw new InvalidOperationException("عنوان الحاسبة الرئيسية مطلوب.");

        var password = DpapiSecretProtector.Unprotect(profile.SqlPasswordEncrypted);
        var dataSource = BuildDataSource(profile.MainServerHost, profile.SqlInstance, profile.SqlPort);

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = profile.DatabaseName,
            UserID = profile.SqlUsername,
            Password = password,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = Math.Clamp(profile.ConnectionTimeoutSeconds, 5, 120),
            Encrypt = SqlConnectionEncryptOption.Optional
        };

        return builder.ConnectionString;
    }

    public NetworkConnectionProfile CreateProfileForSystem(ApplicationSystemType systemType, string databaseName) =>
        new()
        {
            SystemType = systemType,
            DatabaseName = databaseName,
            SqlPort = 1433,
            ConnectionTimeoutSeconds = 15
        };

    public async Task<NetworkConnectionTestResult> TestConnectionAsync(
        NetworkConnectionProfile profile,
        string? plainPassword = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.MainServerHost))
            return NetworkConnectionTestResult.Fail("يرجى إدخال عنوان IP للحاسبة الرئيسية.");

        if (string.IsNullOrWhiteSpace(profile.SqlUsername))
            return NetworkConnectionTestResult.Fail("يرجى إدخال اسم مستخدم SQL.");

        var password = !string.IsNullOrWhiteSpace(plainPassword)
            ? plainPassword
            : DpapiSecretProtector.Unprotect(profile.SqlPasswordEncrypted);

        if (string.IsNullOrWhiteSpace(password))
            return NetworkConnectionTestResult.Fail("يرجى إدخال كلمة مرور SQL.");

        try
        {
            var sw = Stopwatch.StartNew();
            var builder = new SqlConnectionStringBuilder(BuildConnectionString(CloneProfile(profile)))
            {
                Password = password
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            sw.Stop();

            var version = connection.ServerVersion;
            var migrationCount = await GetMigrationCountAsync(connection, cancellationToken);

            return NetworkConnectionTestResult.Ok(
                "تم الاتصال بنجاح بالحاسبة الرئيسية.",
                (int)sw.ElapsedMilliseconds,
                version,
                migrationCount);
        }
        catch (SqlException ex)
        {
            return NetworkConnectionTestResult.Fail(TranslateSqlError(ex));
        }
        catch (Exception ex)
        {
            return NetworkConnectionTestResult.Fail($"فشل الاتصال: {ex.Message}");
        }
    }

    public Task<NetworkConnectionTestResult> TestCurrentConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_current is null || !_current.IsConfigured)
            return Task.FromResult(NetworkConnectionTestResult.Fail("لم يتم إعداد الربط بعد."));

        return TestConnectionAsync(_current, null, cancellationToken);
    }

    public void SaveBranchProfile(NetworkConnectionProfile profile)
    {
        _current = profile;
        SaveToDisk();
    }

    public void ClearBranchProfile()
    {
        _current = null;
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private static async Task<int> GetMigrationCountAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory'";
        await using var checkCmd = new SqlCommand(sql, connection);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0;
        if (!exists)
            return 0;

        await using var countCmd = new SqlCommand("SELECT COUNT(*) FROM [__EFMigrationsHistory]", connection);
        return Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
    }

    private static string BuildDataSource(string host, string? instance, int port)
    {
        if (!string.IsNullOrWhiteSpace(instance))
            return $"{host}\\{instance},{port}";

        return port == 1433 ? host : $"{host},{port}";
    }

    private static string GetDatabaseName(ApplicationSystemType systemType) => systemType switch
    {
        ApplicationSystemType.CarContracts => SystemConnectionStrings.CarContractsDatabase,
        ApplicationSystemType.HotelManagement => SystemConnectionStrings.HotelsDatabase,
        ApplicationSystemType.CarTrading => SystemConnectionStrings.CarTradingDatabase,
        _ => SystemConnectionStrings.AccountingDatabase
    };

    private static NetworkConnectionProfile CloneProfile(NetworkConnectionProfile source) => new()
    {
        MainServerHost = source.MainServerHost,
        SqlPort = source.SqlPort,
        SqlInstance = source.SqlInstance,
        DatabaseName = source.DatabaseName,
        SystemType = source.SystemType,
        SqlUsername = source.SqlUsername,
        SqlPasswordEncrypted = source.SqlPasswordEncrypted,
        PairingCode = source.PairingCode,
        UseDiscovery = source.UseDiscovery,
        LastSuccessfulConnection = source.LastSuccessfulConnection,
        ConnectionTimeoutSeconds = source.ConnectionTimeoutSeconds,
        ServerLabel = source.ServerLabel
    };

    private static string TranslateSqlError(SqlException ex) => ex.Number switch
    {
        -1 or 53 => "تعذر الوصول للحاسبة الرئيسية. تحقق من IP والشبكة وجدار الحماية.",
        18456 => "فشل تسجيل الدخول. تحقق من اسم المستخدم وكلمة المرور ورمز الربط.",
        4060 => "قاعدة البيانات غير موجودة على الحاسبة الرئيسية.",
        _ => $"خطأ SQL ({ex.Number}): {ex.Message}"
    };

    private NetworkConnectionProfile? LoadFromDisk()
    {
        if (!File.Exists(_path))
            return null;

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<NetworkConnectionProfile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void SaveToDisk()
    {
        if (_current is null)
            return;

        var json = JsonSerializer.Serialize(_current, JsonOptions);
        File.WriteAllText(_path, json);
    }
}
