namespace DeployPilot.Shared;

public enum DatabaseProvider
{
    MySql,
    Postgres
}

public enum BuildTechnology
{
    MsBuildClassic,
    DotNetSdk,
    CSharpWinForms,
    VbNetWinForms,
    FoxPro,
    CustomCommand
}

public enum BuildJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Canceled
}

public enum BuildEventLevel
{
    Info,
    Warning,
    Error,
    Success
}

public sealed record Organization(
    Guid Id,
    string Name,
    string Slug,
    DateTimeOffset CreatedAt);

public sealed record UserAccount(
    Guid Id,
    Guid OrganizationId,
    string DisplayName,
    string Email,
    string Role,
    DateTimeOffset CreatedAt);

public sealed record ApplicationDefinition(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Slug,
    DateTimeOffset CreatedAt);

public sealed record ModuleDefinition(
    Guid Id,
    Guid ApplicationId,
    string Name,
    string ExecutableName,
    string InstallPath,
    bool IsEnabled,
    DateTimeOffset CreatedAt);

public sealed record RepositoryDefinition(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string RemoteUrl,
    string DefaultBranch,
    BuildTechnology Technology,
    string ProjectPath,
    string? BuildCommand,
    DateTimeOffset CreatedAt);

public sealed record BuildTemplate(
    Guid Id,
    string Name,
    BuildTechnology Technology,
    string ScriptPath,
    string Description,
    bool IsEnabled);

public sealed record BuildJob(
    Guid Id,
    Guid OrganizationId,
    Guid RepositoryId,
    Guid ApplicationId,
    Guid ModuleId,
    string RequestedBy,
    string? RequestedVersion,
    string? RequestedSha,
    BuildJobStatus Status,
    int Progress,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt)
{
    public string LockKey => $"{OrganizationId:N}:{RepositoryId:N}:{ModuleId:N}";

    public BuildJob MarkRunning(DateTimeOffset now) => this with
    {
        Status = BuildJobStatus.Running,
        Progress = 5,
        StartedAt = now
    };

    public BuildJob MarkFinished(BuildJobStatus status, DateTimeOffset now) => this with
    {
        Status = status,
        Progress = status == BuildJobStatus.Succeeded ? 100 : Progress,
        FinishedAt = now
    };
}

public sealed record BuildEvent(
    Guid Id,
    Guid BuildJobId,
    BuildEventLevel Level,
    string Message,
    int Progress,
    DateTimeOffset CreatedAt);

public sealed record VersionRecord(
    Guid Id,
    Guid ModuleId,
    string Version,
    string GitSha,
    string Changelog,
    DateTimeOffset ReleasedAt,
    Guid? ArtifactId);

public sealed record ArtifactRecord(
    Guid Id,
    Guid VersionId,
    string FileName,
    string RelativePath,
    long SizeBytes,
    string Sha256,
    DateTimeOffset CreatedAt);

public sealed record Installation(
    Guid Id,
    Guid OrganizationId,
    Guid ModuleId,
    string MachineName,
    string InstalledVersion,
    string? CurrentSha,
    DateTimeOffset LastSeenAt);

public sealed record IntegrityCheck(
    Guid Id,
    Guid InstallationId,
    Guid ArtifactId,
    bool IsValid,
    string Details,
    DateTimeOffset CreatedAt);

public sealed record AgentNode(
    Guid Id,
    string Name,
    string MachineName,
    string Version,
    bool IsOnline,
    DateTimeOffset LastSeenAt);

public sealed record DeployPilotSetting(
    string Key,
    string Value,
    DateTimeOffset UpdatedAt);

public sealed record SetupConfiguration(
    DatabaseProvider DatabaseProvider,
    string ConnectionString,
    string ArtifactRoot,
    string ArtifactBaseUrl,
    string AdminEmail,
    string AdminPassword,
    string DefaultLanguage);

public sealed record ArtifactManifest(
    string FileName,
    long SizeBytes,
    string Sha256,
    string Version,
    string GitSha,
    DateTimeOffset CreatedAt);

public sealed record UpdateCheckResult(
    bool HasUpdate,
    string CurrentVersion,
    VersionRecord? LatestVersion,
    ArtifactRecord? Artifact,
    IReadOnlyList<VersionRecord> History);

public interface IDeployPilotStore
{
    IReadOnlyList<BuildTemplate> BuildTemplates { get; }

    Organization AddOrganization(string name, string slug);

    RepositoryDefinition AddRepository(Guid organizationId, string name, string remoteUrl, string branch, BuildTechnology technology, string projectPath, string? buildCommand);

    ApplicationDefinition AddApplication(Guid organizationId, string name, string slug);

    ModuleDefinition AddModule(Guid applicationId, string name, string executableName, string installPath);

    BuildJob RequestBuild(Guid organizationId, Guid repositoryId, Guid applicationId, Guid moduleId, string requestedBy, string? version, string? sha);

    BuildEvent AddBuildEvent(Guid jobId, BuildEventLevel level, string message, int progress);

    VersionRecord AddVersion(Guid moduleId, string version, string gitSha, string changelog, Guid? artifactId = null);

    ArtifactRecord AddArtifact(Guid versionId, string fileName, string relativePath, long sizeBytes, string sha256);

    BuildJob? TryLeaseNextBuildJob(DateTimeOffset now);

    BuildJob? CompleteBuildJob(Guid jobId, bool succeeded, DateTimeOffset now);

    BuildJob? CancelBuildJob(Guid jobId, DateTimeOffset now);

    UpdateCheckResult CheckForUpdate(Guid moduleId, string currentVersion);

    IReadOnlyList<Organization> GetOrganizations();

    IReadOnlyList<RepositoryDefinition> GetRepositories();

    IReadOnlyList<ApplicationDefinition> GetApplications();

    IReadOnlyList<ModuleDefinition> GetModules();

    IReadOnlyList<BuildJob> GetBuildJobs();

    IReadOnlyList<BuildEvent> GetBuildEvents(Guid jobId);

    IReadOnlyList<VersionRecord> GetVersionHistory(Guid moduleId);
}
