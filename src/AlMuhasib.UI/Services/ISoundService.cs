namespace AlMuhasib.UI.Services;

public enum SoundEffect
{
    Success,
    Save,
    Delete,
    Error,
    Warning,
    Verify,
    Confirm,
    Cancel,
    Info,
    Click,
    Scan,
    Login,
    Notification
}

public interface ISoundService
{
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
    void Play(SoundEffect effect);
}
