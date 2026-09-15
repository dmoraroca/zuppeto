using System.Collections.Concurrent;

namespace Zuppeto.Infrastructure.Email;

/// <summary>Ephemeral Development-only inbox. It is neither persisted nor logged.</summary>
public sealed class DevelopmentActivationInbox
{
    private readonly ConcurrentDictionary<string, string> tokens = new(StringComparer.OrdinalIgnoreCase);

    public void Store(string email, string token) => tokens[email.Trim()] = token;

    public bool TryTake(string email, out string token) => tokens.TryGetValue(email.Trim(), out token!);
}
