using Zuppeto.Domain.Users.ValueObjects;

namespace Zuppeto.Application.Factories;

public sealed class UserProfileFactory : IUserProfileFactory
{
    public UserProfile Create(string displayName, string city, string country, string comments, string? avatarUrl)
    {
        return new UserProfile(displayName, city, country, comments, avatarUrl);
    }
}
