using YepPet.Domain.Users.ValueObjects;

namespace YepPet.Application.Factories;

public interface IUserProfileFactory
{
    UserProfile Create(string displayName, string city, string country, string bio, string? avatarUrl);
}
