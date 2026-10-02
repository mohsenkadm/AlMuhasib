using System.IO;
using System.Windows.Media;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Services;

public sealed class SoundService : ISoundService, IDisposable
{
    private static readonly IReadOnlyDictionary<SoundEffect, string> EffectFiles = new Dictionary<SoundEffect, string>
    {
        [SoundEffect.Success] = "success.wav",
        [SoundEffect.Save] = "save.wav",
        [SoundEffect.Delete] = "delete.wav",
        [SoundEffect.Error] = "error.wav",
        [SoundEffect.Warning] = "warning.wav",
        [SoundEffect.Verify] = "verify.wav",
        [SoundEffect.Confirm] = "confirm.wav",
        [SoundEffect.Cancel] = "cancel.wav",
        [SoundEffect.Info] = "info.wav",
        [SoundEffect.Click] = "click.wav",
        [SoundEffect.Scan] = "scan.wav",
        [SoundEffect.Login] = "login.wav",
        [SoundEffect.Notification] = "notification.wav",
    };

    private readonly IUserPreferencesService _preferences;
    private readonly string _soundsDirectory;
    private readonly Dictionary<SoundEffect, MediaPlayer> _players = [];
    private bool _disposed;

    public SoundService(IUserPreferencesService preferences)
    {
        _preferences = preferences;
        _soundsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
        PreloadPlayers();
    }

    public bool IsEnabled => _preferences.Current.SoundEnabled;

    public void SetEnabled(bool enabled) =>
        _preferences.Update(p => p.SoundEnabled = enabled);

    public void Play(SoundEffect effect)
    {
        if (_disposed || !IsEnabled)
            return;

        if (!_players.TryGetValue(effect, out var player))
            return;

        try
        {
            player.Stop();
            player.Position = TimeSpan.Zero;
            player.Play();
        }
        catch
        {
            // تجاهل أخطاء الصوت حتى لا تؤثر على سير العمل
        }
    }

    private void PreloadPlayers()
    {
        foreach (var (effect, fileName) in EffectFiles)
        {
            var path = Path.Combine(_soundsDirectory, fileName);
            if (!File.Exists(path))
                continue;

            var player = new MediaPlayer
            {
                Volume = 0.55
            };
            player.Open(new Uri(path, UriKind.Absolute));
            _players[effect] = player;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var player in _players.Values)
        {
            try
            {
                player.Stop();
                player.Close();
            }
            catch
            {
                // ignore during shutdown
            }
        }

        _players.Clear();
    }
}
