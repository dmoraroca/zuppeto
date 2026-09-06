using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Reviews;

public sealed class PlaceReviewQuery : ValueObject
{
    public PlaceReviewQuery(bool onlyVisible, int take)
    {
        if (take <= 0)
        {
            throw new DomainRuleException("El nombre de resultats ha de ser més gran que zero.");
        }

        OnlyVisible = onlyVisible;
        Take = take;
    }

    public bool OnlyVisible { get; }

    public int Take { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return OnlyVisible;
        yield return Take;
    }
}
