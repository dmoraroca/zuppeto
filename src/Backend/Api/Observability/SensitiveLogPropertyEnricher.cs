using Serilog.Core;
using Serilog.Events;
using Zuppeto.Infrastructure.Security;

namespace Zuppeto.Api.Observability;

/// <summary>
/// Redacta propietats estructurades amb noms sensibles abans que arribin
/// a qualsevol sink. No substitueix la prohibició de registrar secrets
/// dins del text lliure dels missatges.
/// </summary>
public sealed class SensitiveLogPropertyEnricher : ILogEventEnricher
{
    internal const string RedactedValue = "[REDACTED]";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var propertyName in logEvent.Properties.Keys
                     .Where(SensitiveDataPolicy.IsSensitiveName)
                     .ToArray())
        {
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty(propertyName, RedactedValue));
        }
    }

}
