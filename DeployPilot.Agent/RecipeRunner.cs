using System.Diagnostics;
using DeployPilot.Shared;

namespace DeployPilot.Agent;

public interface IRecipeRunner
{
    Task<RecipeExecutionResult> RunAsync(RecipeExecutionPlan plan, bool executeRecipe, CancellationToken cancellationToken);
}

public sealed class PowerShellRecipeRunner(ILogger<PowerShellRecipeRunner> logger) : IRecipeRunner
{
    public async Task<RecipeExecutionResult> RunAsync(RecipeExecutionPlan plan, bool executeRecipe, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(plan.RepositoryPath);
        Directory.CreateDirectory(plan.OutputPath);

        if (!executeRecipe)
        {
            logger.LogInformation("Dry-run recipe execution for job {BuildJobId}: powershell {Arguments}", plan.BuildJobId, string.Join(' ', plan.Arguments));
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            return new RecipeExecutionResult(true, 0, "Recipe dry-run completed.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in plan.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("PowerShell process could not be started.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;
        var message = process.ExitCode == 0
            ? string.IsNullOrWhiteSpace(output) ? "Recipe completed successfully." : output.Trim()
            : string.IsNullOrWhiteSpace(error) ? "Recipe failed." : error.Trim();

        return new RecipeExecutionResult(process.ExitCode == 0, process.ExitCode, message);
    }
}
