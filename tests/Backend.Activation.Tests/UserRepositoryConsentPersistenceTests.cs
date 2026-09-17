using Microsoft.EntityFrameworkCore;
using Xunit;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Repositories;

namespace Backend.Activation.Tests;

public sealed class UserRepositoryConsentPersistenceTests
{
    [Fact]
    public void New_consent_event_is_tracked_as_added_when_updating_an_existing_user()
    {
        using var dbContext = CreateDbContext();
        var user = new UserRecord
        {
            Id = Guid.NewGuid(),
            Email = "consent-regression@zuppeto.test",
            Role = "User",
            PrivacyAccepted = true,
            PrivacyAcceptedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Attach(user);

        var repository = new UserRepository(dbContext);
        repository.AppendConsentEventIfNeeded(user, (false, null));

        var consentEvent = Assert.Single(user.PrivacyConsentEvents);
        Assert.Equal(EntityState.Added, dbContext.Entry(consentEvent).State);
        Assert.True(consentEvent.Accepted);
        Assert.Equal("repository-update", consentEvent.Source);
    }

    [Fact]
    public void Unchanged_consent_does_not_append_an_audit_event()
    {
        using var dbContext = CreateDbContext();
        var acceptedAtUtc = DateTimeOffset.UtcNow;
        var user = new UserRecord
        {
            Id = Guid.NewGuid(),
            Email = "unchanged-consent@zuppeto.test",
            Role = "User",
            PrivacyAccepted = true,
            PrivacyAcceptedAtUtc = acceptedAtUtc
        };
        dbContext.Attach(user);

        var repository = new UserRepository(dbContext);
        repository.AppendConsentEventIfNeeded(user, (true, acceptedAtUtc));

        Assert.Empty(user.PrivacyConsentEvents);
    }

    private static ZuppetoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ZuppetoDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;

        return new ZuppetoDbContext(options);
    }
}
