using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Zuppeto.Application.TerritorialImports;

namespace Zuppeto.Infrastructure.TerritorialImports;

public sealed class XlsxTerritorialReader : ITerritorialWorkbookReader
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public async Task<TerritorialSourceWorkbook> ReadAsync(Stream source, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
        var workbook = Load(archive, "xl/workbook.xml");
        var relationships = Load(archive, "xl/_rels/workbook.xml.rels")
            .Root!.Elements(PackageRelationships + "Relationship")
            .ToDictionary(item => (string)item.Attribute("Id")!, item => (string)item.Attribute("Target")!);
        var sharedStrings = ReadSharedStrings(archive);
        var sheets = new List<TerritorialSourceSheet>();

        foreach (var sheet in workbook.Root!.Element(Spreadsheet + "sheets")!.Elements(Spreadsheet + "sheet"))
        {
            var name = (string)sheet.Attribute("name")!;
            var relationshipId = (string)sheet.Attribute(OfficeRelationships + "id")!;
            var path = NormalizeWorksheetPath(relationships[relationshipId]);
            sheets.Add(new TerritorialSourceSheet(name, ReadRows(archive, path, sharedStrings)));
        }

        return new TerritorialSourceWorkbook(sheets, Fingerprint(sheets));
    }

    private static IReadOnlyCollection<TerritorialSourceRow> ReadRows(ZipArchive archive, string path, IReadOnlyList<string> sharedStrings)
    {
        var document = Load(archive, path);
        var rows = new List<TerritorialSourceRow>();
        foreach (var row in document.Descendants(Spreadsheet + "row"))
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in row.Elements(Spreadsheet + "c"))
            {
                if (cell.Element(Spreadsheet + "f") is not null)
                    throw new InvalidDataException("Els XLSX territorials no poden contenir fórmules executables.");
                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var column = new string(reference.TakeWhile(char.IsLetter).ToArray());
                if (column.Length == 0) continue;
                values[column] = CellValue(cell, sharedStrings);
            }
            rows.Add(new TerritorialSourceRow((int?)row.Attribute("r") ?? rows.Count + 1, values));
        }
        return rows;
    }

    private static string? CellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        if (type == "inlineStr") return string.Concat(cell.Descendants(Spreadsheet + "t").Select(item => item.Value));
        var raw = cell.Element(Spreadsheet + "v")?.Value;
        if (type == "s" && int.TryParse(raw, out var index) && index >= 0 && index < sharedStrings.Count) return sharedStrings[index];
        if (type == "b") return raw == "1" ? "true" : "false";
        return raw;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];
        using var stream = entry.Open();
        var document = XDocument.Load(stream, LoadOptions.None);
        return document.Root!.Elements(Spreadsheet + "si")
            .Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(text => text.Value))).ToArray();
    }

    private static string Fingerprint(IReadOnlyCollection<TerritorialSourceSheet> sheets)
    {
        var value = string.Join("\n", sheets.Select(sheet =>
        {
            var header = sheet.Rows.Take(10).OrderByDescending(row => row.Values.Count(item => !string.IsNullOrWhiteSpace(item.Value))).FirstOrDefault();
            return $"{sheet.Name}:{string.Join('|', header?.Values.OrderBy(item => item.Key).Select(item => item.Value?.Trim()) ?? [])}";
        }));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string NormalizeWorksheetPath(string target)
    {
        var normalized = target.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("xl/", StringComparison.Ordinal)) return normalized;
        while (normalized.StartsWith("../", StringComparison.Ordinal)) normalized = normalized[3..];
        return $"xl/{normalized}";
    }

    private static XDocument Load(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidDataException($"L'XLSX no conté '{path}'.");
        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.None);
    }
}
