using DeployPilot.Orchestrator;
using DeployPilot.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<BuildQueue>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
