using System.Text.Json;
using System.Text.Json.Serialization;
using Zuppeto.Domain.Geography;
using Zuppeto.Domain.TerritorialImports;

namespace Zuppeto.Application.TerritorialImports;

public static class TerritorialImportJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class TerritorialImportValidator
{
    public IReadOnlyCollection<TerritorialImportIssue> Validate(
        IReadOnlyCollection<CanonicalTerritorialUnit> units,
        TerritorialImportContext context)
    {
        var issues = new List<TerritorialImportIssue>();
        var byKey = units.ToDictionary(item => item.CanonicalUnitKey, StringComparer.Ordinal);

        foreach (var unit in units)
        {
            void Error(string code, string message, string? field = null, string? value = null) =>
                issues.Add(new TerritorialImportIssue(code, TerritorialIssueSeverity.Error, message, Field: field, ProblemValue: value, CanonicalUnitKey: unit.CanonicalUnitKey));

            if (string.IsNullOrWhiteSpace(unit.CanonicalUnitKey)) Error("KEY_REQUIRED", "La clau canònica és obligatòria.");
            if (!context.TerritorialUnitTypeCodes.Contains(unit.TerritorialUnitTypeCode, StringComparer.OrdinalIgnoreCase))
                Error("TYPE_UNKNOWN", "El tipus territorial no existeix al país.", "territorialUnitTypeCode", unit.TerritorialUnitTypeCode);
            if (unit.ParentCanonicalUnitKey == unit.CanonicalUnitKey) Error("SELF_PARENT", "Una unitat no pot ser pare d'ella mateixa.");
            if (unit.ParentCanonicalUnitKey is not null && !byKey.ContainsKey(unit.ParentCanonicalUnitKey) && !ParentExistsInCatalog(unit, context))
                Error("PARENT_MISSING", "No s'ha trobat el pare canònic.", "parentCanonicalUnitKey", unit.ParentCanonicalUnitKey);
            if (unit.Names.Count == 0 || unit.Names.All(item => string.IsNullOrWhiteSpace(item.Name))) Error("NAME_REQUIRED", "Cal almenys un nom territorial.");

            foreach (var name in unit.Names)
            {
                try { _ = TerritorialLocale.NormalizeOptional(name.Locale); }
                catch (Exception) { Error("LOCALE_INVALID", "El locale territorial no és vàlid.", "locale", name.Locale); }
            }

            if ((unit.Latitude is null) != (unit.Longitude is null)) Error("COORDINATES_INCOMPLETE", "Les coordenades han d'estar completes.");
            if (unit.Latitude == decimal.MinValue || unit.Longitude == decimal.MinValue) Error("COORDINATES_FORMAT", "El format de les coordenades no és vàlid.");
            else if (unit.Latitude is < -90 or > 90 || unit.Longitude is < -180 or > 180) Error("COORDINATES_RANGE", "Les coordenades són fora de rang.");
            if (unit.Latitude == 0 && unit.Longitude == 0) Error("COORDINATES_ZERO_SENTINEL", "La coordenada (0,0) no ha estat interpretada com a sentinella.");
            foreach (var code in unit.Codes.Where(item => string.IsNullOrWhiteSpace(item.Scheme) || string.IsNullOrWhiteSpace(item.Value)))
                Error("CODE_INVALID", "L'esquema i el valor del codi són obligatoris.");
        }

        foreach (var duplicate in units.SelectMany(unit => unit.Codes.Select(code => (unit.CanonicalUnitKey, code.Scheme, code.Value)))
                     .GroupBy(item => $"{item.Scheme}\u001f{item.Value}", StringComparer.Ordinal)
                     .Where(group => group.Select(item => item.CanonicalUnitKey).Distinct().Count() > 1))
            issues.Add(new TerritorialImportIssue("CODE_DUPLICATE", TerritorialIssueSeverity.Error,
                "El mateix codi vigent identifica més d'una unitat.", ProblemValue: duplicate.Key));

        DetectCycles(units, issues);
        return issues;
    }

    private static bool ParentExistsInCatalog(CanonicalTerritorialUnit unit, TerritorialImportContext context) =>
        context.CurrentUnits.Any(existing => existing.Codes.Any(code => unit.ParentCanonicalUnitKey?.EndsWith(code.Value, StringComparison.Ordinal) == true));

    private static void DetectCycles(IReadOnlyCollection<CanonicalTerritorialUnit> units, List<TerritorialImportIssue> issues)
    {
        var parent = units.ToDictionary(item => item.CanonicalUnitKey, item => item.ParentCanonicalUnitKey, StringComparer.Ordinal);
        foreach (var start in parent.Keys)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = start;
            while (parent.TryGetValue(current, out var next) && next is not null)
            {
                if (!visited.Add(current))
                {
                    issues.Add(new TerritorialImportIssue("HIERARCHY_CYCLE", TerritorialIssueSeverity.Error,
                        "La jerarquia canònica conté un cicle.", CanonicalUnitKey: start));
                    break;
                }
                current = next;
            }
        }
    }
}

public sealed class TerritorialDiffEngine
{
    public (TerritorialChangeSet ChangeSet, IReadOnlyCollection<TerritorialImportIssue> Issues) Build(
        Guid importId,
        TerritorialImportContext context,
        IReadOnlyCollection<CanonicalTerritorialUnit> incoming,
        DeactivationGuardDefinition? guard,
        DateTimeOffset now,
        IReadOnlyCollection<string>? managedCodeSchemes = null)
    {
        var result = new TerritorialChangeSet(Guid.NewGuid(), importId, context.CountryId, context.CatalogVersion, now);
        var issues = new List<TerritorialImportIssue>();
        var matched = new HashSet<Guid>();
        var incomingByKey = incoming.ToDictionary(item => item.CanonicalUnitKey, StringComparer.Ordinal);
        var resolved = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var unit in incoming)
        {
            var matches = context.CurrentUnits.Where(existing => existing.Codes.Any(oldCode =>
                unit.Codes.Any(code => code.Scheme == oldCode.Scheme && code.Value == oldCode.Value))).ToArray();
            if (matches.Length > 1)
            {
                issues.Add(new TerritorialImportIssue("IDENTITY_AMBIGUOUS", TerritorialIssueSeverity.Error,
                    "Els codis coincideixen amb més d'una unitat publicada.", CanonicalUnitKey: unit.CanonicalUnitKey));
                continue;
            }

            if (matches.Length == 0)
            {
                result.Add(Change(TerritorialChangeKind.Create, null, unit.CanonicalUnitKey, null, unit, ["unit"]));
                continue;
            }

            var current = matches[0];
            matched.Add(current.Id);
            resolved[unit.CanonicalUnitKey] = current.Id;
            var changed = ChangedFields(current, unit, incomingByKey, resolved, context.DatasetSourceId);
            result.Add(Change(changed.Count == 0 ? TerritorialChangeKind.NoChange : TerritorialChangeKind.Update,
                current.Id, unit.CanonicalUnitKey, current, unit, changed));
        }

        if (string.Equals(context.PublicationMode, "FullSnapshot", StringComparison.OrdinalIgnoreCase))
        {
            var managedSchemes = (managedCodeSchemes ?? incoming.SelectMany(item => item.Codes).Select(item => item.Scheme).ToArray()).ToHashSet(StringComparer.Ordinal);
            var deactivations = context.CurrentUnits.Where(item => item.IsActive && !matched.Contains(item.Id) && item.Codes.Any(code => managedSchemes.Contains(code.Scheme))).ToArray();
            foreach (var unit in deactivations)
                result.Add(Change(TerritorialChangeKind.Deactivate, unit.Id, $"existing:{unit.Id}", unit, null, ["isActive"]));

            if (guard is not null && deactivations.Length > guard.MaximumCount && context.CurrentUnits.Count > 0 &&
                deactivations.Length * 100m / context.CurrentUnits.Count > guard.MaximumPercentage)
                issues.Add(new TerritorialImportIssue("MASS_DEACTIVATION", TerritorialIssueSeverity.Error,
                    $"Es proposen {deactivations.Length} inactivacions ({deactivations.Length * 100m / context.CurrentUnits.Count:0.##}%)."));
        }

        return (result, issues);
    }

    private static TerritorialChange Change(TerritorialChangeKind kind, Guid? id, string key, object? before, object? after, IReadOnlyCollection<string> fields) =>
        new(Guid.NewGuid(), kind, id, key,
            before is null ? null : JsonSerializer.Serialize(before, TerritorialImportJson.Options),
            after is null ? null : JsonSerializer.Serialize(after, TerritorialImportJson.Options), fields);

    private static IReadOnlyCollection<string> ChangedFields(
        TerritorialCatalogUnitSnapshot current,
        CanonicalTerritorialUnit incoming,
        IReadOnlyDictionary<string, CanonicalTerritorialUnit> incomingByKey,
        IReadOnlyDictionary<string, Guid> resolved,
        Guid sourceId)
    {
        var changed = new List<string>();
        if (!string.Equals(current.TerritorialUnitTypeCode, incoming.TerritorialUnitTypeCode, StringComparison.OrdinalIgnoreCase)) changed.Add("territorialUnitType");
        if (current.IsActive != incoming.IsActive) changed.Add("isActive");
        if (current.Latitude != incoming.Latitude || current.Longitude != incoming.Longitude) changed.Add("coordinates");
        var incomingNames = incoming.Names.Select(item => item with { DatasetSourceId = item.DatasetSourceId ?? sourceId });
        var incomingCodes = incoming.Codes.Select(item => item with { DatasetSourceId = item.DatasetSourceId ?? sourceId });
        if (!current.Names.OrderBy(item => item.Name).SequenceEqual(incomingNames.OrderBy(item => item.Name))) changed.Add("names");
        if (!current.Codes.OrderBy(item => item.Scheme).ThenBy(item => item.Value).SequenceEqual(incomingCodes.OrderBy(item => item.Scheme).ThenBy(item => item.Value))) changed.Add("codes");
        if (incoming.ParentCanonicalUnitKey is not null && resolved.TryGetValue(incoming.ParentCanonicalUnitKey, out var parentId) && current.ParentId != parentId) changed.Add("parent");
        return changed;
    }
}
