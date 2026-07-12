using System.Net;
using System.Net.Sockets;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Infrastructure.Network;

public sealed class MainServerDiscoveryResponder : IAsyncDisposable
{
    private UdpClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private Func<bool>? _shouldRespond;
    private Func<DiscoveredMainServer>? _buildServerInfo;

    public bool IsRunning => _listenTask is { IsCompleted: false };

    public Task StartAsync(
        int port,
        Func<DiscoveredMainServer> buildServerInfo,
        Func<bool>? shouldRespond = null,
        CancellationToken cancellationToken = default)
    {
        if (IsRunning)
            return Task.CompletedTask;

        _buildServerInfo = buildServerInfo;
        _shouldRespond = shouldRespond ?? (() => true);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _client = new UdpClient();
        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        _listenTask = ListenLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        if (_client is null || _buildServerInfo is null)
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await _client.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (!QaydDiscoveryProtocol.IsDiscoverRequest(result.Buffer))
                continue;

            if (_shouldRespond?.Invoke() != true)
                continue;

            var info = _buildServerInfo();
            info.Host = string.IsNullOrWhiteSpace(info.Host)
                ? GetLocalIPv4Address() ?? result.RemoteEndPoint.Address.ToString()
                : info.Host;

            var response = QaydDiscoveryProtocol.BuildResponseBytes(info);
            try
            {
                await _client.SendAsync(response, response.Length, result.RemoteEndPoint);
            }
            catch
            {
                // ignore send failures
            }
        }
    }

    public static string? GetLocalIPv4Address()
    {
        foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                continue;

            if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                continue;

            foreach (var uni in nic.GetIPProperties().UnicastAddresses)
            {
                if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                    return uni.Address.ToString();
            }
        }

        return null;
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        _client?.Dispose();
        _client = null;

        if (_listenTask is not null)
        {
            try { await _listenTask; } catch { /* ignore */ }
            _listenTask = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
