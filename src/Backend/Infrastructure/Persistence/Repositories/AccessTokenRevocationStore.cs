using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Auth;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class AccessTokenRevocationStore(ZuppetoDbContext dbContext) : IAccessTokenRevocationStore
{
    public async Task RevokeAsync(
        Guid userId,
        string tokenId,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedTokenId = tokenId.Trim();
        if (normalizedTokenId.Length == 0) return;

        var nowUtc = DateTimeOffset.UtcNow;
        await dbContext.RevokedAccessTokens
            .Where(token => token.ExpiresAtUtc <= nowUtc)
            .ExecuteDeleteAsync(cancellationToken);

        if (!await dbContext.RevokedAccessTokens.AnyAsync(
                token => token.TokenId == normalizedTokenId,
                cancellationToken))
        {
            dbContext.RevokedAccessTokens.Add(new RevokedAccessTokenRecord
            {
                TokenId = normalizedTokenId,
                UserId = userId,
                ExpiresAtUtc = expiresAtUtc,
                RevokedAtUtc = nowUtc
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var normalizedTokenId = tokenId.Trim();
        return normalizedTokenId.Length == 0
            ? Task.FromResult(false)
            : dbContext.RevokedAccessTokens.AnyAsync(
                token => token.TokenId == normalizedTokenId && token.ExpiresAtUtc > DateTimeOffset.UtcNow,
                cancellationToken);
    }
}
