using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zuppeto.Infrastructure.Persistence;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class LegacyCitiesRetirementPostgreSqlTests
{
    private const string PreviousMigration = "20261011204108_AddTerritorialSourceApprovalWorkflow";

    [Fact]
    public async Task Migration_archives_and_restores_cities_and_menu_state()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ZUPPETO_TERRITORIAL_TEST_DB");
        if (string.IsNullOrWhiteSpace(baseConnection)) return;

        var connectionString = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = "zuppeto_legacy_cities_retirement_tests"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new ZuppetoDbContext(options);
        await db.Database.EnsureDeletedAsync();

        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            var countryId = Guid.NewGuid();
            var cityId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO countries (id, code, name, is_active, sort_order, created_at_utc, updated_at_utc)
                VALUES ({countryId}, 'TEST', 'Test', TRUE, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                INSERT INTO cities (id, country_id, name, normalized_name, is_active, sort_order, created_at_utc, updated_at_utc)
                VALUES ({cityId}, {countryId}, 'Test City', 'TEST CITY', TRUE, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                INSERT INTO menus (id, key, label, route, parent_key, sort_order, is_active)
                VALUES ({Guid.NewGuid()}, 'admin.cities', 'Ciutats', '/admin/ciutats', NULL, 1, FALSE);
                """);

            await migrator.MigrateAsync();
            Assert.Equal(0, await ScalarAsync(connectionString, "SELECT count(*) FROM pg_class WHERE relname = 'cities' AND relkind = 'r'"));
            Assert.Equal(1, await ScalarAsync(connectionString, "SELECT count(*) FROM legacy_cities_archive"));

            await migrator.MigrateAsync(PreviousMigration);
            Assert.Equal(1, await ScalarAsync(connectionString, "SELECT count(*) FROM cities WHERE id = @id", cityId));
            Assert.Equal(1, await ScalarAsync(connectionString, "SELECT count(*) FROM menus WHERE key = 'admin.cities' AND is_active = FALSE"));

            await migrator.MigrateAsync();
            Assert.Equal(1, await ScalarAsync(connectionString, "SELECT count(*) FROM legacy_cities_archive"));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<int> ScalarAsync(string connectionString, string sql, Guid? id = null)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        if (id.HasValue) command.Parameters.AddWithValue("id", id.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
