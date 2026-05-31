using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    public UserAppPreferences Current { get; private set; } = new();

    public UserPreferencesService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "user-preferences.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_path))
        {
            Current = new UserAppPreferences();
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            Current = JsonSerializer.Deserialize<UserAppPreferences>(json, JsonOptions) ?? new UserAppPreferences();
        }
        catch
        {
            Current = new UserAppPreferences();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(_path, json);
    }

    public void Update(Action<UserAppPreferences> apply)
    {
        apply(Current);
        Save();
    }
}
