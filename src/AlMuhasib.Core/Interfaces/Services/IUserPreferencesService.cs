using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IUserPreferencesService
{
    UserAppPreferences Current { get; }
    void Load();
    void Save();
    void Update(Action<UserAppPreferences> apply);
}
