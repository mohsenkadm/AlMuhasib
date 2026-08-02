using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldScaleService : IGoldScaleService, IDisposable
{
    private static readonly Regex WeightRegex = new(
        @"[-+]?\d+(?:[.,]\d+)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IDbContextFactory<GoldDbContext> _contextFactory;
    private readonly object _sync = new();
    private SerialPort? _port;
    private string? _connectedPort;
    private int _baudRate = 9600;
    private decimal _stabilityThreshold = 0.01m;

    public GoldScaleService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public bool IsConnected
    {
        get
        {
            lock (_sync)
                return _port is { IsOpen: true };
        }
    }

    public string? ConnectedPort
    {
        get
        {
            lock (_sync)
                return _connectedPort;
        }
    }

    public Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            return Task.FromResult<IReadOnlyList<string>>(ports);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }

    public async Task ConnectAsync(string? comPort = null, int? baudRate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var settings = await GoldSettingsService.EnsureSettingsAsync(context, cancellationToken);

            var portName = string.IsNullOrWhiteSpace(comPort) ? settings.ScaleComPort : comPort;
            if (string.IsNullOrWhiteSpace(portName))
                throw new InvalidOperationException("لم يتم تحديد منفذ الميزان (COM). راجع إعدادات محل الذهب.");

            _baudRate = baudRate ?? (settings.ScaleBaudRate <= 0 ? 9600 : settings.ScaleBaudRate);
            _stabilityThreshold = settings.ScaleStabilityThresholdGrams <= 0
                ? 0.01m
                : settings.ScaleStabilityThresholdGrams;

            lock (_sync)
            {
                DisconnectInternal();

                _port = new SerialPort(portName, _baudRate)
                {
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 1500,
                    WriteTimeout = 1500,
                    Encoding = Encoding.ASCII,
                    NewLine = "\r\n"
                };
                _port.Open();
                _connectedPort = portName;
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            DisconnectInternal();
            throw new InvalidOperationException($"تعذر الاتصال بميزان الذهب: {ex.Message}", ex);
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        DisconnectInternal();
        return Task.CompletedTask;
    }

    public async Task<decimal> ReadWeightGramsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            try
            {
                await ConnectAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"الميزان غير متصل: {ex.Message}", ex);
            }
        }

        try
        {
            string raw;
            lock (_sync)
            {
                if (_port is null || !_port.IsOpen)
                    throw new InvalidOperationException("الميزان غير متصل");

                try
                {
                    _port.DiscardInBuffer();
                }
                catch
                {
                    // ignore
                }

                // Many scales continuously stream; try reading a line, then buffer.
                try
                {
                    raw = _port.ReadLine();
                }
                catch (TimeoutException)
                {
                    raw = _port.ReadExisting();
                }
            }

            if (!TryParseWeight(raw, out var weight))
                throw new InvalidOperationException("تعذر قراءة وزن صالح من الميزان");

            return weight;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"فشل قراءة الوزن من الميزان: {ex.Message}", ex);
        }
    }

    public async Task<bool> WaitForStableWeightAsync(
        decimal? thresholdGrams = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var threshold = thresholdGrams ?? _stabilityThreshold;
        if (threshold <= 0)
            threshold = 0.01m;

        var limit = timeout ?? TimeSpan.FromSeconds(8);
        var started = DateTime.UtcNow;
        decimal? previous = null;

        while (DateTime.UtcNow - started < limit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var current = await ReadWeightGramsAsync(cancellationToken);
                if (previous.HasValue && Math.Abs(current - previous.Value) <= threshold)
                    return true;
                previous = current;
            }
            catch
            {
                // keep trying until timeout
            }

            await Task.Delay(200, cancellationToken);
        }

        return false;
    }

    public void Dispose()
    {
        DisconnectInternal();
    }

    private void DisconnectInternal()
    {
        lock (_sync)
        {
            try
            {
                if (_port is { IsOpen: true })
                    _port.Close();
            }
            catch
            {
                // ignore
            }

            try
            {
                _port?.Dispose();
            }
            catch
            {
                // ignore
            }

            _port = null;
            _connectedPort = null;
        }
    }

    internal static bool TryParseWeight(string? raw, out decimal weight)
    {
        weight = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var matches = WeightRegex.Matches(raw);
        if (matches.Count == 0)
            return false;

        // Prefer the last numeric token (common for streaming scale protocols).
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var token = matches[i].Value.Replace(',', '.');
            if (decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out weight))
                return true;
        }

        return false;
    }
}
