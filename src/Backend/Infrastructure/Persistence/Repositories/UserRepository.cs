using Microsoft.EntityFrameworkCore;
using YepPet.Domain.Abstractions;
using YepPet.Domain.Users;
using YepPet.Infrastructure.Persistence.Entities;
using YepPet.Infrastructure.Persistence.Mappings;

namespace YepPet.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(YepPetDbContext dbContext) : IUserRepository
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
            throw new InvalidOperationException($"User '{user.Id}' was not found.");
        }

        var previousAccepted = record.PrivacyAccepted;
        var previousAcceptedAtUtc = record.PrivacyAcceptedAtUtc;

        UserPersistenceMapper.Apply(user, record);
        AppendConsentEventIfNeeded(record, (previousAccepted, previousAcceptedAtUtc));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AppendConsentEventIfNeeded(
        UserRecord record,
        (bool accepted, DateTimeOffset? acceptedAtUtc)? previousState)
    {
        if (previousState is not null &&
            previousState.Value.accepted == record.PrivacyAccepted &&
            previousState.Value.acceptedAtUtc == record.PrivacyAcceptedAtUtc)
        {
            return;
        }

        record.PrivacyConsentEvents.Add(new PrivacyConsentEventRecord
        {
            Id = Guid.NewGuid(),
            UserId = record.Id,
            Accepted = record.PrivacyAccepted,
            RegisteredAtUtc = record.PrivacyAcceptedAtUtc ?? DateTimeOffset.UtcNow,
            Source = previousState is null ? "repository-add" : "repository-update"
        });
    }
}
