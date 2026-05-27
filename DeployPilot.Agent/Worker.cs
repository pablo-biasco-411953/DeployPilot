namespace DeployPilot.Agent;

using DeployPilot.Shared;
using Microsoft.Extensions.Options;

public class Worker(
    ILogger<Worker> logger,
    DeployPilotApiClient apiClient,
    RecipeExecutionPlanner planner,
    IRecipeRunner runner,
    IOptions<AgentOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var lease = await apiClient.LeaseBuildJobAsync(stoppingToken);
                if (lease is null)
                {
                    await Task.Delay(options.Value.PollInterval, stoppingToken);
                    continue;
                }

                logger.LogInformation("Leased build job {BuildJobId} using recipe {RecipeName}.", lease.Job.Id, lease.Template.Name);
                await apiClient.ReportEventAsync(lease.Job.Id, BuildEventLevel.Info, "Agent leased the build job.", 10, stoppingToken);

                var plan = planner.CreatePlan(lease, options.Value);
                await apiClient.ReportEventAsync(lease.Job.Id, BuildEventLevel.Info, $"Prepared recipe {lease.Template.Name}.", 25, stoppingToken);

                var result = await runner.RunAsync(plan, options.Value.ExecuteRecipes, stoppingToken);
                await apiClient.CompleteBuildJobAsync(lease.Job.Id, result.Succeeded, result.Message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Agent loop failed. The next poll will retry.");
                await Task.Delay(options.Value.PollInterval, stoppingToken);
            }
        }
    }
}
