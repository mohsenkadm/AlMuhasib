using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Network;
using AlMuhasib.Infrastructure.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AlMuhasib.Infrastructure.Services;

public sealed class MainServerHostingService : IMainServerHostingService, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IConfiguration _configuration;
    private readonly MainServerDiscoveryResponder _discoveryResponder = new();
    private readonly string _settingsPath;
    private MainServerSettings _current;

    public MainServerHostingService(IConfiguration configuration)
    {
        _configuration = configuration;
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "main-server-settings.json");
        _current = LoadFromDisk() ?? new MainServerSettings();
        if (string.IsNullOrWhiteSpace(_current.PairingCode))
            _current.PairingCode = GeneratePairingCode();
    }

    public MainServerSettings Current => _current;
    public bool IsDiscoveryRunning => _discoveryResponder.IsRunning;

    public void SaveSettings(MainServerSettings settings)
    {
        _current = settings;
        SaveToDisk();
    }

    public string GeneratePairingCode() =>
        Random.Shared.Next(100000, 999999).ToString();

    public async Task<MainServerSetupResult> ConfigureSqlExpressAsync(
        ApplicationSystemType systemType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var databaseName = GetDatabaseName(systemType);
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "configure-sql-express.ps1");
            if (!File.Exists(scriptPath))
            {
                return await ConfigureSqlExpressInlineAsync(databaseName, cancellationToken);
            }

            var branchPassword = string.IsNullOrWhiteSpace(_current.BranchSqlPasswordEncrypted)
                ? GenerateSecurePassword()
                : DpapiSecretProtector.Unprotect(_current.BranchSqlPasswordEncrypted);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -DatabaseName \"{databaseName}\" -BranchUsername \"{_current.BranchSqlUsername}\" -BranchPassword \"{branchPassword}\" -SqlPort {_current.SqlPort}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
                return MainServerSetupResult.Fail("تعذر تشغيل سكربت تهيئة SQL Express.");

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
                return MainServerSetupResult.Fail(string.IsNullOrWhiteSpace(error) ? output : error);

            _current.BranchSqlPasswordEncrypted = DpapiSecretProtector.Protect(branchPassword);
            _current.SqlExpressConfigured = true;
            _current.ConfiguredAt = DateTime.UtcNow;
            SaveToDisk();

            var connectionString = BuildExpressConnectionString(databaseName);
            return MainServerSetupResult.Ok("تم تهيئة SQL Express لاستقبال الحاسبات الفرعية.", connectionString);
        }
        catch (Exception ex)
        {
            return MainServerSetupResult.Fail($"فشل تهيئة SQL Express: {ex.Message}");
        }
    }

    private async Task<MainServerSetupResult> ConfigureSqlExpressInlineAsync(string databaseName, CancellationToken cancellationToken)
    {
        var instance = _current.SqlInstance ?? "SQLEXPRESS";
        var dataSource = $"{Environment.MachineName}\\{instance}";
        var branchPassword = string.IsNullOrWhiteSpace(_current.BranchSqlPasswordEncrypted)
            ? GenerateSecurePassword()
            : DpapiSecretProtector.Unprotect(_current.BranchSqlPasswordEncrypted);

        try
        {
            var masterConnection = new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                ConnectTimeout = 30
            }.ConnectionString;

            await using (var connection = new SqlConnection(masterConnection))
            {
                await connection.OpenAsync(cancellationToken);

                var createLoginSql = $@"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{_current.BranchSqlUsername}')
    CREATE LOGIN [{_current.BranchSqlUsername}] WITH PASSWORD = N'{branchPassword.Replace("'", "''")}', CHECK_POLICY = OFF;
ELSE
    ALTER LOGIN [{_current.BranchSqlUsername}] WITH PASSWORD = N'{branchPassword.Replace("'", "''")}';";

                await using (var cmd = new SqlCommand(createLoginSql, connection))
                    await cmd.ExecuteNonQueryAsync(cancellationToken);

                var createUserSql = $@"
USE [{databaseName}];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{_current.BranchSqlUsername}')
BEGIN
    CREATE USER [{_current.BranchSqlUsername}] FOR LOGIN [{_current.BranchSqlUsername}];
    ALTER ROLE db_datareader ADD MEMBER [{_current.BranchSqlUsername}];
    ALTER ROLE db_datawriter ADD MEMBER [{_current.BranchSqlUsername}];
END";

                await using (var cmd = new SqlCommand(createUserSql, connection))
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            _current.BranchSqlPasswordEncrypted = DpapiSecretProtector.Protect(branchPassword);
            _current.SqlExpressConfigured = true;
            _current.ConfiguredAt = DateTime.UtcNow;
            SaveToDisk();

            return MainServerSetupResult.Ok(
                "تم إعداد مستخدم الفروع على SQL Server.",
                BuildExpressConnectionString(databaseName));
        }
        catch (Exception ex)
        {
            return MainServerSetupResult.Fail(
                $"تعذر الاتصال بـ SQL Express ({dataSource}). تأكد من تثبيت SQL Server Express وتفعيل TCP/IP.\n{ex.Message}");
        }
    }

    public async Task StartDiscoveryResponderAsync(
        ApplicationSystemType systemType,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        if (!_current.AllowBranchConnections || !_current.DiscoveryEnabled)
            return;

        var host = MainServerDiscoveryResponder.GetLocalIPv4Address() ?? "127.0.0.1";
        await _discoveryResponder.StartAsync(
            _current.DiscoveryPort,
            () => new DiscoveredMainServer
            {
                Host = host,
                SqlPort = _current.SqlPort,
                SqlInstance = _current.SqlInstance,
                SystemType = systemType,
                DatabaseName = databaseName,
                ServerLabel = _current.ServerLabel,
                RequiresPairing = true
            },
            () => _current.AllowBranchConnections,
            cancellationToken);
    }

    public Task StopDiscoveryResponderAsync() => _discoveryResponder.StopAsync();

    private string BuildExpressConnectionString(string databaseName)
    {
        var instance = _current.SqlInstance ?? "SQLEXPRESS";
        return new SqlConnectionStringBuilder
        {
            DataSource = $"{Environment.MachineName}\\{instance}",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        }.ConnectionString;
    }

    private static string GetDatabaseName(ApplicationSystemType systemType) => systemType switch
    {
        ApplicationSystemType.CarContracts => SystemConnectionStrings.CarContractsDatabase,
        ApplicationSystemType.HotelManagement => SystemConnectionStrings.HotelsDatabase,
        ApplicationSystemType.CarTrading => SystemConnectionStrings.CarTradingDatabase,
        ApplicationSystemType.RealEstateContracts => SystemConnectionStrings.RealEstateContractsDatabase,
        _ => SystemConnectionStrings.AccountingDatabase
    };

    private static string GenerateSecurePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$";
        return new string(Enumerable.Range(0, 16).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private MainServerSettings? LoadFromDisk()
    {
        if (!File.Exists(_settingsPath))
            return null;

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<MainServerSettings>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private void SaveToDisk()
    {
        var json = JsonSerializer.Serialize(_current, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    public async ValueTask DisposeAsync() => await _discoveryResponder.DisposeAsync();
}
