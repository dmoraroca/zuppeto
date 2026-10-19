using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class PlaceDataAttributionQuery(ZuppetoDbContext dbContext) : IPlaceDataAttributionQuery
{
    public async Task<IReadOnlyCollection<PlaceDataAttributionDto>> ListAsync(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.PlaceSourceRecords
            .AsNoTracking()
            .Where(record => record.CanonicalPlaceId == placeId && record.VerificationStatus == "Verified")
            .Select(record => new
            {
                Organisation = record.Source.OrganisationRecord != null ? record.Source.OrganisationRecord.Name : record.Source.Organisation,
                record.Source.Dataset,
                LicenseIdentifier = record.Source.LicenseRecord != null ? record.Source.LicenseRecord.Code : record.Source.LicenseIdentifier,
                LicenseUrl = record.Source.LicenseRecord != null ? record.Source.LicenseRecord.OfficialUrl : record.Source.LicenseUrl,
                record.Source.Attribution,
                SourceUrl = record.Source.Url
            })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(row => new PlaceDataAttributionDto(row.Organisation, row.Dataset, row.LicenseIdentifier,
                row.LicenseUrl, row.Attribution, row.SourceUrl))
            .Distinct()
            .OrderBy(item => item.Organisation)
            .ThenBy(item => item.Dataset)
            .ToArray();
    }
}
