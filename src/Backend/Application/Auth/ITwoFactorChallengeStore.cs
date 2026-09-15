namespace Zuppeto.Application.Auth;
public sealed record TwoFactorChallenge(Guid UserId, string Provider, bool RequiresProfileCompletion, DateTimeOffset ExpiresAtUtc);
public interface ITwoFactorChallengeStore
{
    string Create(Guid userId, string provider, bool requiresProfileCompletion = false);
    bool TryConsume(string challengeId, out TwoFactorChallenge challenge);
    bool TryUseTotpTimeStep(Guid userId, long timeStep);
    void RevokeForUser(Guid userId);
}
