using Microsoft.EntityFrameworkCore;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Domain.Users;
using Zuppeto.Infrastructure.Persistence.Entities;
using Zuppeto.Infrastructure.Persistence.Mappings;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ZuppetoDbContext dbContext) : IUserRepository
{
    public async Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        return records
            .Select(UserPersistenceMapper.ToDomain)
            .ToArray();
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

        return record is null ? null : UserPersistenceMapper.ToDomain(record);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var record = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        return record is null ? null : UserPersistenceMapper.ToDomain(record);
    }

    public async Task<User?> GetByActivationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.ActivationTokenHash == tokenHash, cancellationToken);

        return record is null ? null : UserPersistenceMapper.ToDomain(record);
    }

    public async Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.PasswordResetTokenHash == tokenHash, cancellationToken);
        return record is null ? null : UserPersistenceMapper.ToDomain(record);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var record = UserPersistenceMapper.ToRecord(user);

        await dbContext.Users.AddAsync(record, cancellationToken);
        AppendConsentEventIfNeeded(record, null);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Users
            .Include(current => current.PrivacyConsentEvents)
            .FirstOrDefaultAsync(current => current.Id == user.Id, cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException("No s’ha trobat l’usuari.");
        }

        var previousAccepted = record.PrivacyAccepted;
        var previousAcceptedAtUtc = record.PrivacyAcceptedAtUtc;

        UserPersistenceMapper.Apply(user, record);
        AppendConsentEventIfNeeded(record, (previousAccepted, previousAcceptedAtUtc));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Users
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken);

        if (record is null)
        {
            return;
        }

        dbContext.Users.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal void AppendConsentEventIfNeeded(
        UserRecord record,
        (bool accepted, DateTimeOffset? acceptedAtUtc)? previousState)
    {
        if (previousState is not null &&
            previousState.Value.accepted == record.PrivacyAccepted &&
            previousState.Value.acceptedAtUtc == record.PrivacyAcceptedAtUtc)
        {
            return;
        }

        var consentEvent = new PrivacyConsentEventRecord
        {
            Id = Guid.NewGuid(),
            UserId = record.Id,
            Accepted = record.PrivacyAccepted,
            RegisteredAtUtc = record.PrivacyAcceptedAtUtc ?? DateTimeOffset.UtcNow,
            Source = previousState is null ? "repository-add" : "repository-update"
        };

        record.PrivacyConsentEvents.Add(consentEvent);

        // The key is generated client-side while the model also has a database default.
        // When the parent is already tracked EF can infer Modified for this new child,
        // which issues an UPDATE against a row that does not exist. Make the append-only
        // audit event state explicit so profile consent is persisted with INSERT.
        dbContext.Entry(consentEvent).State = EntityState.Added;
    }
}
