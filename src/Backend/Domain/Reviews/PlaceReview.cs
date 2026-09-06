using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Reviews;

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
            throw new DomainRuleException("El lloc és obligatori.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new DomainRuleException("L’autor és obligatori.");
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
            throw new DomainRuleException("La puntuació ha de ser entre 1 i 5.");
        }

        Score = score;
    }

    public void UpdateComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainRuleException("El comentari de la ressenya és obligatori.");
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
