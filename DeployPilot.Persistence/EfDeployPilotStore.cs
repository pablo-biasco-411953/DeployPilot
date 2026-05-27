using DeployPilot.Persistence.Entities;
using DeployPilot.Shared;
using Microsoft.EntityFrameworkCore;

namespace DeployPilot.Persistence;

public sealed class EfDeployPilotStore(DeployPilotDbContext dbContext) : IDeployPilotStore
{
    public IReadOnlyList<BuildTemplate> BuildTemplates => dbContext.BuildTemplates
        .AsNoTracking()
        .OrderBy(template => template.Name)
        .Select(template => template.ToDomain())
        .ToArray();

    public Organization AddOrganization(string name, string slug)
    {
        var entity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Organizations.Add(entity);
        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public RepositoryDefinition AddRepository(Guid organizationId, string name, string remoteUrl, string branch, BuildTechnology technology, string projectPath, string? buildCommand)
    {
        var entity = new RepositoryEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            RemoteUrl = remoteUrl,
            DefaultBranch = branch,
            Technology = technology,
            ProjectPath = projectPath,
            BuildCommand = buildCommand,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Repositories.Add(entity);
        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public ApplicationDefinition AddApplication(Guid organizationId, string name, string slug)
    {
        var entity = new ApplicationEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Slug = slug,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Applications.Add(entity);
        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public ModuleDefinition AddModule(Guid applicationId, string name, string executableName, string installPath)
    {
        var entity = new ModuleEntity
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Name = name,
            ExecutableName = executableName,
            InstallPath = installPath,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Modules.Add(entity);
        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public BuildJob RequestBuild(Guid organizationId, Guid repositoryId, Guid applicationId, Guid moduleId, string requestedBy, string? version, string? sha)
    {
        var entity = new BuildJobEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RepositoryId = repositoryId,
            ApplicationId = applicationId,
            ModuleId = moduleId,
            RequestedBy = requestedBy,
            RequestedVersion = version,
            RequestedSha = sha,
            Status = BuildJobStatus.Queued,
            Progress = 0,
            RequestedAt = DateTimeOffset.UtcNow
        };

        dbContext.BuildJobs.Add(entity);
        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public BuildEvent AddBuildEvent(Guid jobId, BuildEventLevel level, string message, int progress)
    {
        var entity = new BuildEventEntity
        {
            Id = Guid.NewGuid(),
            BuildJobId = jobId,
            Level = level,
            Message = message,
            Progress = progress,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.BuildEvents.Add(entity);
        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public VersionRecord AddVersion(Guid moduleId, string version, string gitSha, string changelog, Guid? artifactId = null)
    {
        var entity = new VersionEntity
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            Version = version,
            GitSha = gitSha,
            Changelog = changelog,
            ArtifactId = artifactId,
            ReleasedAt = DateTimeOffset.UtcNow
        };

        dbContext.Versions.Add(entity);
        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public ArtifactRecord AddArtifact(Guid versionId, string fileName, string relativePath, long sizeBytes, string sha256)
    {
        var entity = new ArtifactEntity
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            FileName = fileName,
            RelativePath = relativePath,
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Artifacts.Add(entity);

        var version = dbContext.Versions.FirstOrDefault(item => item.Id == versionId);
        if (version is not null)
        {
            version.ArtifactId = entity.Id;
        }

        dbContext.SaveChanges();
        return entity.ToDomain();
    }

    public BuildJob? TryLeaseNextBuildJob(DateTimeOffset now)
    {
        using var transaction = dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true
            ? null
            : dbContext.Database.BeginTransaction();

        var runningLocks = dbContext.BuildJobs
            .Where(job => job.Status == BuildJobStatus.Running)
            .Select(job => new { job.OrganizationId, job.RepositoryId, job.ModuleId })
            .ToArray();

        var queuedJobs = dbContext.BuildJobs
            .Where(job => job.Status == BuildJobStatus.Queued)
            .OrderBy(job => job.RequestedAt)
            .ToArray();

        var next = queuedJobs.FirstOrDefault(job => !runningLocks.Any(active =>
            active.OrganizationId == job.OrganizationId &&
            active.RepositoryId == job.RepositoryId &&
            active.ModuleId == job.ModuleId));

        if (next is null)
        {
            return null;
        }

        next.Status = BuildJobStatus.Running;
        next.Progress = 5;
        next.StartedAt = now;
        dbContext.SaveChanges();
        transaction?.Commit();
        return next.ToDomain();
    }

    public BuildJob? CompleteBuildJob(Guid jobId, bool succeeded, DateTimeOffset now)
    {
        var job = dbContext.BuildJobs.FirstOrDefault(item => item.Id == jobId);
        if (job is null)
        {
            return null;
        }

        job.Status = succeeded ? BuildJobStatus.Succeeded : BuildJobStatus.Failed;
        job.Progress = succeeded ? 100 : job.Progress;
        job.FinishedAt = now;
        dbContext.SaveChanges();
        return job.ToDomain();
    }

    public BuildJob? CancelBuildJob(Guid jobId, DateTimeOffset now)
    {
        var job = dbContext.BuildJobs.FirstOrDefault(item => item.Id == jobId);
        if (job is null || job.Status is BuildJobStatus.Succeeded or BuildJobStatus.Failed)
        {
            return null;
        }

        job.Status = BuildJobStatus.Canceled;
        job.FinishedAt = now;
        dbContext.SaveChanges();
        return job.ToDomain();
    }

    public UpdateCheckResult CheckForUpdate(Guid moduleId, string currentVersion)
    {
        var versions = dbContext.Versions
            .AsNoTracking()
            .Where(version => version.ModuleId == moduleId)
            .Select(version => version.ToDomain())
            .ToArray();
        var artifacts = dbContext.Artifacts
            .AsNoTracking()
            .Select(artifact => artifact.ToDomain())
            .ToArray();

        return new UpdateResolver().Resolve(currentVersion, versions, artifacts);
    }

    public IReadOnlyList<Organization> GetOrganizations() => dbContext.Organizations
        .AsNoTracking()
        .OrderBy(item => item.Name)
        .Select(item => item.ToDomain())
        .ToArray();

    public IReadOnlyList<RepositoryDefinition> GetRepositories() => dbContext.Repositories
        .AsNoTracking()
        .OrderBy(item => item.Name)
        .Select(item => item.ToDomain())
        .ToArray();

    public IReadOnlyList<ApplicationDefinition> GetApplications() => dbContext.Applications
        .AsNoTracking()
        .OrderBy(item => item.Name)
        .Select(item => item.ToDomain())
        .ToArray();

    public IReadOnlyList<ModuleDefinition> GetModules() => dbContext.Modules
        .AsNoTracking()
        .OrderBy(item => item.Name)
        .Select(item => item.ToDomain())
        .ToArray();

    public IReadOnlyList<BuildJob> GetBuildJobs() => dbContext.BuildJobs
        .AsNoTracking()
        .OrderByDescending(item => item.RequestedAt)
        .Select(item => item.ToDomain())
        .ToArray();

    public IReadOnlyList<BuildEvent> GetBuildEvents(Guid jobId) => dbContext.BuildEvents
        .AsNoTracking()
        .Where(item => item.BuildJobId == jobId)
        .OrderBy(item => item.CreatedAt)
        .Select(item => item.ToDomain())
        .ToArray();

    public IReadOnlyList<VersionRecord> GetVersionHistory(Guid moduleId) => dbContext.Versions
        .AsNoTracking()
        .Where(version => version.ModuleId == moduleId)
        .Select(version => version.ToDomain())
        .ToArray()
        .OrderByDescending(version => SemanticVersionValue.Parse(version.Version))
        .ToArray();
}
