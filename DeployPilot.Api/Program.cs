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
    return job is null ? Results.NoContent() : Results.Ok(job);
});

app.MapPost("/api/agents/build-jobs/{jobId:guid}/complete", (Guid jobId, CompleteBuildJobRequest request, IDeployPilotStore store) =>
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

app.Run();

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

public sealed record CompleteBuildJobRequest(bool Succeeded, string? Message);

public partial class Program;
