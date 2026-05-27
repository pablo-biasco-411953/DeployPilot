using System.IO;
using System.Text.Json;

namespace DeployPilot.Server;

public sealed class ServerSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5080";

    public string Language { get; set; } = "en-US";

    public static string FilePath
    {
        get
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeployPilot");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "server-settings.json");
        }
    }

    public static ServerSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new ServerSettings();
        }

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<ServerSettings>(json) ?? new ServerSettings();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
