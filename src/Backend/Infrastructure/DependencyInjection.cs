using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YepPet.Infrastructure.Persistence;

namespace YepPet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("YepPet")
            ?? "Host=localhost;Port=5433;Database=yeppet;Username=app;Password=app";

        services.AddDbContext<YepPetDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
