namespace DeployPilot.Orchestrator;

using DeployPilot.Shared;

public class Worker(ILogger<Worker> logger, BuildQueue queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SeedDemoQueue();

        while (!stoppingToken.IsCancellationRequested)
        {
            var started = queue.TryStartNext(DateTimeOffset.UtcNow);
            if (started is not null)
            {
                logger.LogInformation(
                    "Started build job {JobId} for organization {OrganizationId}, repository {RepositoryId}, module {ModuleId}.",
                    started.Id,
                    started.OrganizationId,
                    started.RepositoryId,
                    started.ModuleId);

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                queue.Complete(started.Id, succeeded: true, DateTimeOffset.UtcNow);
                logger.LogInformation("Completed build job {JobId}.", started.Id);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private void SeedDemoQueue()
    {
        if (queue.Snapshot().Count > 0)
        {
            return;
        }

        var organizationId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var job = new BuildJob(
            Guid.NewGuid(),
            organizationId,
            repositoryId,
            applicationId,
            moduleId,
            "demo",
            "1.0.0",
            null,
            BuildJobStatus.Queued,
            0,
            DateTimeOffset.UtcNow,
            null,
            null);

        queue.Enqueue(job);
    }
}
