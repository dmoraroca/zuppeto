namespace Zuppeto.Infrastructure.Persistence.Entities;

public sealed class MissingPlaceProposalRecord
{
    public Guid Id { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string ApproximateAddress { get; set; } = string.Empty;
    public string? AdditionalInformation { get; set; }
    public Guid? PossibleDuplicatePlaceId { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
}
