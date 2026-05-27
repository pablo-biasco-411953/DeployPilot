namespace DeployPilot.Client;

public static class DeployPilotClientFactory
{
    public static DeployPilotApiClient Create(string apiBaseUrl)
    {
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("API base URL must be absolute.", nameof(apiBaseUrl));
        }

        return new DeployPilotApiClient(new HttpClient { BaseAddress = uri });
    }
}
