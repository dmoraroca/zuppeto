namespace Zuppeto.Domain.Places;

public enum PlaceLifecycleStatus
{
    Active,
    SuspectedClosed,
    TemporarilyClosed,
    Closed,
    Unknown
}

public enum PlaceMatchOutcome
{
    ConfirmedMatch,
    ProbableMatch,
    NeedsReview,
    NotMatch
}

public enum PlaceConflictStatus
{
    Open,
    Resolved,
    Dismissed
}

public static class PlaceLifecyclePolicy
{
    public static bool CanApplyAutomatically(PlaceLifecycleStatus current, PlaceLifecycleStatus proposed,
        bool explicitClosureTag, int concordantSources)
    {
        if (proposed == current) return true;
        if (proposed == PlaceLifecycleStatus.SuspectedClosed) return true;
        if (proposed == PlaceLifecycleStatus.TemporarilyClosed) return explicitClosureTag;
        if (proposed == PlaceLifecycleStatus.Closed) return explicitClosureTag && concordantSources >= 2;
        return proposed == PlaceLifecycleStatus.Active && current is PlaceLifecycleStatus.Unknown or PlaceLifecycleStatus.SuspectedClosed;
    }
}

public static class PlaceObservationAuthority
{
    public static int Rank(string authority) => authority.ToUpperInvariant() switch
    {
        "MANUAL_VERIFIED" => 3,
        "MANUAL" => 2,
        "AUTO" => 1,
        _ => 0
    };

    public static bool MayReplace(string currentAuthority, string proposedAuthority) =>
        Rank(proposedAuthority) > Rank(currentAuthority);
}
