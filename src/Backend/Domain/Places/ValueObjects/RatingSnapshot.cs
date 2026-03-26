using YepPet.Domain.Common;

namespace YepPet.Domain.Places.ValueObjects;

public sealed class RatingSnapshot : ValueObject
{
    public RatingSnapshot(decimal average, int reviewCount)
    {
        if (average is < 0 or > 5)
        {
            throw new DomainRuleException("Average rating must be between 0 and 5.");
        }

        if (reviewCount < 0)
        {
            throw new DomainRuleException("Review count cannot be negative.");
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
