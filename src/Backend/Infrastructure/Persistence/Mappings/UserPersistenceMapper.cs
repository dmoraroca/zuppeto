using YepPet.Domain.Users;
using YepPet.Domain.Users.ValueObjects;
using YepPet.Infrastructure.Persistence.Entities;

namespace YepPet.Infrastructure.Persistence.Mappings;

internal static class UserPersistenceMapper
{
    public static User ToDomain(UserRecord record)
    {
        return new User(
            record.Id,
            record.Email,
            record.PasswordHash,
            record.Role.Trim(),
            new UserProfile(
                record.DisplayName ?? record.Email,
                record.City ?? string.Empty,
                record.Country ?? string.Empty,
                record.Bio ?? string.Empty,
                record.AvatarUrl),
            new PrivacyConsent(record.PrivacyAccepted, record.PrivacyAcceptedAtUtc),
            record.CreatedAtUtc,
            record.LastAccessedAtUtc);
    }

    public static UserRecord ToRecord(User user)
    {
        var record = new UserRecord();
        Apply(user, record);
        return record;
    }

    public static void Apply(User user, UserRecord record)
    {
        record.Id = user.Id;
        record.Email = user.Email;
        record.PasswordHash = user.PasswordHash;
        record.Role = user.Role;
        record.DisplayName = user.Profile.DisplayName;
        record.City = user.Profile.City;
        record.Country = user.Profile.Country;
        record.Bio = user.Profile.Bio;
        record.AvatarUrl = user.Profile.AvatarUrl;
        record.PrivacyAccepted = user.PrivacyConsent.Accepted;
        record.PrivacyAcceptedAtUtc = user.PrivacyConsent.AcceptedAtUtc;
        record.CreatedAtUtc = user.CreatedAtUtc;
        record.LastAccessedAtUtc = user.LastAccessedAtUtc;
    }
}
