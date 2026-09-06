using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Zuppeto.Infrastructure.Persistence;

/// <summary>
/// Truncates string properties to HasMaxLength so PostgreSQL never raises 22001.
/// </summary>
public sealed class ClampStringMaxLengthInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Clamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Clamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Clamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType != typeof(string))
                {
                    continue;
                }

                var maxLength = property.Metadata.GetMaxLength();
                if (maxLength is null or <= 0)
                {
                    continue;
                }

                if (property.CurrentValue is string value && value.Length > maxLength)
                {
                    property.CurrentValue = value[..maxLength.Value];
                }
            }
        }
    }
}
