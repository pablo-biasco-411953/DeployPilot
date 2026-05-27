using System.IO;
using System.Text.Json;

namespace DeployPilot.Launcher;

public sealed class LauncherSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5080";

    public string Language { get; set; } = "en-US";

    public Dictionary<Guid, string> InstalledVersions { get; set; } = [];

    public static string FilePath
    {
        get
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeployPilot");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "launcher-settings.json");
        }
    }

    public static LauncherSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new LauncherSettings();
        }

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<LauncherSettings>(json) ?? new LauncherSettings();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
