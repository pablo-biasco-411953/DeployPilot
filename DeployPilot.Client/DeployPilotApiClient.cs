using System.Net;
using System.Net.Http.Json;
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
}
