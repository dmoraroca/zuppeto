namespace Zuppeto.Infrastructure.Persistence;

public enum DatabaseMaintenanceMode
{
    None,
    Migrate,
    SeedDevelopmentData,
    InitializeDevelopmentData
}

public static class DatabaseMaintenanceRequest
{
    private const string Prefix = "--database-maintenance=";

    public static DatabaseMaintenanceMode Parse(IEnumerable<string> arguments)
    {
        var values = arguments
            .Where(argument => argument.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .Select(argument => argument[Prefix.Length..].Trim())
            .ToArray();

        if (values.Length == 0)
        {
            return DatabaseMaintenanceMode.None;
        }

        if (values.Length > 1)
        {
            throw new InvalidOperationException("Només es permet una operació explícita de manteniment de base de dades.");
        }

        return values[0].ToLowerInvariant() switch
        {
            "migrate" => DatabaseMaintenanceMode.Migrate,
            "seed-development-data" => DatabaseMaintenanceMode.SeedDevelopmentData,
            "initialize-development-data" => DatabaseMaintenanceMode.InitializeDevelopmentData,
            _ => throw new InvalidOperationException("Operació de manteniment de base de dades desconeguda.")
        };
    }
}
