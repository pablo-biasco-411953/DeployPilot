using DeployPilot.Agent;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));
builder.Services.AddSingleton<RecipeExecutionPlanner>();
builder.Services.AddSingleton<IRecipeRunner, PowerShellRecipeRunner>();
builder.Services.AddSingleton<GitSyncPlanner>();
builder.Services.AddSingleton<IGitSyncRunner, GitSyncRunner>();
builder.Services.AddHttpClient<DeployPilotApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentOptions>>().Value;
    client.BaseAddress = new Uri(options.ApiBaseUrl);
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
