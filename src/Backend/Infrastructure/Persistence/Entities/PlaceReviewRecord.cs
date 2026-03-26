namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class PlaceReviewRecord
{
    public Guid Id { get; set; }

    public Guid PlaceId { get; set; }

    public Guid AuthorUserId { get; set; }

    public int Score { get; set; }

    public string Comment { get; set; } = string.Empty;

    public bool IsVisible { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public PlaceRecord Place { get; set; } = null!;

    public UserRecord AuthorUser { get; set; } = null!;
}
