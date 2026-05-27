using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace DeployPilot.Shared;

public sealed record SemanticVersionValue(int Major, int Minor, int Patch, int Revision, string? Prerelease)
    : IComparable<SemanticVersionValue>
{
    public static SemanticVersionValue Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Version cannot be empty.", nameof(value));
        }

        var mainAndPre = value.Split('-', 2, StringSplitOptions.TrimEntries);
        var parts = mainAndPre[0].Split('.', StringSplitOptions.TrimEntries);
        if (parts.Length is < 2 or > 4)
        {
            throw new FormatException($"'{value}' is not a supported semantic version.");
        }

        var numbers = parts.Select(part =>
        {
            if (!int.TryParse(part, out var number) || number < 0)
            {
                throw new FormatException($"'{value}' contains an invalid numeric segment.");
            }

            return number;
        }).ToArray();

        return new SemanticVersionValue(
            numbers.ElementAtOrDefault(0),
            numbers.ElementAtOrDefault(1),
            numbers.ElementAtOrDefault(2),
            numbers.ElementAtOrDefault(3),
            mainAndPre.Length == 2 ? mainAndPre[1] : null);
    }

    public int CompareTo(SemanticVersionValue? other)
    {
        if (other is null)
        {
            return 1;
        }

        var numericComparison =
            Major.CompareTo(other.Major) is var major and not 0 ? major :
            Minor.CompareTo(other.Minor) is var minor and not 0 ? minor :
            Patch.CompareTo(other.Patch) is var patch and not 0 ? patch :
            Revision.CompareTo(other.Revision);

        if (numericComparison != 0)
        {
            return numericComparison;
        }

        if (Prerelease == other.Prerelease)
        {
            return 0;
        }

        if (Prerelease is null)
        {
            return 1;
        }

        if (other.Prerelease is null)
        {
            return -1;
        }

        return string.Compare(Prerelease, other.Prerelease, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        var version = Revision > 0 ? $"{Major}.{Minor}.{Patch}.{Revision}" : $"{Major}.{Minor}.{Patch}";
        return Prerelease is null ? version : $"{version}-{Prerelease}";
    }
}

public sealed class UpdateResolver
{
    public UpdateCheckResult Resolve(string currentVersion, IEnumerable<VersionRecord> versions, IEnumerable<ArtifactRecord> artifacts)
    {
        var current = SemanticVersionValue.Parse(currentVersion);
        var history = versions
            .OrderByDescending(version => SemanticVersionValue.Parse(version.Version))
            .ThenByDescending(version => version.ReleasedAt)
            .ToArray();

        var latest = history.FirstOrDefault(version => SemanticVersionValue.Parse(version.Version).CompareTo(current) > 0);
        var artifact = latest is null
            ? null
            : artifacts.FirstOrDefault(candidate => candidate.VersionId == latest.Id || candidate.Id == latest.ArtifactId);

        return new UpdateCheckResult(latest is not null, currentVersion, latest, artifact, history);
    }
}

public sealed class BuildQueue
{
    private readonly object _gate = new();
    private readonly Queue<Guid> _queuedIds = new();
    private readonly Dictionary<Guid, BuildJob> _jobs = new();
    private readonly HashSet<string> _activeLocks = new(StringComparer.OrdinalIgnoreCase);

    public BuildJob Enqueue(BuildJob job)
    {
        lock (_gate)
        {
            var queued = job with { Status = BuildJobStatus.Queued, Progress = 0 };
            _jobs[queued.Id] = queued;
            _queuedIds.Enqueue(queued.Id);
            return queued;
        }
    }

    public BuildJob? TryStartNext(DateTimeOffset now)
    {
        lock (_gate)
        {
            var deferred = new Queue<Guid>();
            BuildJob? selected = null;

            while (_queuedIds.Count > 0)
            {
                var jobId = _queuedIds.Dequeue();
                if (!_jobs.TryGetValue(jobId, out var job) || job.Status != BuildJobStatus.Queued)
                {
                    continue;
                }

                if (_activeLocks.Contains(job.LockKey))
                {
                    deferred.Enqueue(jobId);
                    continue;
                }

                selected = job.MarkRunning(now);
                _jobs[jobId] = selected;
                _activeLocks.Add(selected.LockKey);
                break;
            }

            while (deferred.Count > 0)
            {
                _queuedIds.Enqueue(deferred.Dequeue());
            }

            return selected;
        }
    }

    public BuildJob? Complete(Guid jobId, bool succeeded, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                return null;
            }

            var completed = job.MarkFinished(succeeded ? BuildJobStatus.Succeeded : BuildJobStatus.Failed, now);
            _jobs[jobId] = completed;
            _activeLocks.Remove(job.LockKey);
            return completed;
        }
    }

    public BuildJob? Cancel(Guid jobId, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_jobs.TryGetValue(jobId, out var job) || job.Status is BuildJobStatus.Succeeded or BuildJobStatus.Failed)
            {
                return null;
            }

            var canceled = job.MarkFinished(BuildJobStatus.Canceled, now);
            _jobs[jobId] = canceled;
            _activeLocks.Remove(job.LockKey);
            return canceled;
        }
    }

    public IReadOnlyList<BuildJob> Snapshot()
    {
        lock (_gate)
        {
            return _jobs.Values.OrderBy(job => job.RequestedAt).ToArray();
        }
    }
}

public sealed class RecipeSelector
{
    public BuildTemplate Select(RepositoryDefinition repository, IEnumerable<BuildTemplate> templates)
    {
        return templates.FirstOrDefault(template => template.IsEnabled && template.Technology == repository.Technology)
            ?? throw new InvalidOperationException($"No enabled build recipe was found for {repository.Technology}.");
    }
}

public sealed class SetupConfigurationValidator
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "en-US",
        "es",
        "es-AR"
    };

    public IReadOnlyList<string> Validate(SetupConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
        {
            errors.Add("A database connection string is required.");
        }

        if (string.IsNullOrWhiteSpace(configuration.ArtifactRoot))
        {
            errors.Add("An artifact root folder is required.");
        }

        if (!Uri.TryCreate(configuration.ArtifactBaseUrl, UriKind.Absolute, out _))
        {
            errors.Add("Artifact base URL must be absolute.");
        }

        if (string.IsNullOrWhiteSpace(configuration.AdminEmail) || !configuration.AdminEmail.Contains('@'))
        {
            errors.Add("A valid admin email is required.");
        }

        if (configuration.AdminPassword.Length < 8)
        {
            errors.Add("Admin password must contain at least 8 characters.");
        }

        if (!SupportedLanguages.Contains(configuration.DefaultLanguage))
        {
            errors.Add("Default language must be English or Spanish.");
        }

        return errors;
    }
}

public sealed class ArtifactManifestService
{
    public async Task<ArtifactManifest> CreateAsync(string filePath, string version, string gitSha, CancellationToken cancellationToken = default)
    {
        var file = new FileInfo(filePath);
        if (!file.Exists)
        {
            throw new FileNotFoundException("Artifact file was not found.", filePath);
        }

        await using var stream = file.OpenRead();
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return new ArtifactManifest(file.Name, file.Length, Convert.ToHexString(hash).ToLowerInvariant(), version, gitSha, DateTimeOffset.UtcNow);
    }
}

public sealed class IntegrityService
{
    public async Task<bool> MatchesSha256Async(string filePath, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        await using var stream = File.OpenRead(filePath);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class LocalizationCatalog
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _resources =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App.Title"] = "DeployPilot",
                ["Server.Header"] = "Deployment control center",
                ["Launcher.Header"] = "Applications ready to update",
                ["Update.Available"] = "Update available",
                ["Update.None"] = "Everything is up to date"
            },
            ["es"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App.Title"] = "DeployPilot",
                ["Server.Header"] = "Centro de control de despliegues",
                ["Launcher.Header"] = "Aplicaciones listas para actualizar",
                ["Update.Available"] = "Actualizacion disponible",
                ["Update.None"] = "Todo esta actualizado"
            }
        };

    public string GetString(string key, string language)
    {
        var normalized = language.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? "es" : "en";
        return _resources.TryGetValue(normalized, out var resource) && resource.TryGetValue(key, out var value)
            ? value
            : _resources["en"].GetValueOrDefault(key, key);
    }
}

public sealed class InMemoryDeployPilotStore : IDeployPilotStore
{
    private readonly object _jobGate = new();
    private readonly ConcurrentDictionary<Guid, Organization> _organizations = new();
    private readonly ConcurrentDictionary<Guid, RepositoryDefinition> _repositories = new();
    private readonly ConcurrentDictionary<Guid, ApplicationDefinition> _applications = new();
    private readonly ConcurrentDictionary<Guid, ModuleDefinition> _modules = new();
    private readonly ConcurrentDictionary<Guid, BuildJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, List<BuildEvent>> _events = new();
    private readonly ConcurrentDictionary<Guid, VersionRecord> _versions = new();
    private readonly ConcurrentDictionary<Guid, ArtifactRecord> _artifacts = new();

    public IReadOnlyList<BuildTemplate> BuildTemplates { get; } =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "MSBuild classic", BuildTechnology.MsBuildClassic, "recipes/msbuild-classic.ps1", "Builds legacy Visual Studio solutions.", true),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), ".NET SDK", BuildTechnology.DotNetSdk, "recipes/dotnet-sdk.ps1", "Builds SDK-style projects.", true),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "C# WinForms", BuildTechnology.CSharpWinForms, "recipes/csharp-winforms.ps1", "Builds C# Windows Forms applications.", true),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "VB.NET WinForms", BuildTechnology.VbNetWinForms, "recipes/vbnet-winforms.ps1", "Builds VB.NET Windows Forms applications.", true),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "FoxPro", BuildTechnology.FoxPro, "recipes/foxpro.ps1", "Runs a configurable FoxPro build command.", true),
        new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "Custom command", BuildTechnology.CustomCommand, "recipes/custom-command.ps1", "Runs a custom build command.", true)
    ];

    public Organization AddOrganization(string name, string slug)
    {
        var organization = new Organization(Guid.NewGuid(), name, slug, DateTimeOffset.UtcNow);
        _organizations[organization.Id] = organization;
        return organization;
    }

    public RepositoryDefinition AddRepository(Guid organizationId, string name, string remoteUrl, string branch, BuildTechnology technology, string projectPath, string? buildCommand)
    {
        var repository = new RepositoryDefinition(Guid.NewGuid(), organizationId, name, remoteUrl, branch, technology, projectPath, buildCommand, DateTimeOffset.UtcNow);
        _repositories[repository.Id] = repository;
        return repository;
    }

    public ApplicationDefinition AddApplication(Guid organizationId, string name, string slug)
    {
        var application = new ApplicationDefinition(Guid.NewGuid(), organizationId, name, slug, DateTimeOffset.UtcNow);
        _applications[application.Id] = application;
        return application;
    }

    public ModuleDefinition AddModule(Guid applicationId, string name, string executableName, string installPath)
    {
        var module = new ModuleDefinition(Guid.NewGuid(), applicationId, name, executableName, installPath, true, DateTimeOffset.UtcNow);
        _modules[module.Id] = module;
        return module;
    }

    public BuildJob RequestBuild(Guid organizationId, Guid repositoryId, Guid applicationId, Guid moduleId, string requestedBy, string? version, string? sha)
    {
        var job = new BuildJob(Guid.NewGuid(), organizationId, repositoryId, applicationId, moduleId, requestedBy, version, sha, BuildJobStatus.Queued, 0, DateTimeOffset.UtcNow, null, null);
        _jobs[job.Id] = job;
        return job;
    }

    public BuildEvent AddBuildEvent(Guid jobId, BuildEventLevel level, string message, int progress)
    {
        var buildEvent = new BuildEvent(Guid.NewGuid(), jobId, level, message, progress, DateTimeOffset.UtcNow);
        _events.AddOrUpdate(jobId, _ => [buildEvent], (_, existing) =>
        {
            lock (existing)
            {
                existing.Add(buildEvent);
                return existing;
            }
        });
        return buildEvent;
    }

    public VersionRecord AddVersion(Guid moduleId, string version, string gitSha, string changelog, Guid? artifactId = null)
    {
        var record = new VersionRecord(Guid.NewGuid(), moduleId, version, gitSha, changelog, DateTimeOffset.UtcNow, artifactId);
        _versions[record.Id] = record;
        return record;
    }

    public ArtifactRecord AddArtifact(Guid versionId, string fileName, string relativePath, long sizeBytes, string sha256)
    {
        var artifact = new ArtifactRecord(Guid.NewGuid(), versionId, fileName, relativePath, sizeBytes, sha256, DateTimeOffset.UtcNow);
        _artifacts[artifact.Id] = artifact;
        if (_versions.TryGetValue(versionId, out var version))
        {
            _versions[versionId] = version with { ArtifactId = artifact.Id };
        }

        return artifact;
    }

    public BuildJob? TryLeaseNextBuildJob(DateTimeOffset now)
    {
        lock (_jobGate)
        {
            var runningLocks = _jobs.Values
                .Where(job => job.Status == BuildJobStatus.Running)
                .Select(job => job.LockKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var next = _jobs.Values
                .Where(job => job.Status == BuildJobStatus.Queued)
                .OrderBy(job => job.RequestedAt)
                .FirstOrDefault(job => !runningLocks.Contains(job.LockKey));

            if (next is null)
            {
                return null;
            }

            var leased = next.MarkRunning(now);
            _jobs[leased.Id] = leased;
            return leased;
        }
    }

    public BuildJob? CompleteBuildJob(Guid jobId, bool succeeded, DateTimeOffset now)
    {
        lock (_jobGate)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                return null;
            }

            var completed = job.MarkFinished(succeeded ? BuildJobStatus.Succeeded : BuildJobStatus.Failed, now);
            _jobs[jobId] = completed;
            return completed;
        }
    }

    public BuildJob? CancelBuildJob(Guid jobId, DateTimeOffset now)
    {
        lock (_jobGate)
        {
            if (!_jobs.TryGetValue(jobId, out var job) || job.Status is BuildJobStatus.Succeeded or BuildJobStatus.Failed)
            {
                return null;
            }

            var canceled = job.MarkFinished(BuildJobStatus.Canceled, now);
            _jobs[jobId] = canceled;
            return canceled;
        }
    }

    public UpdateCheckResult CheckForUpdate(Guid moduleId, string currentVersion)
    {
        var versions = _versions.Values.Where(version => version.ModuleId == moduleId);
        return new UpdateResolver().Resolve(currentVersion, versions, _artifacts.Values);
    }

    public IReadOnlyList<Organization> GetOrganizations() => _organizations.Values.OrderBy(item => item.Name).ToArray();

    public IReadOnlyList<RepositoryDefinition> GetRepositories() => _repositories.Values.OrderBy(item => item.Name).ToArray();

    public IReadOnlyList<ApplicationDefinition> GetApplications() => _applications.Values.OrderBy(item => item.Name).ToArray();

    public IReadOnlyList<ModuleDefinition> GetModules() => _modules.Values.OrderBy(item => item.Name).ToArray();

    public IReadOnlyList<BuildJob> GetBuildJobs() => _jobs.Values.OrderByDescending(item => item.RequestedAt).ToArray();

    public IReadOnlyList<BuildEvent> GetBuildEvents(Guid jobId) => _events.TryGetValue(jobId, out var events)
        ? events.OrderBy(item => item.CreatedAt).ToArray()
        : [];

    public IReadOnlyList<VersionRecord> GetVersionHistory(Guid moduleId) => _versions.Values
        .Where(version => version.ModuleId == moduleId)
        .OrderByDescending(version => SemanticVersionValue.Parse(version.Version))
        .ToArray();
}
