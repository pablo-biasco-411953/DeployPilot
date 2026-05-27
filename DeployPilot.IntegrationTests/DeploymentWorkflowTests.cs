using DeployPilot.Shared;
using DeployPilot.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeployPilot.IntegrationTests;

public class DeploymentWorkflowTests
{
    [Fact]
    public void StoreSupportsOrganizationRepositoryModuleBuildVersionAndUpdateFlow()
    {
        var store = new InMemoryDeployPilotStore();
        var organization = store.AddOrganization("Northwind Labs", "northwind");
        var repository = store.AddRepository(
            organization.Id,
            "Desktop Suite",
            "https://example.com/northwind/desktop-suite.git",
            "main",
            BuildTechnology.DotNetSdk,
            "src/DesktopSuite/DesktopSuite.csproj",
            null);
        var application = store.AddApplication(organization.Id, "Desktop Suite", "desktop-suite");
        var module = store.AddModule(application.Id, "Inventory", "Inventory.exe", "%LocalAppData%/DeployPilot/Inventory");

        var job = store.RequestBuild(organization.Id, repository.Id, application.Id, module.Id, "admin", "1.1.0", "abc123");
        var queued = store.AddBuildEvent(job.Id, BuildEventLevel.Info, "Queued by API.", 0);
        var version = store.AddVersion(module.Id, "1.1.0", "abc123", "Added safe rollback support.");
        var artifact = store.AddArtifact(version.Id, "inventory-1.1.0.zip", "northwind/inventory-1.1.0.zip", 4096, "sha256");
        var update = store.CheckForUpdate(module.Id, "1.0.0");

        Assert.Equal("Northwind Labs", organization.Name);
        Assert.Equal(BuildTechnology.DotNetSdk, repository.Technology);
        Assert.Equal("Inventory", module.Name);
        Assert.Equal(BuildEventLevel.Info, queued.Level);
        Assert.True(update.HasUpdate);
        Assert.Equal(version.Id, update.LatestVersion?.Id);
        Assert.Equal(artifact.Id, update.Artifact?.Id);
    }

    [Fact]
    public void EfStorePersistsOrganizationRepositoryModuleBuildVersionAndUpdateFlow()
    {
        var options = new DbContextOptionsBuilder<DeployPilotDbContext>()
            .UseInMemoryDatabase($"deploypilot-{Guid.NewGuid():N}")
            .Options;

        using var dbContext = new DeployPilotDbContext(options);
        dbContext.Database.EnsureCreated();
        var store = new EfDeployPilotStore(dbContext);

        var organization = store.AddOrganization("Contoso Health", "contoso-health");
        var repository = store.AddRepository(
            organization.Id,
            "Clinical Desktop",
            "https://example.com/contoso/clinical-desktop.git",
            "main",
            BuildTechnology.MsBuildClassic,
            "ClinicalDesktop.sln",
            null);
        var application = store.AddApplication(organization.Id, "Clinical Desktop", "clinical-desktop");
        var module = store.AddModule(application.Id, "Reception", "Reception.exe", "%ProgramData%/DeployPilot/Reception");
        var job = store.RequestBuild(organization.Id, repository.Id, application.Id, module.Id, "admin", null, "def456");
        store.AddBuildEvent(job.Id, BuildEventLevel.Success, "Build completed.", 100);
        var version = store.AddVersion(module.Id, "2.4.0", "def456", "Improved setup diagnostics.");
        store.AddArtifact(version.Id, "reception-2.4.0.zip", "contoso/reception-2.4.0.zip", 8192, "hash");

        var update = store.CheckForUpdate(module.Id, "2.3.0");

        Assert.Single(store.GetOrganizations());
        Assert.Single(store.GetRepositories());
        Assert.Single(store.GetApplications());
        Assert.Single(store.GetModules());
        Assert.Single(store.GetBuildJobs());
        Assert.Single(store.GetBuildEvents(job.Id));
        Assert.True(update.HasUpdate);
        Assert.Equal("2.4.0", update.LatestVersion?.Version);
    }

    [Fact]
    public void EfStoreLeasesOneBuildPerOrganizationRepositoryModuleLock()
    {
        var options = new DbContextOptionsBuilder<DeployPilotDbContext>()
            .UseInMemoryDatabase($"deploypilot-{Guid.NewGuid():N}")
            .Options;

        using var dbContext = new DeployPilotDbContext(options);
        dbContext.Database.EnsureCreated();
        var store = new EfDeployPilotStore(dbContext);

        var organization = store.AddOrganization("Release Co", "release-co");
        var repository = store.AddRepository(organization.Id, "Suite", "https://example.com/suite.git", "main", BuildTechnology.DotNetSdk, "Suite.csproj", null);
        var application = store.AddApplication(organization.Id, "Suite", "suite");
        var module = store.AddModule(application.Id, "Portal", "Portal.exe", "%LocalAppData%/DeployPilot/Portal");

        store.RequestBuild(organization.Id, repository.Id, application.Id, module.Id, "admin", "1.0.1", null);
        store.RequestBuild(organization.Id, repository.Id, application.Id, module.Id, "admin", "1.0.2", null);

        var first = store.TryLeaseNextBuildJob(DateTimeOffset.UtcNow);
        var second = store.TryLeaseNextBuildJob(DateTimeOffset.UtcNow);

        Assert.NotNull(first);
        Assert.Null(second);

        store.CompleteBuildJob(first.Id, succeeded: true, DateTimeOffset.UtcNow);
        var third = store.TryLeaseNextBuildJob(DateTimeOffset.UtcNow);

        Assert.NotNull(third);
        Assert.Equal(BuildJobStatus.Running, third.Status);
    }
}
