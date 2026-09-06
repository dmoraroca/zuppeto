using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Users.ValueObjects;

public sealed class UserProfile : ValueObject
{
    public UserProfile(string displayName, string city, string country, string comments, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainRuleException("Display name is required.");
        }

        DisplayName = displayName.Trim();
        City = city.Trim();
        Country = country.Trim();
        Comments = comments.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }

    public string DisplayName { get; }

    public string City { get; }

    public string Country { get; }

    public string Comments { get; }

    public string? AvatarUrl { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DisplayName;
        yield return City;
        yield return Country;
        yield return Comments;
        yield return AvatarUrl;
    }
}
