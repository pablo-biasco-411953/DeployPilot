using DeployPilot.Shared;

namespace DeployPilot.Server;

public sealed class ServerBuildCard
{
    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required string Status { get; init; }

    public required int Progress { get; init; }

    public required string RequestedAt { get; init; }

    public static ServerBuildCard From(BuildJob job)
    {
        return new ServerBuildCard
        {
            Title = $"Build {job.Id.ToString("N")[..8]}",
            Subtitle = $"Module {job.ModuleId.ToString("N")[..8]}",
            Status = job.Status.ToString(),
            Progress = job.Progress,
            RequestedAt = job.RequestedAt.ToLocalTime().ToString("g")
        };
    }
}
