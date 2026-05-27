using System.ComponentModel;
using System.Runtime.CompilerServices;
using DeployPilot.Shared;

namespace DeployPilot.Launcher;

public sealed class LauncherModuleCard : INotifyPropertyChanged
{
    private int _progress;
    private string _status = "";
    private string _actionLabel = "";

    public required Guid ModuleId { get; init; }

    public required string Name { get; init; }

    public required string ExecutableName { get; init; }

    public required string CurrentVersion { get; init; }

    public required string LatestVersion { get; init; }

    public required string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public required string Changelog { get; init; }

    public required string ActionLabel
    {
        get => _actionLabel;
        set
        {
            _actionLabel = value;
            OnPropertyChanged();
        }
    }

    public required int Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged();
        }
    }

    public required bool HasUpdate { get; init; }

    public required ArtifactRecord? Artifact { get; init; }

    public string SpeedLabel { get; private set; } = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public static LauncherModuleCard From(ModuleDefinition module, string currentVersion, UpdateCheckResult update)
    {
        var latestVersion = update.LatestVersion?.Version ?? currentVersion;
        return new LauncherModuleCard
        {
            ModuleId = module.Id,
            Name = module.Name,
            ExecutableName = module.ExecutableName,
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            Status = update.HasUpdate ? "Update available" : "Everything is up to date",
            Changelog = update.LatestVersion?.Changelog ?? "No pending changes were found for this module.",
            ActionLabel = update.HasUpdate ? "Update" : "Open",
            Progress = update.HasUpdate ? 0 : 100,
            HasUpdate = update.HasUpdate,
            Artifact = update.Artifact
        };
    }

    public void UpdateDownloadProgress(long bytesReceived, long? totalBytes, double bytesPerSecond)
    {
        if (totalBytes is > 0)
        {
            Progress = Math.Clamp((int)Math.Round(bytesReceived * 100d / totalBytes.Value), 0, 100);
        }

        SpeedLabel = $"{FormatBytes(bytesReceived)} received · {FormatBytes((long)bytesPerSecond)}/s";
        OnPropertyChanged(nameof(SpeedLabel));
    }

    private static string FormatBytes(long value)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        var size = (double)value;
        var suffix = 0;
        while (size >= 1024 && suffix < suffixes.Length - 1)
        {
            size /= 1024;
            suffix++;
        }

        return $"{size:0.##} {suffixes[suffix]}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
