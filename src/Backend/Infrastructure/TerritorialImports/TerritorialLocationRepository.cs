using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;

namespace Zuppeto.Infrastructure.TerritorialImports;

internal sealed class TerritorialLocationRepository(ZuppetoDbContext db) : ITerritorialLocationRepository
{
    public async Task<IReadOnlyCollection<TerritorialAdminCountryDto>> ListCountriesAsync(CancellationToken ct = default) =>
        await db.Countries.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new TerritorialAdminCountryDto(x.Id, x.Code, x.Name, x.Iso2, x.Iso3, x.IsActive)).ToArrayAsync(ct);

    public async Task<PageResult<TerritorialLocalityOptionDto>> SearchLocalitiesAsync(
        Guid countryId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await db.Countries.AsNoTracking().AnyAsync(x => x.Id == countryId && x.IsActive, ct))
            throw new KeyNotFoundException("No s'ha trobat el país.");
        var query = db.TerritorialUnits.AsNoTracking().Where(x => x.CountryId == countryId && x.IsActive &&
            (x.ManualSelectableLocality == true || (x.ManualSelectableLocality == null && x.TerritorialUnitType.IsSelectableLocality)));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.Names.Any(n => n.Name.Contains(value)) || x.Codes.Any(c => c.Value.Contains(value)));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault())
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TerritorialLocalityOptionDto(
                x.Id, x.CountryId,
                x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault() ?? x.Names.Select(n => n.Name).FirstOrDefault() ?? string.Empty,
                (x.Parent == null
                    ? x.TerritorialUnitType.Name
                    : x.Parent.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault() ?? x.TerritorialUnitType.Name) + " · " + x.Country.Name,
                x.Names.Where(n => n.IsPrimary).Select(n => n.Locale).FirstOrDefault())).ToArrayAsync(ct);
        return new PageResult<TerritorialLocalityOptionDto>(rows, page, pageSize, total);
    }

    public async Task<TerritorialLocationSelectionDto> ResolveSelectionAsync(Guid countryId, Guid territorialUnitId, CancellationToken ct = default)
    {
        var country = await db.Countries.AsNoTracking().Where(x => x.Id == countryId && x.IsActive)
            .Select(x => new { x.Id, x.Name }).SingleOrDefaultAsync(ct);
        if (country is null)
            throw new KeyNotFoundException("No s'ha trobat el país.");
        var unit = await db.TerritorialUnits.AsNoTracking().Where(x => x.Id == territorialUnitId)
            .Select(x => new
            {
                x.CountryId, x.IsActive,
                IsSelectable = x.ManualSelectableLocality ?? x.TerritorialUnitType.IsSelectableLocality,
                Name = x.Names.Where(n => n.IsPrimary).Select(n => n.Name).FirstOrDefault()
                    ?? x.Names.Select(n => n.Name).FirstOrDefault() ?? string.Empty
            }).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("No s'ha trobat la localitat.");
        if (unit.CountryId != countryId) throw new InvalidOperationException("La localitat no pertany al país indicat.");
        if (!unit.IsActive) throw new InvalidOperationException("La localitat està inactiva.");
        if (!unit.IsSelectable) throw new InvalidOperationException("La unitat territorial no és una localitat seleccionable.");
        return new TerritorialLocationSelectionDto(country.Id, country.Name, territorialUnitId, unit.Name);
    }
}
