using System.Collections.Concurrent;
using System.Security.Cryptography;
using Zuppeto.Application.Auth;

namespace Zuppeto.Infrastructure.Auth;

internal sealed class MemoryTwoFactorChallengeStore : ITwoFactorChallengeStore
{
    private readonly ConcurrentDictionary<string, TwoFactorChallenge> values = new();
    private readonly ConcurrentDictionary<Guid, long> acceptedTimeSteps = new();

    public string Create(Guid userId, string provider, bool requiresProfileCompletion = false)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        values[id] = new(userId, provider, requiresProfileCompletion, DateTimeOffset.UtcNow.AddMinutes(5));
        return id;
    }

    public bool TryConsume(string id, out TwoFactorChallenge challenge)
    {
        if (!values.TryRemove(id, out challenge!)) return false;
        return challenge.ExpiresAtUtc > DateTimeOffset.UtcNow;
    }

    public bool TryUseTotpTimeStep(Guid userId, long timeStep)
    {
        while (true)
        {
            if (!acceptedTimeSteps.TryGetValue(userId, out var current))
                return acceptedTimeSteps.TryAdd(userId, timeStep) || TryUseTotpTimeStep(userId, timeStep);
            if (timeStep <= current) return false;
            if (acceptedTimeSteps.TryUpdate(userId, timeStep, current)) return true;
        }
    }

    public void RevokeForUser(Guid userId)
    {
        foreach (var pair in values.Where(x => x.Value.UserId == userId)) values.TryRemove(pair.Key, out _);
        acceptedTimeSteps.TryRemove(userId, out _);
    }
}
