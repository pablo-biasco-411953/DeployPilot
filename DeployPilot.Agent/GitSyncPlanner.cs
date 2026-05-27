using DeployPilot.Shared;

namespace DeployPilot.Agent;

public sealed class GitSyncPlanner
{
    public GitSyncPlan CreatePlan(AgentBuildLease lease, AgentOptions options)
    {
        var repositoryPath = Path.GetFullPath(Path.Combine(options.WorkspaceRoot, lease.Repository.Id.ToString("N")));
        var checkoutTarget = string.IsNullOrWhiteSpace(lease.Job.RequestedSha)
            ? lease.Repository.DefaultBranch
            : lease.Job.RequestedSha;

        var commands = new List<IReadOnlyList<string>>
        {
            new[] { "clone", "--no-checkout", lease.Repository.RemoteUrl, repositoryPath },
            new[] { "-C", repositoryPath, "fetch", "--all", "--tags", "--prune" },
            new[] { "-C", repositoryPath, "checkout", checkoutTarget ?? lease.Repository.DefaultBranch },
            new[] { "-C", repositoryPath, "reset", "--hard", checkoutTarget ?? lease.Repository.DefaultBranch }
        };

        return new GitSyncPlan(
            lease.Repository.Id,
            lease.Repository.RemoteUrl,
            lease.Repository.DefaultBranch,
            lease.Job.RequestedSha,
            repositoryPath,
            commands);
    }
}
