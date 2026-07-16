using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Infrastructure.Network;

public static class QaydDiscoveryProtocol
{
    public const int DefaultPort = 40777;
    public const string DiscoverRequest = "QAYD_DISCOVER_v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static byte[] BuildDiscoverRequestBytes() =>
        Encoding.UTF8.GetBytes(DiscoverRequest);

    public static bool IsDiscoverRequest(ReadOnlySpan<byte> data)
    {
        if (data.Length < DiscoverRequest.Length)
            return false;

        return Encoding.UTF8.GetString(data) == DiscoverRequest;
    }

    public static byte[] BuildResponseBytes(DiscoveredMainServer server)
    {
        var payload = new DiscoveryResponsePayload
        {
            App = "Qayd",
            Version = "1",
            Host = server.Host,
            SqlPort = server.SqlPort,
            Instance = server.SqlInstance,
            SystemType = (int)server.SystemType,
            DbName = server.DatabaseName,
            ServerLabel = server.ServerLabel,
            RequiresPairing = server.RequiresPairing
        };

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));
    }

    public static DiscoveredMainServer? TryParseResponse(ReadOnlySpan<byte> data, IPAddress sourceAddress)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            var payload = JsonSerializer.Deserialize<DiscoveryResponsePayload>(json, JsonOptions);
            if (payload is null || !string.Equals(payload.App, "Qayd", StringComparison.OrdinalIgnoreCase))
                return null;

            return new DiscoveredMainServer
            {
                Host = string.IsNullOrWhiteSpace(payload.Host) ? sourceAddress.ToString() : payload.Host,
                SqlPort = payload.SqlPort > 0 ? payload.SqlPort : 1433,
                SqlInstance = payload.Instance,
                SystemType = (ApplicationSystemType)payload.SystemType,
                DatabaseName = payload.DbName ?? string.Empty,
                ServerLabel = string.IsNullOrWhiteSpace(payload.ServerLabel) ? payload.Host ?? sourceAddress.ToString() : payload.ServerLabel,
                RequiresPairing = payload.RequiresPairing,
                DiscoveredAt = DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }

    public static async Task SendDiscoverBroadcastAsync(int port, CancellationToken cancellationToken)
    {
        using var client = new UdpClient { EnableBroadcast = true };
        var request = BuildDiscoverRequestBytes();

        foreach (var broadcastAddress in GetBroadcastAddresses())
        {
            try
            {
                await client.SendAsync(request, request.Length, new IPEndPoint(broadcastAddress, port));
            }
            catch
            {
                // ignore unreachable interfaces
            }
        }
    }

    public static IEnumerable<IPAddress> GetBroadcastAddresses()
    {
        yield return IPAddress.Broadcast;

        foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                continue;

            if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                continue;

            foreach (var uni in nic.GetIPProperties().UnicastAddresses)
            {
                if (uni.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                var mask = uni.IPv4Mask;
                if (mask is null)
                    continue;

                var ipBytes = uni.Address.GetAddressBytes();
                var maskBytes = mask.GetAddressBytes();
                var broadcastBytes = new byte[4];
                for (var i = 0; i < 4; i++)
                    broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

                yield return new IPAddress(broadcastBytes);
            }
        }
    }

    private sealed class DiscoveryResponsePayload
    {
        public string? App { get; set; }
        public string? Version { get; set; }
        public string? Host { get; set; }
        public int SqlPort { get; set; }
        public string? Instance { get; set; }
        public int SystemType { get; set; }
        public string? DbName { get; set; }
        public string? ServerLabel { get; set; }
        public bool RequiresPairing { get; set; }
    }
}
