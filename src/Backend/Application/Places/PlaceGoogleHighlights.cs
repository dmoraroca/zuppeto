namespace Zuppeto.Application.Places;

/// <summary>
/// Maps Google amenity flags and the resolved type profile to public chips.
/// </summary>
internal static class PlaceGoogleHighlights
{
    internal static IReadOnlyCollection<string> ToFeatureChips(
        PlaceExternalDetailsDto details,
        string? storedName = null,
        string? extraEvidence = null)
    {
        var chips = new List<string>();
        AddAmenityChips(chips, details);

        var profile = PlaceGoogleTypeInterpreter.Resolve(details, storedName, extraEvidence);
        if (profile is not null)
        {
            AddChip(chips, profile.Value.CategoryChip);
            foreach (var extra in profile.Value.ExtraChips)
            {
                AddChip(chips, extra);
            }
        }
        else if (PlaceGoogleTypeInterpreter.IsUsableDisplayName(details.PrimaryTypeDisplayName)
            && !PlaceGoogleTypeInterpreter.LooksLikeFoodDisplayName(details.PrimaryTypeDisplayName))
        {
            AddChip(chips, details.PrimaryTypeDisplayName!);
        }

        return chips;
    }

    internal static string CategoryLabel(IEnumerable<string> features, string zuppetoTypeLabel)
    {
        foreach (var feature in features)
        {
            var value = feature.Trim();
            if (value.Length > 0 && PlaceGoogleTypeCatalog.PetFamilyChips.Contains(value))
            {
                return value;
            }
        }

        foreach (var feature in features)
        {
            var value = feature.Trim();
            if (value.Length > 0 && PlaceGoogleTypeCatalog.CategoryChips.Contains(value))
            {
                return value;
            }
        }

        if (!string.IsNullOrWhiteSpace(zuppetoTypeLabel)
            && !zuppetoTypeLabel.Equals("Servei", StringComparison.OrdinalIgnoreCase))
        {
            return zuppetoTypeLabel.Trim();
        }

        return string.Empty;
    }

    internal static bool NeedsTypeInterpretation(IReadOnlyCollection<string> features)
    {
        if (features.Count == 0)
        {
            return true;
        }

        return features.All(feature => feature.Trim().Equals("Servei", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// True when chips must be rebuilt: empty/Servei, or a pet-shop name still tagged as bakery/etc.
    /// </summary>
    internal static bool NeedsChipRefresh(
        IReadOnlyCollection<string> features,
        string? placeName,
        string? description = null)
    {
        if (NeedsTypeInterpretation(features))
        {
            return true;
        }

        var evidence = $"{placeName} {description}";
        if (!PlaceGoogleTypeInterpreter.LooksLikeVeterinary(evidence)
            && !PlaceGoogleTypeInterpreter.LooksLikePetShop(evidence))
        {
            return false;
        }

        return !features.Any(feature => PlaceGoogleTypeCatalog.PetFamilyChips.Contains(feature.Trim()));
    }

    internal static IReadOnlyCollection<string> MergeConfirmedChips(
        IReadOnlyCollection<string> existing,
        IEnumerable<string>? confirmed)
    {
        var chips = existing.ToList();
        foreach (var chip in confirmed ?? [])
        {
            AddChip(chips, chip);
        }

        return chips;
    }

    private static void AddAmenityChips(List<string> chips, PlaceExternalDetailsDto details)
    {
        if (details.AllowsDogs == true)
        {
            AddChip(chips, "Gossos permesos");
        }
        else if (details.AllowsDogs == false)
        {
            AddChip(chips, "No es permeten gossos");
        }

        if (details.OutdoorSeating == true)
        {
            AddChip(chips, "Terrassa");
        }

        if (details.Reservable == true)
        {
            AddChip(chips, "Reserva");
        }

        if (details.Takeout == true)
        {
            AddChip(chips, "Per emportar");
        }

        if (details.Restroom == true)
        {
            AddChip(chips, "Lavabo");
        }

        if (details.GoodForChildren == true)
        {
            AddChip(chips, "Apta per nens");
        }
    }

    private static void AddChip(List<string> chips, string? label)
    {
        var value = label?.Trim() ?? string.Empty;
        if (value.Length == 0 || chips.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        chips.Add(value);
    }
}
