using DeployPilot.Shared;
using DeployPilot.Agent;
using DeployPilot.Client;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeployPilot.Tests;

public class CoreBehaviorTests
{
    [Fact]
    public void SemanticVersionComparisonHandlesFourSegments()
    {
        var older = SemanticVersionValue.Parse("2.0.1.4");
        var newer = SemanticVersionValue.Parse("2.0.1.5");

        Assert.True(newer.CompareTo(older) > 0);
    }

    [Fact]
    public void UpdateResolverReturnsLatestAvailableVersion()
    {
        var moduleId = Guid.NewGuid();
        var latestVersionId = Guid.NewGuid();
        var versions = new[]
        {
            new VersionRecord(Guid.NewGuid(), moduleId, "1.0.0", "aaa", "Initial release", DateTimeOffset.UtcNow.AddDays(-2), null),
            new VersionRecord(latestVersionId, moduleId, "1.1.0", "bbb", "Improved updater", DateTimeOffset.UtcNow.AddDays(-1), null)
        };
        var artifacts = new[]
        {
            new ArtifactRecord(Guid.NewGuid(), latestVersionId, "app.zip", "apps/app.zip", 128, "hash", DateTimeOffset.UtcNow)
        };

        var result = new UpdateResolver().Resolve("1.0.0", versions, artifacts);

        Assert.True(result.HasUpdate);
        Assert.Equal("1.1.0", result.LatestVersion?.Version);
        Assert.Equal("app.zip", result.Artifact?.FileName);
    }

    [Fact]
    public void BuildQueueDoesNotStartTwoJobsWithTheSameLock()
    {
        var queue = new BuildQueue();
        var organizationId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();

        queue.Enqueue(CreateJob(organizationId, repositoryId, applicationId, moduleId));
        queue.Enqueue(CreateJob(organizationId, repositoryId, applicationId, moduleId));

        var first = queue.TryStartNext(DateTimeOffset.UtcNow);
        var second = queue.TryStartNext(DateTimeOffset.UtcNow);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void BuildQueueAllowsNextJobAfterCompletion()
    {
        var queue = new BuildQueue();
        var organizationId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();

        queue.Enqueue(CreateJob(organizationId, repositoryId, applicationId, moduleId));
        queue.Enqueue(CreateJob(organizationId, repositoryId, applicationId, moduleId));

        var first = queue.TryStartNext(DateTimeOffset.UtcNow)!;
        queue.Complete(first.Id, succeeded: true, DateTimeOffset.UtcNow);

        var second = queue.TryStartNext(DateTimeOffset.UtcNow);

        Assert.NotNull(second);
        Assert.Equal(BuildJobStatus.Running, second.Status);
    }

    [Fact]
    public void CancelMarksRunningJobAsCanceled()
    {
        var queue = new BuildQueue();
        queue.Enqueue(CreateJob(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        var started = queue.TryStartNext(DateTimeOffset.UtcNow)!;

        var canceled = queue.Cancel(started.Id, DateTimeOffset.UtcNow);

        Assert.Equal(BuildJobStatus.Canceled, canceled?.Status);
    }

    [Fact]
    public async Task ManifestAndIntegrityValidationUseSha256()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        await File.WriteAllTextAsync(filePath, "deploypilot artifact");

        try
        {
            var manifest = await new ArtifactManifestService().CreateAsync(filePath, "1.0.0", "abc123");
            var isValid = await new IntegrityService().MatchesSha256Async(filePath, manifest.Sha256);

            Assert.Equal("1.0.0", manifest.Version);
            Assert.True(isValid);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void RecipeSelectorReturnsMatchingTemplate()
    {
        var store = new InMemoryDeployPilotStore();
        var repository = new RepositoryDefinition(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Demo",
            "https://example.com/demo.git",
            "main",
            BuildTechnology.FoxPro,
            "legacy/app.pjx",
            null,
            DateTimeOffset.UtcNow);

        var template = new RecipeSelector().Select(repository, store.BuildTemplates);

        Assert.Equal(BuildTechnology.FoxPro, template.Technology);
    }

    [Fact]
    public void LocalizationFallsBackToEnglish()
    {
        var catalog = new LocalizationCatalog();

        var value = catalog.GetString("Update.Available", "fr-FR");

        Assert.Equal("Update available", value);
    }

    [Fact]
    public void SetupValidatorRejectsInvalidConfiguration()
    {
        var configuration = new SetupConfiguration(
            DatabaseProvider.Postgres,
            "",
            "",
            "not-a-url",
            "admin",
            "short",
            "pt-BR");

        var errors = new SetupConfigurationValidator().Validate(configuration);

        Assert.Contains("A database connection string is required.", errors);
        Assert.Contains("Default language must be English or Spanish.", errors);
    }

    [Fact]
    public void RecipePlannerBuildsDeterministicPowerShellPlan()
    {
        var job = CreateJob(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var repository = new RepositoryDefinition(
            job.RepositoryId,
            job.OrganizationId,
            "Desktop Suite",
            "https://example.com/desktop-suite.git",
            "main",
            BuildTechnology.DotNetSdk,
            "src/DesktopSuite/DesktopSuite.csproj",
            null,
            DateTimeOffset.UtcNow);
        var template = new BuildTemplate(
            Guid.NewGuid(),
            ".NET SDK",
            BuildTechnology.DotNetSdk,
            "recipes/dotnet-sdk.ps1",
            "Builds SDK-style projects.",
            true);
        var options = new AgentOptions
        {
            WorkspaceRoot = "C:/deploypilot/workspace",
            OutputRoot = "C:/deploypilot/output"
        };

        var plan = new RecipeExecutionPlanner().CreatePlan(new AgentBuildLease(job, repository, template), options);

        Assert.Equal(job.Id, plan.BuildJobId);
        Assert.EndsWith("recipes/dotnet-sdk.ps1", plan.ScriptPath.Replace('\\', '/'));
        Assert.Contains("-ProjectPath", plan.Arguments);
        Assert.Contains("src/DesktopSuite/DesktopSuite.csproj", plan.Arguments);
        Assert.Contains(job.Id.ToString("N"), plan.OutputPath);
    }

    [Fact]
    public void GitSyncPlannerChecksOutRequestedShaWhenProvided()
    {
        var job = CreateJob(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()) with
        {
            RequestedSha = "abc123"
        };
        var repository = new RepositoryDefinition(
            job.RepositoryId,
            job.OrganizationId,
            "Desktop Suite",
            "https://example.com/desktop-suite.git",
            "main",
            BuildTechnology.DotNetSdk,
            "src/DesktopSuite/DesktopSuite.csproj",
            null,
            DateTimeOffset.UtcNow);
        var template = new BuildTemplate(Guid.NewGuid(), ".NET SDK", BuildTechnology.DotNetSdk, "recipes/dotnet-sdk.ps1", "", true);
        var options = new AgentOptions { WorkspaceRoot = "C:/deploypilot/workspace" };

        var plan = new GitSyncPlanner().CreatePlan(new AgentBuildLease(job, repository, template), options);

        Assert.Equal(repository.Id, plan.RepositoryId);
        Assert.Equal("abc123", plan.RequestedSha);
        Assert.Contains(plan.Commands, command => command.SequenceEqual(new[] { "-C", plan.RepositoryPath, "checkout", "abc123" }));
        Assert.Contains(repository.Id.ToString("N"), plan.RepositoryPath);
    }

    [Fact]
    public void GitSyncPlannerUsesDefaultBranchWhenShaIsMissing()
    {
        var job = CreateJob(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var repository = new RepositoryDefinition(
            job.RepositoryId,
            job.OrganizationId,
            "Desktop Suite",
            "https://example.com/desktop-suite.git",
            "release",
            BuildTechnology.DotNetSdk,
            "src/DesktopSuite/DesktopSuite.csproj",
            null,
            DateTimeOffset.UtcNow);
        var template = new BuildTemplate(Guid.NewGuid(), ".NET SDK", BuildTechnology.DotNetSdk, "recipes/dotnet-sdk.ps1", "", true);
        var options = new AgentOptions { WorkspaceRoot = "C:/deploypilot/workspace" };

        var plan = new GitSyncPlanner().CreatePlan(new AgentBuildLease(job, repository, template), options);

        Assert.Null(plan.RequestedSha);
        Assert.Contains(plan.Commands, command => command.SequenceEqual(new[] { "-C", plan.RepositoryPath, "checkout", "release" }));
    }

    [Fact]
    public async Task GitSyncRunnerDryRunDoesNotCreateWorkspace()
    {
        var repositoryPath = Path.Combine(Path.GetTempPath(), "DeployPilotTests", Guid.NewGuid().ToString("N"), "repo");
        var plan = new GitSyncPlan(
            Guid.NewGuid(),
            "https://example.com/desktop-suite.git",
            "main",
            null,
            repositoryPath,
            new[] { new[] { "clone", "--no-checkout", "https://example.com/desktop-suite.git", repositoryPath } });

        var result = await new GitSyncRunner(NullLogger<GitSyncRunner>.Instance).RunAsync(plan, executeGit: false, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(Directory.Exists(repositoryPath));
    }

    [Fact]
    public void ClientFactoryRejectsRelativeApiUrls()
    {
        var exception = Assert.Throws<ArgumentException>(() => DeployPilotClientFactory.Create("/api"));

        Assert.Equal("apiBaseUrl", exception.ParamName);
    }

    [Fact]
    public void RepositoryProbeDetectsSupportedProjectTypes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"deploypilot-probe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "src", "Desktop"));
        Directory.CreateDirectory(Path.Combine(root, "legacy"));
        File.WriteAllText(Path.Combine(root, "DeployPilotDemo.sln"), "");
        File.WriteAllText(Path.Combine(root, "src", "Desktop", "Desktop.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(root, "legacy", "LegacyBilling.pjx"), "");

        try
        {
            var result = new RepositoryProbeService().Probe(root);

            Assert.True(result.Exists);
            Assert.DoesNotContain(result.Warnings, warning => warning.Contains("No supported", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Projects, project => project.Technology == BuildTechnology.MsBuildClassic && project.RelativePath == "DeployPilotDemo.sln");
            Assert.Contains(result.Projects, project => project.Technology == BuildTechnology.CSharpWinForms && project.RelativePath == "src/Desktop/Desktop.csproj");
            Assert.Contains(result.Projects, project => project.Technology == BuildTechnology.FoxPro && project.SuggestedBuildCommand is not null);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RepositoryProbeIgnoresBuildOutputFolders()
    {
        var root = Path.Combine(Path.GetTempPath(), $"deploypilot-probe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "obj"));
        File.WriteAllText(Path.Combine(root, "src", "obj", "Ignored.csproj"), "<Project />");

        try
        {
            var result = new RepositoryProbeService().Probe(root);

            Assert.True(result.Exists);
            Assert.Empty(result.Projects);
            Assert.Contains("No supported project files were detected.", result.Warnings);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static BuildJob CreateJob(Guid organizationId, Guid repositoryId, Guid applicationId, Guid moduleId)
    {
        return new BuildJob(
            Guid.NewGuid(),
            organizationId,
            repositoryId,
            applicationId,
            moduleId,
            "test",
            null,
            null,
            BuildJobStatus.Queued,
            0,
            DateTimeOffset.UtcNow,
            null,
            null);
    }
}
