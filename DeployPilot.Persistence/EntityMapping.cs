using DeployPilot.Persistence.Entities;
using DeployPilot.Shared;

namespace DeployPilot.Persistence;

internal static class EntityMapping
{
    public static Organization ToDomain(this OrganizationEntity entity) =>
        new(entity.Id, entity.Name, entity.Slug, entity.CreatedAt);

    public static RepositoryDefinition ToDomain(this RepositoryEntity entity) =>
        new(entity.Id, entity.OrganizationId, entity.Name, entity.RemoteUrl, entity.DefaultBranch, entity.Technology, entity.ProjectPath, entity.BuildCommand, entity.CreatedAt);

    public static ApplicationDefinition ToDomain(this ApplicationEntity entity) =>
        new(entity.Id, entity.OrganizationId, entity.Name, entity.Slug, entity.CreatedAt);

    public static ModuleDefinition ToDomain(this ModuleEntity entity) =>
        new(entity.Id, entity.ApplicationId, entity.Name, entity.ExecutableName, entity.InstallPath, entity.IsEnabled, entity.CreatedAt);

    public static BuildTemplate ToDomain(this BuildTemplateEntity entity) =>
        new(entity.Id, entity.Name, entity.Technology, entity.ScriptPath, entity.Description, entity.IsEnabled);

    public static BuildJob ToDomain(this BuildJobEntity entity) =>
        new(entity.Id, entity.OrganizationId, entity.RepositoryId, entity.ApplicationId, entity.ModuleId, entity.RequestedBy, entity.RequestedVersion, entity.RequestedSha, entity.Status, entity.Progress, entity.RequestedAt, entity.StartedAt, entity.FinishedAt);

    public static BuildEvent ToDomain(this BuildEventEntity entity) =>
        new(entity.Id, entity.BuildJobId, entity.Level, entity.Message, entity.Progress, entity.CreatedAt);

    public static VersionRecord ToDomain(this VersionEntity entity) =>
        new(entity.Id, entity.ModuleId, entity.Version, entity.GitSha, entity.Changelog, entity.ReleasedAt, entity.ArtifactId);

    public static ArtifactRecord ToDomain(this ArtifactEntity entity) =>
        new(entity.Id, entity.VersionId, entity.FileName, entity.RelativePath, entity.SizeBytes, entity.Sha256, entity.CreatedAt);
}
