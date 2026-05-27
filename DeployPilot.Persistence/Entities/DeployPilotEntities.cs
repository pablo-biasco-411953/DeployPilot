using DeployPilot.Shared;

namespace DeployPilot.Persistence.Entities;

public sealed class OrganizationEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UserAccountEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ApplicationEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ModuleEntity
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = "";
    public string ExecutableName { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RepositoryEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string RemoteUrl { get; set; } = "";
    public string DefaultBranch { get; set; } = "";
    public BuildTechnology Technology { get; set; }
    public string ProjectPath { get; set; } = "";
    public string? BuildCommand { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class BuildTemplateEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public BuildTechnology Technology { get; set; }
    public string ScriptPath { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsEnabled { get; set; }
}

public sealed class BuildJobEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RepositoryId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid ModuleId { get; set; }
    public string RequestedBy { get; set; } = "";
    public string? RequestedVersion { get; set; }
    public string? RequestedSha { get; set; }
    public BuildJobStatus Status { get; set; }
    public int Progress { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}

public sealed class BuildEventEntity
{
    public Guid Id { get; set; }
    public Guid BuildJobId { get; set; }
    public BuildEventLevel Level { get; set; }
    public string Message { get; set; } = "";
    public int Progress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class VersionEntity
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string Version { get; set; } = "";
    public string GitSha { get; set; } = "";
    public string Changelog { get; set; } = "";
    public DateTimeOffset ReleasedAt { get; set; }
    public Guid? ArtifactId { get; set; }
}

public sealed class ArtifactEntity
{
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public string FileName { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class InstallationEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ModuleId { get; set; }
    public string MachineName { get; set; } = "";
    public string InstalledVersion { get; set; } = "";
    public string? CurrentSha { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}

public sealed class IntegrityCheckEntity
{
    public Guid Id { get; set; }
    public Guid InstallationId { get; set; }
    public Guid ArtifactId { get; set; }
    public bool IsValid { get; set; }
    public string Details { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AgentNodeEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsOnline { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}

public sealed class SettingEntity
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}
