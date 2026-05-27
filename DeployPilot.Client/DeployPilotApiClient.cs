using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using DeployPilot.Shared;

namespace DeployPilot.Client;

public sealed class DeployPilotApiClient(HttpClient httpClient)
{
    public async Task<ApiHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ApiHealth>("/health", cancellationToken)
            ?? throw new InvalidOperationException("DeployPilot API returned an empty health response.");
    }

    public async Task<IReadOnlyList<ModuleDefinition>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<ModuleDefinition>>("/api/modules", cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<BuildTemplate>> GetBuildTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<BuildTemplate>>("/api/build-templates", cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<BuildJob>> GetBuildJobsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<BuildJob>>("/api/build-jobs", cancellationToken)
            ?? [];
    }

    public async Task<RepositoryProbeResult> ProbeRepositoryAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/repositories/probe", new RepositoryProbeRequest(repositoryPath), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepositoryProbeResult>(cancellationToken)
            ?? throw new InvalidOperationException("DeployPilot API returned an empty repository probe response.");
    }

    public async Task<IReadOnlyList<VersionRecord>> GetVersionHistoryAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<IReadOnlyList<VersionRecord>>($"/api/modules/{moduleId}/versions", cancellationToken)
            ?? [];
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(Guid moduleId, string currentVersion, CancellationToken cancellationToken = default)
    {
        var escapedVersion = Uri.EscapeDataString(currentVersion);
        return await httpClient.GetFromJsonAsync<UpdateCheckResult>($"/api/launcher/modules/{moduleId}/updates?currentVersion={escapedVersion}", cancellationToken)
            ?? throw new InvalidOperationException("DeployPilot API returned an empty update response.");
    }

    public async Task<DemoSeedResult> SeedDemoAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync("/api/demo/seed", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DemoSeedResult>(cancellationToken)
            ?? throw new InvalidOperationException("DeployPilot API returned an empty demo seed response.");
    }

    public async Task<Stream> DownloadArtifactAsync(string artifactUrl, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(artifactUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException("Artifact was not found.", artifactUrl);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task DownloadArtifactAsync(
        Uri artifactUri,
        Stream destination,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(artifactUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException("Artifact was not found.", artifactUri.ToString());
        }

        response.EnsureSuccessStatusCode();
        var totalBytes = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[64 * 1024];
        var bytesReceived = 0L;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            bytesReceived += read;

            var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
            progress?.Report(new DownloadProgress(bytesReceived, totalBytes, bytesReceived / seconds));
        }
    }
}
