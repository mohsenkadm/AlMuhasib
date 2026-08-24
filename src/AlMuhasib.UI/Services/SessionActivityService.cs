namespace AlMuhasib.UI.Services;

public sealed class SessionActivityService : ISessionActivityService
{
    public DateTime LastActivity { get; private set; } = DateTime.Now;

    public void RecordActivity() => LastActivity = DateTime.Now;
}
