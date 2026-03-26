using YepPet.Domain.Common;

namespace YepPet.Domain.Users.ValueObjects;

public sealed class PrivacyConsent : ValueObject
{
    public PrivacyConsent(bool accepted, DateTimeOffset? acceptedAtUtc)
    {
        if (accepted && acceptedAtUtc is null)
        {
            throw new DomainRuleException("Accepted consent must include acceptance timestamp.");
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
