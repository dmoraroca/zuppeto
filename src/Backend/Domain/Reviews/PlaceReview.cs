using YepPet.Domain.Common;

namespace YepPet.Domain.Reviews;

public sealed class PlaceReview : AggregateRoot<Guid>
{
    public PlaceReview(
        Guid id,
        Guid placeId,
        Guid authorUserId,
        int score,
        string comment,
        DateTimeOffset createdAtUtc) : base(id)
    {
        if (placeId == Guid.Empty)
        {
            throw new DomainRuleException("Place id is required.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new DomainRuleException("Author user id is required.");
        }

        PlaceId = placeId;
        AuthorUserId = authorUserId;
        CreatedAtUtc = createdAtUtc;
        ChangeScore(score);
        UpdateComment(comment);
        IsVisible = true;
    }

    public Guid PlaceId { get; }

    public Guid AuthorUserId { get; }

    public int Score { get; private set; }

    public string Comment { get; private set; } = string.Empty;

    public bool IsVisible { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public void ChangeScore(int score)
    {
        if (score is < 1 or > 5)
        {
            throw new DomainRuleException("Review score must be between 1 and 5.");
        }

        Score = score;
    }

    public void UpdateComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainRuleException("Review comment is required.");
        }

        Comment = comment.Trim();
    }

    public void Hide()
    {
        IsVisible = false;
    }

    public void Show()
    {
        IsVisible = true;
    }
}
