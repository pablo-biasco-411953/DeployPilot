using System.Net;
using System.Net.Http.Json;
using DeployPilot.Shared;

namespace DeployPilot.Agent;

public sealed class DeployPilotApiClient(HttpClient httpClient)
{
    public async Task<AgentBuildLease?> LeaseBuildJobAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync("/api/agents/build-jobs/lease", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentBuildLease>(cancellationToken);
    }

    public async Task ReportEventAsync(Guid buildJobId, BuildEventLevel level, string message, int progress, CancellationToken cancellationToken)
    {
        var request = new BuildJobEventRequest(level, message, progress);
        using var response = await httpClient.PostAsJsonAsync($"/api/build-jobs/{buildJobId}/events", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CompleteBuildJobAsync(Guid buildJobId, bool succeeded, string message, CancellationToken cancellationToken)
    {
        var request = new BuildJobCompletionRequest(succeeded, message);
        using var response = await httpClient.PostAsJsonAsync($"/api/agents/build-jobs/{buildJobId}/complete", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
