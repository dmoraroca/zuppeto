namespace YepPet.Infrastructure.Persistence.Entities;

public sealed class PlaceSearchQueryResultRecord
{
    public Guid QueryId { get; set; }

    public PlaceSearchQueryRecord Query { get; set; } = null!;

    public Guid PlaceId { get; set; }

    public PlaceRecord Place { get; set; } = null!;

    public int Rank { get; set; }

    public DateTimeOffset CapturedAtUtc { get; set; }
}
