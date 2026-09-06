using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Users.ValueObjects;

public sealed class PrivacyConsent : ValueObject
{
    public PrivacyConsent(bool accepted, DateTimeOffset? acceptedAtUtc)
    {
        if (accepted && acceptedAtUtc is null)
        {
            throw new DomainRuleException("El consentiment ha d’incloure la data d’acceptació.");
        }

        if (!accepted)
        {
            acceptedAtUtc = null;
        }

        Accepted = accepted;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public bool Accepted { get; }

    public DateTimeOffset? AcceptedAtUtc { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Accepted;
        yield return AcceptedAtUtc;
    }
}
