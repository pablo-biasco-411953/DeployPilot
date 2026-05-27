using DeployPilot.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeployPilot.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeployPilotPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"] ?? "InMemory";
        var connectionString = configuration["Persistence:ConnectionString"];

        services.AddDbContext<DeployPilotDbContext>(options =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                RequireConnectionString(connectionString, provider);
                options.UseNpgsql(connectionString);
                return;
            }

            if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase) || provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                RequireConnectionString(connectionString, provider);
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                return;
            }

            options.UseInMemoryDatabase(configuration["Persistence:DatabaseName"] ?? "deploypilot-dev");
        });

        services.AddScoped<IDeployPilotStore, EfDeployPilotStore>();
        return services;
    }

    public static void EnsureDeployPilotDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeployPilotDbContext>();
        dbContext.Database.EnsureCreated();
    }

    private static void RequireConnectionString(string? connectionString, string provider)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Persistence provider '{provider}' requires Persistence:ConnectionString.");
        }
    }
}
