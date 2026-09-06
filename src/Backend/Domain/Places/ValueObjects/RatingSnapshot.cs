using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places.ValueObjects;

public sealed class RatingSnapshot : ValueObject
{
    public RatingSnapshot(decimal average, int reviewCount)
    {
        if (average is < 0 or > 5)
        {
            throw new DomainRuleException("La mitjana ha de ser entre 0 i 5.");
        }

        if (reviewCount < 0)
        {
            throw new DomainRuleException("El nombre de ressenyes no pot ser negatiu.");
        }

        Average = average;
        ReviewCount = reviewCount;
    }

    public decimal Average { get; }

    public int ReviewCount { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Average;
        yield return ReviewCount;
    }
}
