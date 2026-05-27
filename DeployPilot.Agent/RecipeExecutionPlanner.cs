using DeployPilot.Shared;

namespace DeployPilot.Agent;

public sealed class RecipeExecutionPlanner
{
    public RecipeExecutionPlan CreatePlan(AgentBuildLease lease, AgentOptions options)
    {
        var repositoryFolder = lease.Repository.Id.ToString("N");
        var repositoryPath = Path.GetFullPath(Path.Combine(options.WorkspaceRoot, repositoryFolder));
        var outputPath = Path.GetFullPath(Path.Combine(options.OutputRoot, lease.Job.Id.ToString("N")));
        var scriptPath = Path.GetFullPath(lease.Template.ScriptPath);
        var arguments = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            scriptPath,
            "-RepositoryPath",
            repositoryPath,
            "-OutputPath",
            outputPath
        };

        if (!string.IsNullOrWhiteSpace(lease.Repository.ProjectPath))
        {
            arguments.Add("-ProjectPath");
            arguments.Add(lease.Repository.ProjectPath);
        }

        if (!string.IsNullOrWhiteSpace(lease.Repository.BuildCommand))
        {
            arguments.Add("-BuildCommand");
            arguments.Add(lease.Repository.BuildCommand);
        }

        return new RecipeExecutionPlan(
            lease.Job.Id,
            lease.Repository.Technology,
            scriptPath,
            repositoryPath,
            lease.Repository.ProjectPath,
            outputPath,
            lease.Repository.BuildCommand,
            arguments);
    }
}
