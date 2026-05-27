using DeployPilot.Agent;
using DeployPilot.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<RecipeSelector>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
