namespace Zuppeto.Application.Auth;

public interface IAccessTokenRevocationStore
{
    Task RevokeAsync(
        Guid userId,
        string tokenId,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default);
}
