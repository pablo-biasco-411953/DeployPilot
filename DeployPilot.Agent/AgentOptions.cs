namespace DeployPilot.Agent;

public sealed class AgentOptions
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5080";

    public string WorkspaceRoot { get; set; } = "workspace";

    public string OutputRoot { get; set; } = "artifacts";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public bool ExecuteRecipes { get; set; }

    public bool ExecuteGit { get; set; }
}
