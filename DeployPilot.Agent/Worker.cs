namespace DeployPilot.Agent;

using DeployPilot.Shared;

public class Worker(ILogger<Worker> logger, RecipeSelector recipeSelector) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var repository = new RepositoryDefinition(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "demo-repository",
                "https://example.com/repository.git",
                "main",
                BuildTechnology.DotNetSdk,
                "src/DemoApp/DemoApp.csproj",
                null,
                DateTimeOffset.UtcNow);

            var template = recipeSelector.Select(repository, new InMemoryDeployPilotStore().BuildTemplates);
            logger.LogInformation("Agent heartbeat. Selected recipe {RecipeName} at {ScriptPath}.", template.Name, template.ScriptPath);
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
