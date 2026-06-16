using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Services;

public sealed class DeveloperAccessService : IDeveloperAccessService
{
    public const string DefaultPassword = "Muhasib@Dev";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private string _passwordHash;

    public DeveloperAccessService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "developer-access.json");
        _passwordHash = LoadOrCreateHash();
    }

    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, _passwordHash);
    }

    public void SetPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new InvalidOperationException("كلمة المرور يجب أن تكون 6 أحرف على الأقل");

        _passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        SaveToDisk();
    }

    private string LoadOrCreateHash()
    {
        if (File.Exists(_path))
        {
            try
            {
                var json = File.ReadAllText(_path);
                var data = JsonSerializer.Deserialize<DeveloperAccessFile>(json, JsonOptions);
                if (!string.IsNullOrWhiteSpace(data?.PasswordHash))
                    return data.PasswordHash;
            }
            catch
            {
                // fall through to default
            }
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword);
        _passwordHash = hash;
        SaveToDisk();
        return hash;
    }

    private void SaveToDisk()
    {
        var json = JsonSerializer.Serialize(new DeveloperAccessFile { PasswordHash = _passwordHash }, JsonOptions);
        File.WriteAllText(_path, json);
    }

    private sealed class DeveloperAccessFile
    {
        public string PasswordHash { get; set; } = string.Empty;
    }
}
