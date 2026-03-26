using YepPet.Domain.Common;

namespace YepPet.Domain.Users.ValueObjects;

public sealed class UserProfile : ValueObject
{
    public UserProfile(string displayName, string city, string country, string bio, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainRuleException("Display name is required.");
        }

        DisplayName = displayName.Trim();
        City = city.Trim();
        Country = country.Trim();
        Bio = bio.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }

    public string DisplayName { get; }

    public string City { get; }

    public string Country { get; }

    public string Bio { get; }

    public string? AvatarUrl { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DisplayName;
        yield return City;
        yield return Country;
        yield return Bio;
        yield return AvatarUrl;
    }
}
