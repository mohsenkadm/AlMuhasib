using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public sealed class SystemProfileService : ISystemProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private SystemProfile _current;

    public SystemProfileService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "system-profile.json");
        _current = LoadFromDisk();
    }

    public SystemProfile Current => _current;
    public bool IsFirstRun => !_current.IsConfigured;

    public ApplicationSystemType ActiveSystem =>
        _current.SelectedSystem ?? ApplicationSystemType.Accounting;

    public string ActiveDatabaseName => ActiveSystem switch
    {
        ApplicationSystemType.CarContracts => "AlMuhasibCarContractsDb",
        ApplicationSystemType.HotelManagement => "AlMuhasibHotelsDb",
        _ => "AlMuhasibDb"
    };

    public void SaveSelection(ApplicationSystemType system)
    {
        if (_current.IsConfigured)
            throw new InvalidOperationException("System type is already configured and cannot be changed.");

        _current = new SystemProfile
        {
            SelectedSystem = system,
            SelectedAt = DateTime.UtcNow
        };
        SaveToDisk();
    }

    public void ChangeSystem(ApplicationSystemType system)
    {
        _current = new SystemProfile
        {
            SelectedSystem = system,
            SelectedAt = DateTime.UtcNow
        };
        SaveToDisk();
    }

    private SystemProfile LoadFromDisk()
    {
        if (!File.Exists(_path))
            return new SystemProfile();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<SystemProfile>(json, JsonOptions) ?? new SystemProfile();
        }
        catch
        {
            return new SystemProfile();
        }
    }

    private void SaveToDisk()
    {
        var json = JsonSerializer.Serialize(_current, JsonOptions);
        File.WriteAllText(_path, json);
    }
}
