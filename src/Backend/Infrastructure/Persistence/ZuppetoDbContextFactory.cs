using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zuppeto.Infrastructure.Persistence;

public sealed class ZuppetoDbContextFactory : IDesignTimeDbContextFactory<ZuppetoDbContext>
{
    public ZuppetoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Zuppeto")
            ?? Environment.GetEnvironmentVariable("ZUPPETO_DB_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta ConnectionStrings__Zuppeto o ZUPPETO_DB_CONNECTION_STRING per executar eines EF.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ZuppetoDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ZuppetoDbContext(optionsBuilder.Options);
    }
}
