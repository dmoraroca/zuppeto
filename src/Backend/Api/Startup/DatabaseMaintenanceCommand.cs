using Microsoft.EntityFrameworkCore;
using Zuppeto.Infrastructure.Auth;
using Zuppeto.Infrastructure.Persistence;

namespace Zuppeto.Api.Startup;

public static class DatabaseMaintenanceCommand
{
    public static async Task RunAsync(
        DatabaseMaintenanceMode mode,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        if (mode == DatabaseMaintenanceMode.None)
        {
            throw new InvalidOperationException("Cal indicar una operació explícita de manteniment.");
        }

        await using var scope = services.CreateAsyncScope();

        if (mode is DatabaseMaintenanceMode.Migrate or DatabaseMaintenanceMode.InitializeDevelopmentData)
        {
            var database = scope.ServiceProvider.GetRequiredService<ZuppetoDbContext>();
            await database.Database.MigrateAsync(cancellationToken);
            if (mode == DatabaseMaintenanceMode.Migrate)
            {
                return;
            }
        }

        await scope.ServiceProvider
            .GetRequiredService<DevelopmentIdentitySeeder>()
            .SeedAsync(cancellationToken);
        await scope.ServiceProvider
            .GetRequiredService<DevelopmentPlacesSeeder>()
            .SeedAsync(cancellationToken);
        await scope.ServiceProvider
            .GetRequiredService<TerritorialPilotConfigurationSeeder>()
            .SeedAsync(cancellationToken);
    }
}
