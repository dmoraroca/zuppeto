namespace Zuppeto.Application.Results;

public enum FailureKind
{
    None = 0,
    NotFound,
    Conflict,
    Invalid,
    ExternalUnavailable,
    RateLimited,
    Cancelled,
    Unauthorized
}
