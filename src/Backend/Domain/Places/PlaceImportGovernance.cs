using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places;

public enum PlaceImportStatus
{
    Prepared,
    Running,
    Validated,
    PendingReview,
    Approved,
    Published,
    Failed,
    Cancelled
}

public static class PlaceImportGovernance
{
    public static void EnsureTransition(PlaceImportStatus current, PlaceImportStatus next)
    {
        var allowed = (current, next) switch
        {
            (PlaceImportStatus.Prepared, PlaceImportStatus.Running) => true,
            (PlaceImportStatus.Running, PlaceImportStatus.Validated) => true,
            (PlaceImportStatus.Validated, PlaceImportStatus.PendingReview) => true,
            (PlaceImportStatus.PendingReview, PlaceImportStatus.Approved) => true,
            (PlaceImportStatus.Approved, PlaceImportStatus.Published) => true,
            (PlaceImportStatus.Prepared or PlaceImportStatus.Running, PlaceImportStatus.Failed) => true,
            (PlaceImportStatus.Prepared or PlaceImportStatus.Running or PlaceImportStatus.PendingReview, PlaceImportStatus.Cancelled) => true,
            (PlaceImportStatus.Failed, PlaceImportStatus.Prepared) => true,
            _ => false
        };

        if (!allowed)
            throw new DomainRuleException($"Transició d’importació no permesa: {current} → {next}.");
    }
}
