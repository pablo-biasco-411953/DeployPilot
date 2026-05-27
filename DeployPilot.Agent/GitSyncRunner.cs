using System.Diagnostics;
using DeployPilot.Shared;

namespace DeployPilot.Agent;

public interface IGitSyncRunner
{
    Task<GitSyncResult> RunAsync(GitSyncPlan plan, bool executeGit, CancellationToken cancellationToken);
}

public sealed class GitSyncRunner(ILogger<GitSyncRunner> logger) : IGitSyncRunner
{
    public async Task<GitSyncResult> RunAsync(GitSyncPlan plan, bool executeGit, CancellationToken cancellationToken)
    {
        try
        {
            if (!executeGit)
            {
                logger.LogInformation("Dry-run Git sync for repository {RepositoryId}: {CommandCount} command(s).", plan.RepositoryId, plan.Commands.Count);
                await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
                return new GitSyncResult(true, "Git sync dry-run completed.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(plan.RepositoryPath) ?? ".");

            if (Directory.Exists(Path.Combine(plan.RepositoryPath, ".git")))
            {
                await RunGitCommandAsync(new[] { "-C", plan.RepositoryPath, "remote", "set-url", "origin", plan.RemoteUrl }, cancellationToken);
                foreach (var command in plan.Commands.Skip(1))
                {
                    await RunGitCommandAsync(command, cancellationToken);
                }

                return new GitSyncResult(true, "Existing repository synchronized.");
            }

            if (Directory.Exists(plan.RepositoryPath) && Directory.EnumerateFileSystemEntries(plan.RepositoryPath).Any())
            {
                return new GitSyncResult(false, $"Repository path is not empty and is not a Git repository: {plan.RepositoryPath}");
            }

            await RunGitCommandAsync(plan.Commands[0], cancellationToken);
            foreach (var command in plan.Commands.Skip(1))
            {
                await RunGitCommandAsync(command, cancellationToken);
            }

            return new GitSyncResult(true, "Repository cloned and synchronized.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Git synchronization failed for repository {RepositoryId}.", plan.RepositoryId);
            return new GitSyncResult(false, ex.Message);
        }
    }

    private static async Task RunGitCommandAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Git process could not be started.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await errorTask;
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Git command failed." : error.Trim());
        }

        await outputTask;
    }
}
