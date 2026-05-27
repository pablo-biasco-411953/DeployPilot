using DeployPilot.Persistence;
using DeployPilot.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDeployPilotPersistence(builder.Configuration);

var app = builder.Build();

app.Services.EnsureDeployPilotDatabase();

app.MapGet("/health", () => Results.Ok(new
{
    service = "DeployPilot.Api",
    status = "healthy",
    time = DateTimeOffset.UtcNow
}));

app.MapGet("/api/build-templates", (IDeployPilotStore store) => Results.Ok(store.BuildTemplates));

app.MapGet("/api/organizations", (IDeployPilotStore store) => Results.Ok(store.GetOrganizations()));

app.MapPost("/api/organizations", (CreateOrganizationRequest request, IDeployPilotStore store) =>
{
    var organization = store.AddOrganization(request.Name, request.Slug);
    return Results.Created($"/api/organizations/{organization.Id}", organization);
});

app.MapGet("/api/repositories", (IDeployPilotStore store) => Results.Ok(store.GetRepositories()));

app.MapGet("/api/repositories/{repositoryId:guid}", (Guid repositoryId, IDeployPilotStore store) =>
{
    var repository = store.GetRepositories().FirstOrDefault(item => item.Id == repositoryId);
    return repository is null ? Results.NotFound() : Results.Ok(repository);
});

app.MapPost("/api/repositories", (CreateRepositoryRequest request, IDeployPilotStore store) =>
{
    var repository = store.AddRepository(
        request.OrganizationId,
        request.Name,
        request.RemoteUrl,
        request.DefaultBranch,
        request.Technology,
        request.ProjectPath,
        request.BuildCommand);

    return Results.Created($"/api/repositories/{repository.Id}", repository);
});

app.MapGet("/api/applications", (IDeployPilotStore store) => Results.Ok(store.GetApplications()));

app.MapPost("/api/applications", (CreateApplicationRequest request, IDeployPilotStore store) =>
{
    var application = store.AddApplication(request.OrganizationId, request.Name, request.Slug);
    return Results.Created($"/api/applications/{application.Id}", application);
});

app.MapGet("/api/modules", (IDeployPilotStore store) => Results.Ok(store.GetModules()));

app.MapPost("/api/modules", (CreateModuleRequest request, IDeployPilotStore store) =>
{
    var module = store.AddModule(request.ApplicationId, request.Name, request.ExecutableName, request.InstallPath);
    return Results.Created($"/api/modules/{module.Id}", module);
});

app.MapGet("/api/build-jobs", (IDeployPilotStore store) => Results.Ok(store.GetBuildJobs()));

app.MapPost("/api/build-jobs", (CreateBuildJobRequest request, IDeployPilotStore store) =>
{
    var job = store.RequestBuild(
        request.OrganizationId,
        request.RepositoryId,
        request.ApplicationId,
        request.ModuleId,
        request.RequestedBy,
        request.RequestedVersion,
        request.RequestedSha);

    store.AddBuildEvent(job.Id, BuildEventLevel.Info, "Build request queued.", 0);
    return Results.Created($"/api/build-jobs/{job.Id}", job);
});

app.MapPost("/api/build-jobs/{jobId:guid}/cancel", (Guid jobId, IDeployPilotStore store) =>
{
    var job = store.CancelBuildJob(jobId, DateTimeOffset.UtcNow);
    return job is null ? Results.NotFound() : Results.Ok(job);
});

app.MapGet("/api/build-jobs/{jobId:guid}/events", (Guid jobId, IDeployPilotStore store) =>
    Results.Ok(store.GetBuildEvents(jobId)));

app.MapPost("/api/build-jobs/{jobId:guid}/events", (Guid jobId, CreateBuildEventRequest request, IDeployPilotStore store) =>
{
    var buildEvent = store.AddBuildEvent(jobId, request.Level, request.Message, request.Progress);
    return Results.Created($"/api/build-jobs/{jobId}/events/{buildEvent.Id}", buildEvent);
});

app.MapPost("/api/modules/{moduleId:guid}/versions", (Guid moduleId, CreateVersionRequest request, IDeployPilotStore store) =>
{
    var version = store.AddVersion(moduleId, request.Version, request.GitSha, request.Changelog);
    return Results.Created($"/api/modules/{moduleId}/versions/{version.Id}", version);
});

app.MapPost("/api/versions/{versionId:guid}/artifacts", (Guid versionId, CreateArtifactRequest request, IDeployPilotStore store) =>
{
    var artifact = store.AddArtifact(versionId, request.FileName, request.RelativePath, request.SizeBytes, request.Sha256);
    return Results.Created($"/api/versions/{versionId}/artifacts/{artifact.Id}", artifact);
});

app.MapGet("/api/modules/{moduleId:guid}/versions", (Guid moduleId, IDeployPilotStore store) =>
    Results.Ok(store.GetVersionHistory(moduleId)));

app.MapGet("/api/launcher/modules/{moduleId:guid}/updates", (Guid moduleId, string currentVersion, IDeployPilotStore store) =>
    Results.Ok(store.CheckForUpdate(moduleId, currentVersion)));

app.MapPost("/api/agents/build-jobs/lease", (IDeployPilotStore store) =>
{
    var job = store.TryLeaseNextBuildJob(DateTimeOffset.UtcNow);
    if (job is null)
    {
        return Results.NoContent();
    }

    var repository = store.GetRepositories().FirstOrDefault(item => item.Id == job.RepositoryId);
    if (repository is null)
    {
        store.CompleteBuildJob(job.Id, succeeded: false, DateTimeOffset.UtcNow);
        store.AddBuildEvent(job.Id, BuildEventLevel.Error, "Repository was not found for leased build job.", 0);
        return Results.Problem("Repository was not found for leased build job.");
    }

    var template = store.BuildTemplates.FirstOrDefault(item => item.IsEnabled && item.Technology == repository.Technology);
    if (template is null)
    {
        store.CompleteBuildJob(job.Id, succeeded: false, DateTimeOffset.UtcNow);
        store.AddBuildEvent(job.Id, BuildEventLevel.Error, "Build template was not found for leased build job.", 0);
        return Results.Problem("Build template was not found for leased build job.");
    }

    return Results.Ok(new AgentBuildLease(job, repository, template));
});

app.MapPost("/api/agents/build-jobs/{jobId:guid}/complete", (Guid jobId, BuildJobCompletionRequest request, IDeployPilotStore store) =>
{
    var job = store.CompleteBuildJob(jobId, request.Succeeded, DateTimeOffset.UtcNow);
    if (job is null)
    {
        return Results.NotFound();
    }

    store.AddBuildEvent(
        jobId,
        request.Succeeded ? BuildEventLevel.Success : BuildEventLevel.Error,
        request.Message ?? (request.Succeeded ? "Build completed." : "Build failed."),
        job.Progress);

    return Results.Ok(job);
});

app.MapPost("/api/demo/seed", (IDeployPilotStore store) =>
{
    var existingOrganization = store.GetOrganizations().FirstOrDefault(item => item.Slug == "demo-health");
    if (existingOrganization is not null)
    {
        var existingApplication = store.GetApplications().First(item => item.OrganizationId == existingOrganization.Id);
        var existingModules = store.GetModules()
            .Where(item => item.ApplicationId == existingApplication.Id)
            .Select(item => item.Id)
            .ToArray();

        return Results.Ok(new DemoSeedResult(existingOrganization.Id, existingApplication.Id, existingModules, "Demo data already exists."));
    }

    var organization = store.AddOrganization("Demo Health", "demo-health");
    var repository = store.AddRepository(
        organization.Id,
        "Demo Desktop Suite",
        "https://github.com/example/demo-desktop-suite.git",
        "main",
        BuildTechnology.DotNetSdk,
        "src/DemoDesktopSuite/DemoDesktopSuite.csproj",
        null);
    var application = store.AddApplication(organization.Id, "Demo Desktop Suite", "demo-desktop-suite");
    var inventory = store.AddModule(application.Id, "Inventory Desktop", "InventoryDesktop.exe", "%LocalAppData%/DeployPilot/InventoryDesktop");
    var billing = store.AddModule(application.Id, "Billing Console", "BillingConsole.exe", "%LocalAppData%/DeployPilot/BillingConsole");
    var reports = store.AddModule(application.Id, "Lab Reports", "LabReports.exe", "%LocalAppData%/DeployPilot/LabReports");

    AddDemoVersion(store, inventory.Id, "1.3.0", "abc123inventory", "Improved build manifests, rollback and integrity validation.", "inventory-desktop-1.3.0.zip");
    AddDemoVersion(store, billing.Id, "2.0.4", "abc123billing", "Billing workflow stability update.", "billing-console-2.0.4.zip");
    AddDemoVersion(store, reports.Id, "0.9.8", "abc123reports", "First public beta with version picker support.", "lab-reports-0.9.8.zip");

    store.RequestBuild(organization.Id, repository.Id, application.Id, inventory.Id, "demo", "1.3.0", "abc123inventory");

    return Results.Created(
        "/api/demo/seed",
        new DemoSeedResult(organization.Id, application.Id, [inventory.Id, billing.Id, reports.Id], "Demo data created."));
});

app.Run();

static void AddDemoVersion(IDeployPilotStore store, Guid moduleId, string versionNumber, string gitSha, string changelog, string fileName)
{
    var version = store.AddVersion(moduleId, versionNumber, gitSha, changelog);
    store.AddArtifact(version.Id, fileName, $"demo/{fileName}", Random.Shared.Next(1_000_000, 8_000_000), Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant().PadRight(64, '0')[..64]);
}

public sealed record CreateOrganizationRequest(string Name, string Slug);

public sealed record CreateRepositoryRequest(
    Guid OrganizationId,
    string Name,
    string RemoteUrl,
    string DefaultBranch,
    BuildTechnology Technology,
    string ProjectPath,
    string? BuildCommand);

public sealed record CreateApplicationRequest(Guid OrganizationId, string Name, string Slug);

public sealed record CreateModuleRequest(Guid ApplicationId, string Name, string ExecutableName, string InstallPath);

public sealed record CreateBuildJobRequest(
    Guid OrganizationId,
    Guid RepositoryId,
    Guid ApplicationId,
    Guid ModuleId,
    string RequestedBy,
    string? RequestedVersion,
    string? RequestedSha);

public sealed record CreateBuildEventRequest(BuildEventLevel Level, string Message, int Progress);

public sealed record CreateVersionRequest(string Version, string GitSha, string Changelog);

public sealed record CreateArtifactRequest(string FileName, string RelativePath, long SizeBytes, string Sha256);

public sealed record DemoSeedResult(Guid OrganizationId, Guid ApplicationId, IReadOnlyList<Guid> ModuleIds, string Message);

public partial class Program;
