using YepPet.Domain.Users.ValueObjects;

namespace YepPet.Application.Factories;

public sealed class UserProfileFactory : IUserProfileFactory
{
    public UserProfile Create(string displayName, string city, string country, string bio, string? avatarUrl)
    {
        return new UserProfile(displayName, city, country, bio, avatarUrl);
    }
}
