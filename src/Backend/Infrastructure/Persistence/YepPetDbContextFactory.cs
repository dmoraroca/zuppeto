using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YepPet.Infrastructure.Persistence;

public sealed class YepPetDbContextFactory : IDesignTimeDbContextFactory<YepPetDbContext>
{
    public YepPetDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__YepPet")
            ?? Environment.GetEnvironmentVariable("YEP_PET_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5433;Database=yeppet;Username=app;Password=app";

        var optionsBuilder = new DbContextOptionsBuilder<YepPetDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new YepPetDbContext(optionsBuilder.Options);
    }
}
