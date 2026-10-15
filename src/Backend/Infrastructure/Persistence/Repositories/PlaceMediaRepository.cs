using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal sealed class PlaceMediaRepository(ZuppetoDbContext db) : IPlaceMediaRepository
{
    public async Task<IReadOnlyCollection<PlaceMediaAsset>> ListAsync(Guid placeId, CancellationToken cancellationToken = default) =>
        (await db.PlaceMediaAssets.AsNoTracking().Where(asset => asset.PlaceId == placeId)
            .OrderByDescending(asset => asset.IsPrimary).ThenBy(asset => asset.Source).ThenBy(asset => asset.Id)
            .ToArrayAsync(cancellationToken)).Select(ToDomain).ToArray();

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<PlaceMediaAsset>>> ListByPlaceIdsAsync(
        IReadOnlyCollection<Guid> placeIds,
        CancellationToken cancellationToken = default)
    {
        if (placeIds.Count == 0) return new Dictionary<Guid, IReadOnlyCollection<PlaceMediaAsset>>();
        var records = await db.PlaceMediaAssets.AsNoTracking().Where(asset => placeIds.Contains(asset.PlaceId)).ToArrayAsync(cancellationToken);
        return records.GroupBy(asset => asset.PlaceId).ToDictionary(
            group => group.Key,
            group => (IReadOnlyCollection<PlaceMediaAsset>)group.Select(ToDomain).ToArray());
    }

    public async Task<string?> GetLinkedWikidataQidAsync(Guid placeId, CancellationToken cancellationToken = default)
    {
        var explicitQid = await db.PlaceSourceRecords.AsNoTracking()
            .Where(record => record.CanonicalPlaceId == placeId && record.Source.SourceKey == WikidataSource.SourceKey && record.State == "Active")
            .OrderByDescending(record => record.LastSeenAtUtc)
            .Select(record => record.ExternalId)
            .FirstOrDefaultAsync(cancellationToken);
        if (IsQid(explicitQid)) return explicitQid!.ToUpperInvariant();

        var payloads = await db.PlaceSourceRecords.AsNoTracking()
            .Where(record => record.CanonicalPlaceId == placeId && record.State == "Active")
            .OrderByDescending(record => record.LastSeenAtUtc)
            .Select(record => record.NormalizedPayloadJson)
            .ToArrayAsync(cancellationToken);
        foreach (var payload in payloads)
        {
            try
            {
                using var json = JsonDocument.Parse(payload);
                if (json.RootElement.TryGetProperty("wikidata.id", out var value) && IsQid(value.GetString()))
                    return value.GetString()!.ToUpperInvariant();
                if (json.RootElement.TryGetProperty("wikidata", out value) && IsQid(value.GetString()))
                    return value.GetString()!.ToUpperInvariant();
            }
            catch (JsonException) { }
        }
        return null;
    }

    public async Task UpsertAsync(PlaceMediaAsset asset, CancellationToken cancellationToken = default)
    {
        if (!await db.Places.AnyAsync(place => place.Id == asset.PlaceId, cancellationToken)) throw new KeyNotFoundException("No s’ha trobat el Place.");
        var record = await db.PlaceMediaAssets.SingleOrDefaultAsync(
            item => item.PlaceId == asset.PlaceId && item.ContentHash == asset.ContentHash,
            cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var rejectionReason = record?.VerificationStatus == nameof(PlaceMediaVerificationStatus.Rejected)
            ? record.RejectionReason
            : null;
        var wasPrimary = record?.IsPrimary == true;
        if (record is null)
        {
            record = new PlaceMediaAssetRecord { Id = asset.Id, PlaceId = asset.PlaceId, CreatedAtUtc = now };
            db.PlaceMediaAssets.Add(record);
        }
        Apply(asset, record);
        if (rejectionReason is not null)
        {
            record.VerificationStatus = nameof(PlaceMediaVerificationStatus.Rejected);
            record.CanBePrimary = false;
            record.IsPrimary = false;
            record.RejectionReason = rejectionReason;
        }
        else if (wasPrimary)
        {
            record.IsPrimary = true;
        }
        record.UpdatedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPrimaryAsync(Guid placeId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var assets = await db.PlaceMediaAssets.Where(asset => asset.PlaceId == placeId).ToArrayAsync(cancellationToken);
        var selected = assets.SingleOrDefault(asset => asset.Id == assetId) ?? throw new KeyNotFoundException("No s’ha trobat la imatge.");
        if (selected.VerificationStatus != nameof(PlaceMediaVerificationStatus.Verified) || !selected.CanBePrimary)
            throw new InvalidOperationException("Només una imatge verificada i apta pot ser principal.");
        foreach (var asset in assets) asset.IsPrimary = asset.Id == assetId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid placeId, Guid assetId, string reason, CancellationToken cancellationToken = default)
    {
        var asset = await db.PlaceMediaAssets.SingleOrDefaultAsync(item => item.PlaceId == placeId && item.Id == assetId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat la imatge.");
        asset.VerificationStatus = nameof(PlaceMediaVerificationStatus.Rejected);
        asset.CanBePrimary = false;
        asset.IsPrimary = false;
        asset.RejectionReason = reason;
        asset.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsQid(string? value) => value is { Length: >= 2 and <= 20 } &&
        (value[0] is 'Q' or 'q') && value[1..].All(char.IsAsciiDigit);

    private static PlaceMediaAsset ToDomain(PlaceMediaAssetRecord record) => new(
        record.Id, record.PlaceId, Enum.Parse<PlaceMediaSource>(record.Source), new Uri(record.OriginalUrl),
        new Uri(record.ServedUrl), record.Author, record.LicenseIdentifier, record.LicenseVersion,
        string.IsNullOrWhiteSpace(record.LicenseUrl) ? null : new Uri(record.LicenseUrl), record.Attribution,
        record.RetrievedAtUtc, record.PermanentStorageAllowed, record.TransformationAllowed, record.ContentHash,
        Enum.Parse<PlaceMediaVerificationStatus>(record.VerificationStatus), record.CanBePrimary, record.IsPrimary,
        record.WikidataQid, record.WikimediaFileName,
        string.IsNullOrWhiteSpace(record.EvidenceUrl) ? null : new Uri(record.EvidenceUrl), record.RejectionReason);

    private static void Apply(PlaceMediaAsset asset, PlaceMediaAssetRecord record)
    {
        record.Source = asset.Source.ToString(); record.OriginalUrl = asset.OriginalUri.ToString();
        record.ServedUrl = asset.ServedUri.ToString(); record.Author = asset.Author;
        record.LicenseIdentifier = asset.LicenseIdentifier; record.LicenseVersion = asset.LicenseVersion;
        record.LicenseUrl = asset.LicenseUri?.ToString(); record.Attribution = asset.Attribution;
        record.RetrievedAtUtc = asset.RetrievedAtUtc; record.PermanentStorageAllowed = asset.PermanentStorageAllowed;
        record.TransformationAllowed = asset.TransformationAllowed; record.ContentHash = asset.ContentHash;
        record.VerificationStatus = asset.VerificationStatus.ToString(); record.CanBePrimary = asset.CanBePrimary;
        record.IsPrimary = asset.IsPrimary; record.WikidataQid = asset.WikidataQid;
        record.WikimediaFileName = asset.WikimediaFileName; record.EvidenceUrl = asset.EvidenceUri?.ToString();
        record.RejectionReason = asset.RejectionReason;
    }
}
