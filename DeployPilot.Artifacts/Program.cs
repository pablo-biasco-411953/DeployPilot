using DeployPilot.Shared;

var builder = WebApplication.CreateBuilder(args);

var artifactRoot = builder.Configuration["ArtifactRoot"]
    ?? Environment.GetEnvironmentVariable("DEPLOYPILOT_ARTIFACT_ROOT")
    ?? Path.Combine(AppContext.BaseDirectory, "artifacts");

Directory.CreateDirectory(artifactRoot);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "DeployPilot.Artifacts",
    status = "healthy",
    root = artifactRoot,
    time = DateTimeOffset.UtcNow
}));

app.MapGet("/artifacts", () =>
{
    var files = Directory.EnumerateFiles(artifactRoot, "*", SearchOption.AllDirectories)
        .Select(path => new
        {
            fileName = Path.GetFileName(path),
            relativePath = Path.GetRelativePath(artifactRoot, path).Replace('\\', '/'),
            sizeBytes = new FileInfo(path).Length
        });

    return Results.Ok(files);
});

app.MapGet("/artifacts/{*relativePath}", (string relativePath) =>
{
    var fullPath = Path.GetFullPath(Path.Combine(artifactRoot, relativePath));
    var root = Path.GetFullPath(artifactRoot);

    if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
    {
        return Results.NotFound();
    }

    return Results.File(fullPath, "application/octet-stream", Path.GetFileName(fullPath));
});

app.MapGet("/artifact-manifests/{*relativePath}", async (string relativePath, string version, string gitSha, CancellationToken cancellationToken) =>
{
    var fullPath = Path.GetFullPath(Path.Combine(artifactRoot, relativePath));
    var root = Path.GetFullPath(artifactRoot);

    if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
    {
        return Results.NotFound();
    }

    var manifest = await new ArtifactManifestService().CreateAsync(fullPath, version, gitSha, cancellationToken);
    return Results.Ok(manifest);
});

app.Run();
