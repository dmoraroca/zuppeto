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
    public string Key => "gv-isys";
    public (IReadOnlyCollection<CanonicalTerritorialUnit> Units, IReadOnlyCollection<TerritorialImportIssue> Issues) Canonicalize(IReadOnlyCollection<TerritorialMappedRow> rows) => inner.Canonicalize(rows);
}
