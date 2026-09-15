using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Users;

public sealed class ExternalIdentity : Entity<Guid>
{
    public ExternalIdentity(Guid id, Guid userId, string provider, string subject, DateTimeOffset linkedAtUtc) : base(id)
    {
        if (userId == Guid.Empty) throw new DomainRuleException("L'usuari de la identitat externa és obligatori.");
        if (string.IsNullOrWhiteSpace(provider)) throw new DomainRuleException("El proveïdor és obligatori.");
        if (string.IsNullOrWhiteSpace(subject)) throw new DomainRuleException("L'identificador extern és obligatori.");
        UserId = userId;
        Provider = provider.Trim().ToLowerInvariant();
        Subject = subject.Trim();
        LinkedAtUtc = linkedAtUtc;
    }

    public Guid UserId { get; }
    public string Provider { get; }
    public string Subject { get; }
    public DateTimeOffset LinkedAtUtc { get; }
}
