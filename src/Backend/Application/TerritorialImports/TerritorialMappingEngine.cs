using System.Globalization;

namespace Zuppeto.Application.TerritorialImports;

public sealed class TerritorialMappingEngine
{
    public IReadOnlyCollection<TerritorialMappedRow> Map(TerritorialSourceWorkbook workbook, TerritorialMappingDefinition definition)
    {
        var result = new List<TerritorialMappedRow>();
        foreach (var sheetMapping in definition.Sheets)
        {
            var sheet = workbook.Sheets.FirstOrDefault(item => string.Equals(item.Name, sheetMapping.Sheet, StringComparison.Ordinal));
            if (sheet is null) throw new InvalidOperationException($"No existeix el full requerit '{sheetMapping.Sheet}'.");

            var headerRow = sheet.Rows.FirstOrDefault(item => item.Number == sheetMapping.HeaderRow)
                ?? throw new InvalidOperationException($"No existeix la capçalera {sheetMapping.HeaderRow} al full '{sheetMapping.Sheet}'.");
            var headerByColumn = headerRow.Values
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .ToDictionary(item => item.Key, item => item.Value!.Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (var physicalRow in sheet.Rows.Where(item => item.Number > sheetMapping.HeaderRow))
            {
                var row = new TerritorialSourceRow(physicalRow.Number, physicalRow.Values
                    .Where(item => headerByColumn.ContainsKey(item.Key))
                    .ToDictionary(item => headerByColumn[item.Key], item => item.Value, StringComparer.OrdinalIgnoreCase));
                foreach (var projection in sheetMapping.Units.Where(item => Matches(row, item.Conditions)))
                {
                    var key = Evaluate(row, projection.CanonicalUnitKey);
                    var name = Evaluate(row, projection.Name);
                    if (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(name)) continue;

                    var sourceColumns = ReferencedColumns(projection);
                    var sourceValues = row.Values
                        .Where(item => sourceColumns.Contains(item.Key))
                        .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
                    var longitude = Decimal(row, projection.Longitude);
                    var latitude = Decimal(row, projection.Latitude);
                    if (projection.ZeroZeroIsSentinel && latitude == 0 && longitude == 0) latitude = longitude = null;

                    result.Add(new TerritorialMappedRow(
                        sheet.Name,
                        row.Number,
                        sourceValues,
                        new CanonicalTerritorialUnit(
                            key,
                            EmptyToNull(Evaluate(row, projection.ParentCanonicalUnitKey)),
                            projection.TerritorialUnitTypeCode.Trim(),
                            [new CanonicalTerritorialName(name, projection.Locale, projection.NameKind)],
                            projection.Codes.Select(code => new CanonicalTerritorialCode(code.Scheme.Trim(), Evaluate(row, code.Value), code.IsPrimary)).ToArray(),
                            latitude,
                            longitude)));
                }
            }
        }

        return result;
    }

    private static decimal? Decimal(TerritorialSourceRow row, MappingValueDefinition? expression)
    {
        var value = Evaluate(row, expression);
        if (string.IsNullOrWhiteSpace(value)) return null;
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : decimal.MinValue;
    }

    private static bool Matches(TerritorialSourceRow row, IReadOnlyCollection<MappingConditionDefinition>? conditions) =>
        conditions is null || conditions.All(condition =>
        {
            row.Values.TryGetValue(condition.Column, out var raw);
            var value = raw?.Trim() ?? string.Empty;
            return condition.Operation switch
            {
                MappingConditionOperation.Equals => string.Equals(value, condition.Value, StringComparison.OrdinalIgnoreCase),
                MappingConditionOperation.NotEquals => !string.Equals(value, condition.Value, StringComparison.OrdinalIgnoreCase),
                MappingConditionOperation.In => condition.Values?.Contains(value, StringComparer.OrdinalIgnoreCase) == true,
                MappingConditionOperation.NotIn => condition.Values?.Contains(value, StringComparer.OrdinalIgnoreCase) != true,
                MappingConditionOperation.NotEmpty => value.Length > 0,
                _ => false
            };
        });

    private static string Evaluate(TerritorialSourceRow row, MappingValueDefinition? expression)
    {
        if (expression is null) return string.Empty;
        string value = expression.Operation switch
        {
            MappingValueOperation.Column => expression.Column is not null && row.Values.TryGetValue(expression.Column, out var raw) ? raw ?? string.Empty : string.Empty,
            MappingValueOperation.Constant => expression.Constant ?? string.Empty,
            MappingValueOperation.Concat => string.Join(expression.Separator, expression.Parts?.Select(part => Evaluate(row, part)) ?? []),
            MappingValueOperation.Coalesce => expression.Parts?.Select(part => Evaluate(row, part)).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? string.Empty,
            _ => string.Empty
        };
        value = value.Trim();
        return expression.NullValues?.Contains(value, StringComparer.OrdinalIgnoreCase) == true ? string.Empty : value;
    }

    private static HashSet<string> ReferencedColumns(TerritorialUnitProjectionDefinition projection)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Add(projection.CanonicalUnitKey, result); Add(projection.ParentCanonicalUnitKey, result); Add(projection.Name, result);
        Add(projection.Longitude, result); Add(projection.Latitude, result);
        foreach (var code in projection.Codes) Add(code.Value, result);
        foreach (var condition in projection.Conditions ?? []) result.Add(condition.Column);
        return result;
    }

    private static void Add(MappingValueDefinition? expression, HashSet<string> result)
    {
        if (expression?.Column is not null) result.Add(expression.Column);
        foreach (var part in expression?.Parts ?? []) Add(part, result);
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public sealed class DefaultTerritorialCanonicalizer : ITerritorialCanonicalizer
{
    public string Key => "default";

    public (IReadOnlyCollection<CanonicalTerritorialUnit> Units, IReadOnlyCollection<TerritorialImportIssue> Issues) Canonicalize(
        IReadOnlyCollection<TerritorialMappedRow> rows)
    {
        var units = new List<CanonicalTerritorialUnit>();
        var issues = new List<TerritorialImportIssue>();
        foreach (var group in rows.GroupBy(row => row.Candidate.CanonicalUnitKey, StringComparer.Ordinal))
        {
            var candidates = group.Select(item => item.Candidate).ToArray();
            if (candidates.Select(item => item.TerritorialUnitTypeCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1 ||
                candidates.Select(item => item.ParentCanonicalUnitKey).Distinct(StringComparer.Ordinal).Count() != 1)
            {
                issues.Add(new TerritorialImportIssue("CANONICAL_AMBIGUOUS", TerritorialIssueSeverity.Error,
                    "La mateixa clau canònica produeix tipus o pares incompatibles.", CanonicalUnitKey: group.Key));
                continue;
            }

            var latitudes = candidates.Where(item => item.Latitude is not null).Select(item => item.Latitude).Distinct().ToArray();
            var longitudes = candidates.Where(item => item.Longitude is not null).Select(item => item.Longitude).Distinct().ToArray();
            if (latitudes.Length > 1 || longitudes.Length > 1)
            {
                issues.Add(new TerritorialImportIssue("COORDINATES_CONFLICT", TerritorialIssueSeverity.Error,
                    "La consolidació conté coordenades incompatibles.", CanonicalUnitKey: group.Key));
                continue;
            }

            units.Add(new CanonicalTerritorialUnit(
                group.Key,
                candidates[0].ParentCanonicalUnitKey,
                candidates[0].TerritorialUnitTypeCode,
                candidates.SelectMany(item => item.Names).Distinct().ToArray(),
                candidates.SelectMany(item => item.Codes).Where(item => !string.IsNullOrWhiteSpace(item.Value)).Distinct().ToArray(),
                latitudes.SingleOrDefault(),
                longitudes.SingleOrDefault()));
        }
        return (units, issues);
    }
}

public sealed class GvIsysCanonicalizer(DefaultTerritorialCanonicalizer inner) : ITerritorialCanonicalizer
{
    private static readonly HashSet<string> RegionsWithDistrictGrouping = ["03", "05", "06", "07", "09", "14"];
    private static readonly IReadOnlyDictionary<string, string> BadenWuerttembergRegions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["08111"] = "08101", ["08115"] = "08101", ["08116"] = "08101", ["08117"] = "08101", ["08118"] = "08101", ["08119"] = "08101",
        ["08121"] = "08102", ["08125"] = "08102", ["08126"] = "08102", ["08127"] = "08102", ["08128"] = "08102",
        ["08135"] = "08103", ["08136"] = "08103",
        ["08211"] = "08201", ["08212"] = "08201", ["08215"] = "08201", ["08216"] = "08201",
        ["08221"] = "08202", ["08222"] = "08202", ["08225"] = "08202", ["08226"] = "08202",
        ["08231"] = "08203", ["08235"] = "08203", ["08236"] = "08203", ["08237"] = "08203",
        ["08311"] = "08301", ["08315"] = "08301", ["08316"] = "08301", ["08317"] = "08301",
        ["08325"] = "08302", ["08326"] = "08302", ["08327"] = "08302",
        ["08335"] = "08303", ["08336"] = "08303", ["08337"] = "08303",
        ["08415"] = "08401", ["08416"] = "08401", ["08417"] = "08401",
        ["08421"] = "08402", ["08425"] = "08402", ["08426"] = "08402",
        ["08435"] = "08403", ["08436"] = "08403", ["08437"] = "08403"
    };

    public string Key => "gv-isys";
    public (IReadOnlyCollection<CanonicalTerritorialUnit> Units, IReadOnlyCollection<TerritorialImportIssue> Issues) Canonicalize(IReadOnlyCollection<TerritorialMappedRow> rows)
    {
        var hierarchyIssues = new List<TerritorialImportIssue>();
        var hierarchyRows = rows.Select(row => ReparentDistrict(row, hierarchyIssues)).ToArray();
        var result = inner.Canonicalize(hierarchyRows);
        var preferredNames = hierarchyRows
            .GroupBy(row => row.Candidate.CanonicalUnitKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Candidate.Names.First().Name, StringComparer.Ordinal);
        var units = result.Units.Select(unit =>
        {
            var preferred = preferredNames[unit.CanonicalUnitKey];
            var names = unit.Names
                .GroupBy(name => (name.Name, name.Locale), new NameLocaleComparer())
                .Select(group => group.First())
                .Select(name => name with
                {
                    Kind = string.Equals(name.Name, preferred, StringComparison.Ordinal) ? "Official" : "Alternative",
                    IsPrimary = string.Equals(name.Name, preferred, StringComparison.Ordinal)
                })
                .ToArray();
            return unit with { Names = names };
        }).ToArray();
        return (units, result.Issues.Concat(hierarchyIssues).ToArray());
    }

    private static TerritorialMappedRow ReparentDistrict(TerritorialMappedRow row, ICollection<TerritorialImportIssue> issues)
    {
        var unit = row.Candidate;
        if (unit.TerritorialUnitTypeCode is not ("KREIS" or "KREISFREIE_STADT")) return row;
        var districtCode = unit.Codes.FirstOrDefault(code => code.Scheme == "de:destatis:kreis")?.Value
            ?? unit.CanonicalUnitKey.Split(':').LastOrDefault();
        if (districtCode is null || districtCode.Length != 5) return row;

        string? parent = null;
        if (districtCode.StartsWith("08", StringComparison.Ordinal))
        {
            if (!BadenWuerttembergRegions.TryGetValue(districtCode, out var regionCode))
            {
                issues.Add(new TerritorialImportIssue("GVISYS_BW_REGION_MISSING", TerritorialIssueSeverity.Error,
                    "El Kreis de Baden-Württemberg no consta a la correspondència oficial Kreis→Region.",
                    row.Sheet, row.RowNumber, "KREIS", districtCode, unit.CanonicalUnitKey));
                return row;
            }
            parent = $"de:region:{regionCode}";
        }
        else if (RegionsWithDistrictGrouping.Contains(districtCode[..2]))
            parent = $"de:rb:{districtCode[..3]}";

        return parent is null ? row : row with { Candidate = unit with { ParentCanonicalUnitKey = parent } };
    }

    private sealed class NameLocaleComparer : IEqualityComparer<(string Name, string? Locale)>
    {
        public bool Equals((string Name, string? Locale) x, (string Name, string? Locale) y) =>
            string.Equals(x.Name, y.Name, StringComparison.Ordinal) &&
            string.Equals(x.Locale, y.Locale, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Name, string? Locale) value) =>
            HashCode.Combine(value.Name, value.Locale?.ToUpperInvariant());
    }
}
