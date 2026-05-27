using DeployPilot.Shared;

namespace DeployPilot.Launcher;

public sealed class LauncherModuleCard
{
    public required Guid ModuleId { get; init; }

    public required string Name { get; init; }

    public required string ExecutableName { get; init; }

    public required string CurrentVersion { get; init; }

    public required string LatestVersion { get; init; }

    public required string Status { get; init; }

    public required string Changelog { get; init; }

    public required string ActionLabel { get; init; }

    public required int Progress { get; init; }

    public required bool HasUpdate { get; init; }

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
            HasUpdate = update.HasUpdate
        };
    }
}
