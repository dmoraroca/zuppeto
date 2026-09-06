using Zuppeto.Domain.Users.ValueObjects;

namespace Zuppeto.Application.Factories;

public interface IUserProfileFactory
{
    UserProfile Create(string displayName, string city, string country, string comments, string? avatarUrl);
}
