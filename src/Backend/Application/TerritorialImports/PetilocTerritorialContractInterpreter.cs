using System.Globalization;
using Zuppeto.Domain.TerritorialImports;

namespace Zuppeto.Application.TerritorialImports;

/// <summary>Interprets the versioned, country-neutral PETILOC_TERRITORIAL workbook contract.</summary>
public sealed class PetilocTerritorialContractInterpreter : ITerritorialNormalizedContractInterpreter
{
    public const string ContractId = "PETILOC_TERRITORIAL";
    public const string ContractVersion = "1.0";
    private static readonly HashSet<string> KnownSheets = new(StringComparer.Ordinal)
        { "Manifest", "Fonts", "Tipus", "Unitats", "Noms", "Codis", "Atributs" };

    public bool IsNormalized(TerritorialSourceWorkbook workbook) =>
        workbook.Sheets.Any(sheet => string.Equals(sheet.Name, "Manifest", StringComparison.Ordinal));

    public TerritorialNormalizedContract Interpret(TerritorialSourceWorkbook workbook)
    {
        var issues = new List<TerritorialImportIssue>();
        foreach (var unknown in workbook.Sheets.Where(sheet => !KnownSheets.Contains(sheet.Name)))
            Issue(issues, "CONTRACT_SHEET_UNKNOWN", $"El full '{unknown.Name}' no forma part del contracte 1.0.", unknown.Name);

        var manifestTable = Table(workbook, "Manifest", ["key", "value"], issues, required: true);
        var manifestValues = manifestTable.Rows
            .Where(row => Value(row, "key").Length > 0)
            .GroupBy(row => Value(row, "key"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => Value(group.Last(), "value"), StringComparer.OrdinalIgnoreCase);
        Duplicate(manifestTable.Rows.Select(row => Value(row, "key")), "MANIFEST_KEY_DUPLICATE", "La clau del Manifest està duplicada.", issues);
        var manifest = ParseManifest(manifestValues, issues);

        var sourceTable = Table(workbook, "Fonts",
            ["source_key", "organisation", "dataset", "dataset_version", "territorial_date", "url", "license", "license_url", "attribution"],
            issues, required: true);
        var sources = sourceTable.Rows.Select(row => new TerritorialContractSource(
            Required(row, "source_key", issues), Required(row, "organisation", issues), Required(row, "dataset", issues),
            Optional(row, "dataset_version"), Date(row, "territorial_date", issues, required: false), Optional(row, "url"),
            Optional(row, "license"), Optional(row, "license_url"), Optional(row, "attribution"))).ToArray();
        Duplicate(sources.Select(x => x.SourceKey), "SOURCE_KEY_DUPLICATE", "La clau de font està duplicada.", issues);

        if (manifest.ImportMode == TerritorialImportMode.Full)
            _ = Table(workbook, "Tipus", ["type_code", "name", "display_order", "is_selectable_locality"], issues, required: true);

        var unitTable = Table(workbook, "Unitats",
            ["unit_key", "identity_scheme", "identity_value", "type_code", "parent_unit_key", "primary_name", "primary_locale",
             "is_active", "structure_source_key", "latitude", "longitude", "coordinate_source_key"], issues, required: true);
        var units = unitTable.Rows.Select(row => ParseUnit(row, manifest, issues)).ToArray();
        if (units.Length == 0) Issue(issues, "UNITS_REQUIRED", "El full Unitats ha de contenir almenys una unitat.", "Unitats");
        Duplicate(units.Select(x => x.UnitKey), "UNIT_KEY_DUPLICATE", "La clau d'unitat està duplicada.", issues);
        Duplicate(units.Select(x => $"{x.IdentityScheme}\u001f{x.IdentityValue}"), "IDENTITY_DUPLICATE", "La identitat oficial està duplicada.", issues);
        ValidateHierarchy(units, manifest.ImportMode, issues);

        var names = ParseNames(workbook, manifest.ImportMode, issues);
        var codes = ParseCodes(workbook, manifest.ImportMode, issues);
        ValidateReferences(units, names.Select(x => (x.UnitKey, x.RowNumber, "Noms")), issues);
        ValidateReferences(units, codes.Select(x => (x.UnitKey, x.RowNumber, "Codis")), issues);
        ValidateSources(sources, units, names, codes, manifest, issues);

        var attributes = workbook.Sheets.FirstOrDefault(sheet => sheet.Name == "Atributs");
        if (attributes?.Rows.Skip(1).Any(row => row.Values.Values.Any(value => !string.IsNullOrWhiteSpace(value))) == true)
            Issue(issues, "ATTRIBUTES_RESERVED", "El full Atributs està reservat i no admet dades en el contracte 1.0.", "Atributs");

        return new TerritorialNormalizedContract(manifest, sources, units, names, codes, issues);
    }

    private static TerritorialContractManifest ParseManifest(
        IReadOnlyDictionary<string, string> values, List<TerritorialImportIssue> issues)
    {
        string Get(string key)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            Issue(issues, "MANIFEST_FIELD_REQUIRED", $"Manifest necessita '{key}'.", "Manifest", field: key);
            return string.Empty;
        }

        var id = Get("contract_id");
        var version = Get("contract_version");
        if (id.Length > 0 && id != ContractId) Issue(issues, "CONTRACT_ID_INVALID", "El contracte no és PETILOC_TERRITORIAL.", "Manifest", field: "contract_id", value: id);
        if (version.Length > 0 && version != ContractVersion) Issue(issues, "CONTRACT_VERSION_UNSUPPORTED", $"La versió '{version}' no és compatible; s'admet 1.0.", "Manifest", field: "contract_version", value: version);
        var rawMode = Get("import_mode");
        var mode = rawMode.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ? TerritorialImportMode.Patch : TerritorialImportMode.Full;
        if (!rawMode.Equals("FULL", StringComparison.OrdinalIgnoreCase) && !rawMode.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
            Issue(issues, "IMPORT_MODE_INVALID", "import_mode ha de ser FULL o PATCH.", "Manifest", field: "import_mode", value: rawMode);
        var scope = values.GetValueOrDefault("field_scope", string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if (mode == TerritorialImportMode.Patch && scope.Length == 0)
            Issue(issues, "FIELD_SCOPE_REQUIRED", "PATCH necessita field_scope.", "Manifest", field: "field_scope");
        var supportedScope = new HashSet<string>(["coordinates", "names", "codes", "hierarchy", "status", "type"], StringComparer.Ordinal);
        foreach (var field in scope.Where(field => !supportedScope.Contains(field)))
            Issue(issues, "FIELD_SCOPE_UNKNOWN", $"El camp '{field}' no és un scope compatible.", "Manifest", field: "field_scope", value: field);
        var coverage = values.GetValueOrDefault("coverage", "COUNTRY").Trim().ToUpperInvariant();
        if (coverage != "COUNTRY") Issue(issues, "COVERAGE_UNSUPPORTED", "La versió 1.0 només admet coverage COUNTRY.", "Manifest", field: "coverage", value: coverage);
        var territorialDate = DateOnly.TryParseExact(Get("territorial_date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate : default;
        if (territorialDate == default) Issue(issues, "TERRITORIAL_DATE_INVALID", "territorial_date ha d'usar YYYY-MM-DD.", "Manifest", field: "territorial_date");
        return new(id, version, Get("country_iso2").ToUpperInvariant(), mode, Get("dataset_version"), territorialDate,
            Get("primary_source_key"), scope, coverage);
    }

    private static TerritorialContractUnit ParseUnit(TableRow row, TerritorialContractManifest manifest, List<TerritorialImportIssue> issues)
    {
        var full = manifest.ImportMode == TerritorialImportMode.Full;
        var unitKey = Required(row, "unit_key", issues);
        var identityScheme = Required(row, "identity_scheme", issues);
        var identityValue = Required(row, "identity_value", issues);
        var type = full ? Required(row, "type_code", issues) : Optional(row, "type_code");
        var name = full ? Required(row, "primary_name", issues) : Optional(row, "primary_name");
        var structureSource = full ? Required(row, "structure_source_key", issues) : Optional(row, "structure_source_key");
        var active = Boolean(row, "is_active", issues, full);
        var rawLatitude = Value(row, "latitude");
        var rawLongitude = Value(row, "longitude");
        var operation = CoordinateOperation(rawLatitude, rawLongitude, row, issues);
        decimal? latitude = operation == TerritorialFieldOperation.Set ? Decimal(rawLatitude, row, "latitude", issues) : null;
        decimal? longitude = operation == TerritorialFieldOperation.Set ? Decimal(rawLongitude, row, "longitude", issues) : null;
        if (operation == TerritorialFieldOperation.Set && latitude == 0 && longitude == 0)
            Issue(issues, "COORDINATES_ZERO", "Les coordenades (0,0) no són vàlides per defecte.", row.Sheet, row.Number, "coordinates");
        if (latitude is < -90 or > 90) Issue(issues, "LATITUDE_RANGE", "La latitud és fora de rang.", row.Sheet, row.Number, "latitude", rawLatitude);
        if (longitude is < -180 or > 180) Issue(issues, "LONGITUDE_RANGE", "La longitud és fora de rang.", row.Sheet, row.Number, "longitude", rawLongitude);
        var coordinateSource = Optional(row, "coordinate_source_key");
        if (operation == TerritorialFieldOperation.Set && string.IsNullOrWhiteSpace(coordinateSource))
            Issue(issues, "COORDINATE_SOURCE_REQUIRED", "Les coordenades necessiten coordinate_source_key.", row.Sheet, row.Number, "coordinate_source_key");
        if (manifest.ImportMode == TerritorialImportMode.Patch && !manifest.FieldScope.Contains("coordinates") && operation != TerritorialFieldOperation.Keep)
            Issue(issues, "FIELD_OUTSIDE_SCOPE", "Les coordenades tenen valor però no formen part de field_scope.", row.Sheet, row.Number, "coordinates");
        return new(unitKey, identityScheme, identityValue, type, Optional(row, "parent_unit_key"), name,
            Optional(row, "primary_locale"), active, structureSource, latitude, longitude, coordinateSource, operation, row.Number);
    }

    private static IReadOnlyCollection<TerritorialContractName> ParseNames(TerritorialSourceWorkbook workbook, TerritorialImportMode mode, List<TerritorialImportIssue> issues)
    {
        var table = Table(workbook, "Noms", ["unit_key", "name", "locale", "kind", "is_primary", "source_key", "action"], issues, false);
        return table.Rows.Select(row => new TerritorialContractName(Required(row, "unit_key", issues), Required(row, "name", issues),
            Optional(row, "locale"), Required(row, "kind", issues), Boolean(row, "is_primary", issues, true) ?? false,
            Required(row, "source_key", issues), Operation(row, mode, issues), row.Number)).ToArray();
    }

    private static IReadOnlyCollection<TerritorialContractCode> ParseCodes(TerritorialSourceWorkbook workbook, TerritorialImportMode mode, List<TerritorialImportIssue> issues)
    {
        var table = Table(workbook, "Codis", ["unit_key", "scheme", "value", "is_primary", "valid_from", "valid_to", "source_key", "action"], issues, false);
        return table.Rows.Select(row => new TerritorialContractCode(Required(row, "unit_key", issues), Required(row, "scheme", issues),
            Required(row, "value", issues), Boolean(row, "is_primary", issues, true) ?? false,
            Date(row, "valid_from", issues, false), Date(row, "valid_to", issues, false), Required(row, "source_key", issues),
            Operation(row, mode, issues), row.Number)).ToArray();
    }

    private static void ValidateHierarchy(IReadOnlyCollection<TerritorialContractUnit> units, TerritorialImportMode mode, List<TerritorialImportIssue> issues)
    {
        var keys = units.Select(x => x.UnitKey).ToHashSet(StringComparer.Ordinal);
        if (mode == TerritorialImportMode.Full)
            foreach (var unit in units.Where(x => x.ParentUnitKey is not null && !keys.Contains(x.ParentUnitKey)))
                Issue(issues, "PARENT_MISSING", "parent_unit_key no existeix al workbook.", "Unitats", unit.RowNumber, "parent_unit_key", unit.ParentUnitKey);
        var parent = units.Where(x => x.ParentUnitKey is not null).ToDictionary(x => x.UnitKey, x => x.ParentUnitKey!, StringComparer.Ordinal);
        foreach (var unit in units)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = unit.UnitKey;
            while (parent.TryGetValue(current, out var next))
            {
                if (!visited.Add(current)) { Issue(issues, "HIERARCHY_CYCLE", "La jerarquia conté un cicle.", "Unitats", unit.RowNumber, "parent_unit_key"); break; }
                current = next;
            }
        }
    }

    private static void ValidateReferences(IReadOnlyCollection<TerritorialContractUnit> units,
        IEnumerable<(string UnitKey, int RowNumber, string Sheet)> references, List<TerritorialImportIssue> issues)
    {
        var keys = units.Select(x => x.UnitKey).ToHashSet(StringComparer.Ordinal);
        foreach (var reference in references.Where(x => !keys.Contains(x.UnitKey)))
            Issue(issues, "UNIT_REFERENCE_MISSING", "La unitat referenciada no existeix a Unitats.", reference.Sheet, reference.RowNumber, "unit_key", reference.UnitKey);
    }

    private static void ValidateSources(IReadOnlyCollection<TerritorialContractSource> sources,
        IReadOnlyCollection<TerritorialContractUnit> units, IReadOnlyCollection<TerritorialContractName> names,
        IReadOnlyCollection<TerritorialContractCode> codes, TerritorialContractManifest manifest, List<TerritorialImportIssue> issues)
    {
        var keys = sources.Select(x => x.SourceKey).ToHashSet(StringComparer.Ordinal);
        var used = units.SelectMany(x => new[] { x.StructureSourceKey, x.CoordinateSourceKey }).Concat(names.Select(x => x.SourceKey)).Concat(codes.Select(x => x.SourceKey))
            .Append(manifest.PrimarySourceKey).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!);
        foreach (var source in used.Where(source => !keys.Contains(source)).Distinct(StringComparer.Ordinal))
            Issue(issues, "SOURCE_REFERENCE_MISSING", $"La font '{source}' no està declarada a Fonts.", "Fonts", field: "source_key", value: source);
    }

    private static TableData Table(TerritorialSourceWorkbook workbook, string sheetName, IReadOnlyCollection<string> allowed,
        List<TerritorialImportIssue> issues, bool required)
    {
        var sheet = workbook.Sheets.SingleOrDefault(item => item.Name == sheetName);
        if (sheet is null)
        {
            if (required) Issue(issues, "CONTRACT_SHEET_REQUIRED", $"Falta el full obligatori '{sheetName}'.", sheetName);
            return new([], []);
        }
        var header = sheet.Rows.FirstOrDefault(row => row.Number == 1);
        if (header is null) { Issue(issues, "HEADER_REQUIRED", "Falta la capçalera a la fila 1.", sheetName, 1); return new([], []); }
        var byColumn = header.Values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToDictionary(x => x.Key, x => x.Value!.Trim(), StringComparer.OrdinalIgnoreCase);
        foreach (var column in byColumn.Values.Where(column => !allowed.Contains(column, StringComparer.OrdinalIgnoreCase)))
            Issue(issues, "CONTRACT_COLUMN_UNKNOWN", $"La columna '{column}' no forma part del contracte.", sheetName, 1, column);
        foreach (var column in allowed.Where(column => !byColumn.Values.Contains(column, StringComparer.OrdinalIgnoreCase)))
            Issue(issues, "CONTRACT_COLUMN_REQUIRED", $"Falta la columna '{column}'.", sheetName, 1, column);
        var rows = sheet.Rows.Where(row => row.Number > 1 && row.Values.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
            .Select(row => new TableRow(sheetName, row.Number, row.Values.Where(x => byColumn.ContainsKey(x.Key))
                .ToDictionary(x => byColumn[x.Key], x => x.Value, StringComparer.OrdinalIgnoreCase))).ToArray();
        return new(byColumn.Values.ToArray(), rows);
    }

    private static TerritorialFieldOperation CoordinateOperation(string latitude, string longitude, TableRow row, List<TerritorialImportIssue> issues)
    {
        if (latitude.Length == 0 && longitude.Length == 0) return TerritorialFieldOperation.Keep;
        if (latitude == "@CLEAR" && longitude == "@CLEAR") return TerritorialFieldOperation.Clear;
        if (latitude.Length == 0 || longitude.Length == 0 || latitude == "@CLEAR" || longitude == "@CLEAR")
        {
            Issue(issues, "COORDINATES_INCOMPLETE", "Les coordenades han de ser un parell complet o dos @CLEAR.", row.Sheet, row.Number, "coordinates");
            return TerritorialFieldOperation.Keep;
        }
        return TerritorialFieldOperation.Set;
    }

    private static TerritorialFieldOperation Operation(TableRow row, TerritorialImportMode mode, List<TerritorialImportIssue> issues)
    {
        var value = Value(row, "action").ToUpperInvariant();
        if (value == "") return mode == TerritorialImportMode.Full ? TerritorialFieldOperation.Set : TerritorialFieldOperation.Keep;
        if (value == "KEEP") return TerritorialFieldOperation.Keep;
        if (value is "SET" or "UPSERT") return TerritorialFieldOperation.Set;
        if (value is "CLEAR" or "DELETE") return TerritorialFieldOperation.Clear;
        Issue(issues, "FIELD_OPERATION_INVALID", "action ha de ser KEEP, SET/UPSERT o CLEAR/DELETE.", row.Sheet, row.Number, "action", value);
        return TerritorialFieldOperation.Keep;
    }

    private static decimal? Decimal(string value, TableRow row, string field, List<TerritorialImportIssue> issues)
    {
        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        Issue(issues, "DECIMAL_INVALID", "El valor decimal no és vàlid.", row.Sheet, row.Number, field, value);
        return null;
    }

    private static bool? Boolean(TableRow row, string field, List<TerritorialImportIssue> issues, bool required)
    {
        var value = Value(row, field);
        if (value.Length == 0 && !required) return null;
        if (bool.TryParse(value, out var parsed)) return parsed;
        if (value == "1") return true; if (value == "0") return false;
        Issue(issues, "BOOLEAN_INVALID", $"{field} ha de ser true o false.", row.Sheet, row.Number, field, value);
        return null;
    }

    private static DateOnly? Date(TableRow row, string field, List<TerritorialImportIssue> issues, bool required)
    {
        var value = Value(row, field);
        if (value.Length == 0 && !required) return null;
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed;
        Issue(issues, "DATE_INVALID", $"{field} ha d'usar YYYY-MM-DD.", row.Sheet, row.Number, field, value);
        return null;
    }

    private static string Required(TableRow row, string field, List<TerritorialImportIssue> issues)
    {
        var value = Value(row, field);
        if (value.Length == 0) Issue(issues, "FIELD_REQUIRED", $"El camp '{field}' és obligatori.", row.Sheet, row.Number, field);
        return value;
    }
    private static string? Optional(TableRow row, string field) => Value(row, field) is { Length: > 0 } value ? value : null;
    private static string Value(TableRow row, string field) => row.Values.GetValueOrDefault(field)?.Trim() ?? string.Empty;
    private static void Duplicate(IEnumerable<string> values, string code, string message, List<TerritorialImportIssue> issues)
    {
        foreach (var value in values.Where(x => x.Length > 0).GroupBy(x => x, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key))
            Issue(issues, code, message, problemValue: value);
    }
    private static void Issue(List<TerritorialImportIssue> issues, string code, string message, string? sheet = null,
        int? row = null, string? field = null, string? value = null, string? problemValue = null) =>
        issues.Add(new(code, TerritorialIssueSeverity.Error, message, sheet, row, field, value ?? problemValue));

    private sealed record TableData(IReadOnlyCollection<string> Headers, IReadOnlyCollection<TableRow> Rows);
    private sealed record TableRow(string Sheet, int Number, IReadOnlyDictionary<string, string?> Values);
}
