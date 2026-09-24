using System.Text;
using System.Text.Json;
using Zuppeto.Application.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialAdminServiceTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    [Fact]
    public async Task Inspect_returns_sheet_metadata_checksum_and_only_compatible_mappings()
    {
        var repository = new MemoryRepository();
        var workbook = new TerritorialSourceWorkbook([
            new TerritorialSourceSheet("Àmbits", [
                new TerritorialSourceRow(1, new Dictionary<string, string?> { ["A"] = "CODI", ["B"] = "NOM" }),
                new TerritorialSourceRow(2, new Dictionary<string, string?> { ["A"] = "001", ["B"] = "Sant Julià" })
            ])
        ], new string('b', 64));
        var service = Create(repository, workbook);
        var bytes = Encoding.UTF8.GetBytes("synthetic-xlsx");

        var result = await service.InspectAsync(AdminId, repository.SourceId, "../territori.xlsx", bytes.Length, new MemoryStream(bytes));

        Assert.Equal("territori.xlsx", result.ArtifactName);
        Assert.Equal(new string('b', 64), result.SchemaFingerprint);
        Assert.Equal(2, Assert.Single(result.Sheets).Columns.Count);
        Assert.Equal(64, result.FileChecksum.Length);
        Assert.Single(result.CompatibleMappings);
    }

    [Theory]
    [InlineData("territori.csv", 10)]
    [InlineData("territori.xlsx", 0)]
    [InlineData("territori.xlsx", TerritorialAdminService.MaximumArtifactSize + 1)]
    public async Task Inspect_rejects_invalid_artifacts(string name, long size)
    {
        var service = Create(new MemoryRepository(), EmptyWorkbook());
        await Assert.ThrowsAsync<InvalidDataException>(() => service.InspectAsync(AdminId, Guid.NewGuid(), name, size, new MemoryStream(new byte[Math.Min(size, 10)])));
    }

    [Fact]
    public async Task Every_admin_query_rejects_a_non_admin_actor()
    {
        var repository = new MemoryRepository();
        var service = Create(repository, EmptyWorkbook(), new RejectingAuthorizer());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetContextAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListMappingsAsync(Guid.NewGuid(), null, null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListImportsAsync(Guid.NewGuid(), new(null, null, null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListCatalogAsync(Guid.NewGuid(), new(null, null, null, null, null, null, null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListSourcePreviewAsync(Guid.NewGuid(), Guid.NewGuid(), new(null, null)));
    }

    [Fact]
    public async Task Create_mapping_validates_and_versions_an_immutable_definition_with_unicode()
    {
        var repository = new MemoryRepository();
        var service = Create(repository, EmptyWorkbook());
        using var json = JsonDocument.Parse("""
        {"sheets":[{"sheet":"Municipis","headerRow":1,"units":[{"territorialUnitTypeCode":"MUNICIPALITY","canonicalKey":{"operation":"Column","column":"CODI"},"name":{"operation":"Column","column":"NOM"},"locale":"ca-ES","nameKind":"Oficial Àmbits","codes":[{"scheme":"official:code","value":{"operation":"Column","column":"CODI"},"isPrimary":true}]}]}]}
        """);

        var result = await service.CreateMappingAsync(AdminId,
            new CreateTerritorialMappingTemplateRequest(repository.SourceId, new string('A', 64), json.RootElement.Clone()));

        Assert.Equal(2, result.Version);
        Assert.Equal(new string('a', 64), result.SchemaFingerprint);
        var saved = JsonSerializer.Deserialize<TerritorialMappingDefinition>(result.Definition.GetRawText(), TerritorialImportJson.Options);
        Assert.Equal("Oficial Àmbits", saved!.Sheets.Single().Units.Single().NameKind);
        Assert.Equal(64, result.DefinitionChecksum.Length);
    }

    [Fact]
    public async Task Pagination_is_bounded_before_reaching_persistence()
    {
        var repository = new MemoryRepository();
        var service = Create(repository, EmptyWorkbook());
        var result = await service.ListIssuesAsync(AdminId, Guid.NewGuid(), new(null, null, null, null, -4, 900));
        Assert.Equal(1, result.Page);
        Assert.Equal(200, result.PageSize);
    }

    private static TerritorialAdminService Create(MemoryRepository repository, TerritorialSourceWorkbook workbook, ITerritorialImportAuthorizer? authorizer = null) =>
        new(authorizer ?? new AllowingAuthorizer(), new FixedReader(workbook), repository, null!, [new DefaultTerritorialCanonicalizer()]);

    private static TerritorialSourceWorkbook EmptyWorkbook() => new([], new string('c', 64));

    private sealed class AllowingAuthorizer : ITerritorialImportAuthorizer
    {
        public Task EnsureAdminAsync(Guid actorUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RejectingAuthorizer : ITerritorialImportAuthorizer
    {
        public Task EnsureAdminAsync(Guid actorUserId, CancellationToken cancellationToken = default) => throw new UnauthorizedAccessException();
    }

    private sealed class FixedReader(TerritorialSourceWorkbook workbook) : ITerritorialWorkbookReader
    {
        public Task<TerritorialSourceWorkbook> ReadAsync(Stream source, CancellationToken cancellationToken = default) => Task.FromResult(workbook);
    }

    private sealed class MemoryRepository : ITerritorialAdminRepository
    {
        public Guid SourceId { get; } = Guid.NewGuid();
        private readonly List<TerritorialMappingTemplateDetailDto> mappings = [];

        public MemoryRepository()
        {
            using var definition = JsonDocument.Parse("{}");
            mappings.Add(new(Guid.NewGuid(), SourceId, 1, new string('b', 64), new string('d', 64), true, DateTimeOffset.UtcNow, definition.RootElement.Clone()));
        }

        public Task<TerritorialAdminContextDto> GetContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TerritorialAdminContextDto([], [], []));

        public Task<IReadOnlyCollection<TerritorialMappingTemplateSummaryDto>> ListMappingsAsync(Guid? sourceId, string? fingerprint, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<TerritorialMappingTemplateSummaryDto> result = mappings
                .Where(x => sourceId is null || x.DatasetSourceId == sourceId)
                .Where(x => fingerprint is null || x.SchemaFingerprint == fingerprint)
                .Select(x => new TerritorialMappingTemplateSummaryDto(x.Id, x.DatasetSourceId, x.Version, x.SchemaFingerprint, x.DefinitionChecksum, x.IsActive, x.CreatedAtUtc)).ToArray();
            return Task.FromResult(result);
        }

        public Task<TerritorialMappingTemplateDetailDto?> GetMappingAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(mappings.SingleOrDefault(x => x.Id == id));

        public Task<TerritorialMappingTemplateDetailDto> CreateMappingVersionAsync(Guid sourceId, string fingerprint, string definitionJson, string checksum, CancellationToken cancellationToken = default)
        {
            using var definition = JsonDocument.Parse(definitionJson);
            var item = new TerritorialMappingTemplateDetailDto(Guid.NewGuid(), sourceId, mappings.Max(x => x.Version) + 1, fingerprint, checksum, true, DateTimeOffset.UtcNow, definition.RootElement.Clone());
            mappings.Add(item);
            return Task.FromResult(item);
        }

        public Task<PageResult<TerritorialImportListItemDto>> ListImportsAsync(TerritorialImportQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialImportListItemDto>([], query.Page, query.PageSize, 0));
        public Task<TerritorialImportDetailDto?> GetImportAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<TerritorialImportDetailDto?>(null);
        public Task<PageResult<TerritorialImportIssueDto>> ListIssuesAsync(Guid importId, TerritorialIssueQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialImportIssueDto>([], query.Page, query.PageSize, 0));
        public Task<PageResult<TerritorialChangeItemDto>> ListChangesAsync(Guid importId, TerritorialChangeQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialChangeItemDto>([], query.Page, query.PageSize, 0));
        public Task<TerritorialChangeHierarchyDto?> GetChangeHierarchyAsync(Guid importId, Guid changeId, TerritorialChangeHierarchyQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<TerritorialChangeHierarchyDto?>(null);
        public Task<PageResult<TerritorialSourcePreviewRowDto>> ListSourcePreviewAsync(Guid importId, TerritorialPreviewQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialSourcePreviewRowDto>([], query.Page, query.PageSize, 0));
        public Task<PageResult<TerritorialCanonicalPreviewRowDto>> ListCanonicalPreviewAsync(Guid importId, TerritorialPreviewQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialCanonicalPreviewRowDto>([], query.Page, query.PageSize, 0));
        public Task<PageResult<TerritorialCatalogUnitDto>> ListCatalogAsync(TerritorialCatalogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialCatalogUnitDto>([], query.Page, query.PageSize, 0));
        public Task<TerritorialCatalogDetailDto?> GetCatalogUnitAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<TerritorialCatalogDetailDto?>(null);
        public Task<TerritorialCatalogHierarchyDto?> GetCatalogHierarchyAsync(Guid id, TerritorialCatalogHierarchyQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<TerritorialCatalogHierarchyDto?>(null);
        public Task<PageResult<TerritorialCatalogUnitDto>> ListCatalogDescendantsAsync(Guid id, TerritorialCatalogDescendantQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialCatalogUnitDto>([], query.Page, query.PageSize, 0));
        public Task<TerritorialCatalogDetailDto> MaintainAsync(Guid id, Guid actorUserId, TerritorialMaintenanceRequest request, CancellationToken cancellationToken = default) =>
            throw new KeyNotFoundException();
    }
}
