namespace DeployPilot.Client;

public sealed record ApiHealth(
    string Service,
    string Status,
    DateTimeOffset Time);

public sealed record DemoSeedResult(
    Guid OrganizationId,
    Guid ApplicationId,
    IReadOnlyList<Guid> ModuleIds,
    string Message);

public sealed record DownloadProgress(
    long BytesReceived,
    long? TotalBytes,
    double BytesPerSecond);
