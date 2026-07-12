using System.Net;
using System.Net.Sockets;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Infrastructure.Network;

public sealed class BranchServerDiscoveryClient : IAsyncDisposable
{
    public async Task<IReadOnlyList<DiscoveredMainServer>> DiscoverAsync(
        ApplicationSystemType expectedSystemType,
        int port = QaydDiscoveryProtocol.DefaultPort,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        duration ??= TimeSpan.FromSeconds(8);
        var results = new Dictionary<string, DiscoveredMainServer>(StringComparer.OrdinalIgnoreCase);
        using var client = new UdpClient();
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        client.EnableBroadcast = true;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(duration.Value);

        var receiveTask = ReceiveLoopAsync(client, expectedSystemType, results, timeoutCts.Token);
        var broadcastTask = BroadcastLoopAsync(port, timeoutCts.Token);

        try
        {
            await Task.WhenAll(receiveTask, broadcastTask);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // expected timeout
        }

        return results.Values.OrderBy(s => s.ServerLabel).ToList();
    }

    private static async Task BroadcastLoopAsync(int port, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await QaydDiscoveryProtocol.SendDiscoverBroadcastAsync(port, cancellationToken);
            await Task.Delay(750, cancellationToken);
        }
    }

    private static async Task ReceiveLoopAsync(
        UdpClient client,
        ApplicationSystemType expectedSystemType,
        Dictionary<string, DiscoveredMainServer> results,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await client.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var server = QaydDiscoveryProtocol.TryParseResponse(result.Buffer, result.RemoteEndPoint.Address);
            if (server is null || server.SystemType != expectedSystemType)
                continue;

            var key = $"{server.Host}:{server.SqlPort}:{server.SqlInstance}:{server.DatabaseName}";
            results[key] = server;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
