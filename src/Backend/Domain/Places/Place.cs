using YepPet.Domain.Common;
using YepPet.Domain.Places.ValueObjects;

namespace YepPet.Domain.Places;

public sealed class Place : AggregateRoot<Guid>
{
    private readonly List<string> _features = [];
    private readonly List<string> _tags = [];

    public Place(
        Guid id,
        string name,
        PlaceType type,
        string shortDescription,
        string description,
        string coverImageUrl,
        PostalAddress address,
        GeoLocation location,
        PetPolicy petPolicy,
        Pricing pricing,
        RatingSnapshot rating) : base(id)
    {
        Rename(name);
        UpdateDescriptions(shortDescription, description);
        SetCoverImage(coverImageUrl);
        Type = type;
        Address = address;
        Location = location;
        PetPolicy = petPolicy;
        Pricing = pricing;
        Rating = rating;
    }

    public string Name { get; private set; } = string.Empty;

    public PlaceType Type { get; private set; }

    public string ShortDescription { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string CoverImageUrl { get; private set; } = string.Empty;

    public PostalAddress Address { get; private set; }

    public GeoLocation Location { get; private set; }

    public PetPolicy PetPolicy { get; private set; }

    public Pricing Pricing { get; private set; }

    public RatingSnapshot Rating { get; private set; }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public IReadOnlyCollection<string> Features => _features.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Place name is required.");
        }

        Name = name.Trim();
    }

    public void UpdateDescriptions(string shortDescription, string description)
    {
        if (string.IsNullOrWhiteSpace(shortDescription))
        {
            throw new DomainRuleException("Short description is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainRuleException("Description is required.");
        }

        ShortDescription = shortDescription.Trim();
        Description = description.Trim();
    }

    public void SetCoverImage(string coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(coverImageUrl))
        {
            throw new DomainRuleException("Cover image is required.");
        }

        CoverImageUrl = coverImageUrl.Trim();
    }

    public void Relocate(PostalAddress address, GeoLocation location)
    {
        Address = address;
        Location = location;
    }

    public void UpdatePetPolicy(PetPolicy petPolicy)
    {
        PetPolicy = petPolicy;
    }

    public void UpdatePricing(Pricing pricing)
    {
        Pricing = pricing;
    }

    public void UpdateRating(RatingSnapshot rating)
    {
        Rating = rating;
    }

    public void ReplaceTags(IEnumerable<string> tags)
    {
        _tags.Clear();
        _tags.AddRange(NormalizeLabels(tags));
    }

    public void ReplaceFeatures(IEnumerable<string> features)
    {
        _features.Clear();
        _features.AddRange(NormalizeLabels(features));
    }

    private static IReadOnlyCollection<string> NormalizeLabels(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
