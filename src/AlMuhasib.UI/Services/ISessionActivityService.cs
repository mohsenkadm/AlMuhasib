namespace AlMuhasib.UI.Services;

/// <summary>
/// Tracks user input activity across all application windows (main + POS fullscreen).
/// Used by session idle-lock so activity in detached POS counts as active use.
/// </summary>
public interface ISessionActivityService
{
    DateTime LastActivity { get; }

    void RecordActivity();
}
