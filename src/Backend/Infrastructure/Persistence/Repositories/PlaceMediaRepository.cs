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
        var payloads = await db.PlaceSourceRecords.AsNoTracking()
            .Where(record => record.CanonicalPlaceId == placeId
                && record.Source.SourceKey == OpenStreetMapSource.SourceKey
                && record.State == "Active")
            .OrderByDescending(record => record.LastSeenAtUtc)
            .Select(record => record.NormalizedPayloadJson)
            .ToArrayAsync(cancellationToken);
        foreach (var payload in payloads)
        {
            var qid = ExtractVerifiedExactPlaceQid(payload);
            if (qid is not null) return qid;
        }
        return null;
    }

    public Task<DateTimeOffset?> GetNextDiscoveryAtAsync(
        Guid placeId,
        string source,
        CancellationToken cancellationToken = default) =>
        db.PlaceMediaDiscoveries.AsNoTracking()
            .Where(item => item.PlaceId == placeId && item.Source == source)
            .Select(item => (DateTimeOffset?)item.NextCheckAtUtc)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task RecordDiscoveryAsync(
        Guid placeId,
        string source,
        DateTimeOffset checkedAtUtc,
        DateTimeOffset nextCheckAtUtc,
        int candidateCount,
        string? error,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PlaceMediaDiscoveries.SingleOrDefaultAsync(
            item => item.PlaceId == placeId && item.Source == source, cancellationToken);
        if (record is null)
        {
            record = new PlaceMediaDiscoveryRecord
            {
                Id = Guid.NewGuid(), PlaceId = placeId, Source = source, CreatedAtUtc = checkedAtUtc
            };
            db.PlaceMediaDiscoveries.Add(record);
        }
        record.LastCheckedAtUtc = checkedAtUtc;
        record.NextCheckAtUtc = nextCheckAtUtc;
        record.CandidateCount = candidateCount;
        record.Status = error is null ? "Completed" : "Failed";
        record.Error = error;
        record.UpdatedAtUtc = checkedAtUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static string? ExtractVerifiedExactPlaceQid(string payload)
    {
        try
        {
            using var json = JsonDocument.Parse(payload);
            if (!json.RootElement.TryGetProperty(PlaceWikidataFields.IdentityStatus, out var status)
                || !string.Equals(status.GetString(), PlaceWikidataFields.VerifiedExact, StringComparison.Ordinal)) return null;
            if (!json.RootElement.TryGetProperty(PlaceWikidataFields.PlaceId, out var value) || !IsQid(value.GetString())) return null;
            return value.GetString()!.ToUpperInvariant();
        }
        catch (JsonException) { return null; }
    }

    public async Task UpsertAsync(PlaceMediaAsset asset, CancellationToken cancellationToken = default)
    {
        if (!await db.Places.AnyAsync(place => place.Id == asset.PlaceId, cancellationToken)) throw new KeyNotFoundException("No s’ha trobat el Place.");
        var record = await db.PlaceMediaAssets.SingleOrDefaultAsync(
            item => item.PlaceId == asset.PlaceId && (item.Id == asset.Id || item.ContentHash == asset.ContentHash),
            cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var rejectionReason = record?.VerificationStatus == nameof(PlaceMediaVerificationStatus.Rejected)
            ? record.RejectionReason
            : null;
        var wasVerified = record?.VerificationStatus == nameof(PlaceMediaVerificationStatus.Verified);
        var wasPrimary = record?.IsPrimary == true;
        var identityWasVerified = record?.IdentityVerified == true;
        var moderatedAt = record?.ModeratedAtUtc;
        var moderatorActorId = record?.ModeratorActorId;
        var verifiedServedUrl = wasVerified ? record!.ServedUrl : null;
        var verifiedContentHash = wasVerified ? record!.ContentHash : null;
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
        if (wasVerified)
        {
            record.VerificationStatus = nameof(PlaceMediaVerificationStatus.Verified);
            record.CanBePrimary = true;
            record.IdentityVerified = identityWasVerified;
            record.ModeratedAtUtc = moderatedAt;
            record.ModeratorActorId = moderatorActorId;
            record.ServedUrl = verifiedServedUrl!;
            record.ContentHash = verifiedContentHash!;
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

    public async Task UpdateStoredContentAsync(
        Guid placeId,
        Guid assetId,
        string servedUrl,
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        var asset = await db.PlaceMediaAssets.SingleOrDefaultAsync(
            item => item.PlaceId == placeId && item.Id == assetId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat la imatge.");
        asset.ServedUrl = servedUrl;
        asset.ContentHash = contentHash;
        asset.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AcceptAsync(
        Guid placeId,
        Guid assetId,
        Guid actorId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var asset = await db.PlaceMediaAssets.SingleOrDefaultAsync(
            item => item.PlaceId == placeId && item.Id == assetId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat la imatge.");
        if (asset.VerificationStatus == nameof(PlaceMediaVerificationStatus.Rejected))
            throw new InvalidOperationException("Una imatge rebutjada no es pot aprovar sense crear una revisió nova.");
        if (asset.Source != nameof(PlaceMediaSource.Mapillary) && asset.Source != nameof(PlaceMediaSource.Owner)
            && asset.Source != nameof(PlaceMediaSource.User) && asset.Source != nameof(PlaceMediaSource.OfficialWebsite))
            throw new InvalidOperationException("Aquesta font no utilitza acceptació manual.");
        asset.IdentityVerified = true;
        asset.VerificationStatus = nameof(PlaceMediaVerificationStatus.Verified);
        asset.CanBePrimary = true;
        asset.RejectionReason = null;
        asset.ReuseConditions = $"{asset.ReuseConditions} Evidència de moderació: {reason}".Trim();
        asset.ModeratedAtUtc = DateTimeOffset.UtcNow;
        asset.ModeratorActorId = actorId;
        asset.UpdatedAtUtc = asset.ModeratedAtUtc.Value;
        db.PlaceMediaModerationHistory.Add(new PlaceMediaModerationRecord
        {
            Id = Guid.NewGuid(), MediaAssetId = assetId, ActorUserId = actorId,
            Action = "Accepted", Reason = reason, CreatedAtUtc = asset.UpdatedAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid placeId, Guid assetId, Guid actorId, string reason, CancellationToken cancellationToken = default)
    {
        var asset = await db.PlaceMediaAssets.SingleOrDefaultAsync(item => item.PlaceId == placeId && item.Id == assetId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat la imatge.");
        asset.VerificationStatus = nameof(PlaceMediaVerificationStatus.Rejected);
        asset.CanBePrimary = false;
        asset.IsPrimary = false;
        asset.RejectionReason = reason;
        asset.ModeratedAtUtc = DateTimeOffset.UtcNow;
        asset.ModeratorActorId = actorId;
        asset.UpdatedAtUtc = DateTimeOffset.UtcNow;
        db.PlaceMediaModerationHistory.Add(new PlaceMediaModerationRecord
        {
            Id = Guid.NewGuid(), MediaAssetId = assetId, ActorUserId = actorId,
            Action = "Rejected", Reason = reason, CreatedAtUtc = asset.UpdatedAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlaceMediaUploadQuota> GetUploadQuotaAsync(Guid contributorUserId, DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        var uploadedToday = await db.PlaceMediaAssets.AsNoTracking().CountAsync(
            asset => asset.ContributorUserId == contributorUserId && asset.CreatedAtUtc >= sinceUtc, cancellationToken);
        var pending = await db.PlaceMediaAssets.AsNoTracking().CountAsync(
            asset => asset.ContributorUserId == contributorUserId
                && asset.VerificationStatus == nameof(PlaceMediaVerificationStatus.Pending), cancellationToken);
        return new PlaceMediaUploadQuota(uploadedToday, pending);
    }

    public Task<bool> ContentHashExistsAsync(Guid placeId, string contentHash,
        CancellationToken cancellationToken = default) =>
        db.PlaceMediaAssets.AsNoTracking().AnyAsync(
            asset => asset.PlaceId == placeId && asset.ContentHash == contentHash, cancellationToken);

    public async Task SaveProcessedUploadAsync(PlaceMediaAsset asset,
        IReadOnlyCollection<ProcessedPlaceMediaVariant> variants,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Places.AnyAsync(place => place.Id == asset.PlaceId, cancellationToken))
            throw new KeyNotFoundException("No s’ha trobat el Place.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var record = new PlaceMediaAssetRecord
        {
            Id = asset.Id, PlaceId = asset.PlaceId, CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        Apply(asset, record);
        db.PlaceMediaAssets.Add(record);
        foreach (var variant in variants)
        {
            db.PlaceMediaBlobs.Add(new PlaceMediaBlobRecord
            {
                Id = Guid.NewGuid(), MediaAssetId = asset.Id, Variant = variant.Variant.ToString(),
                MimeType = variant.MimeType, Content = variant.Content, SizeBytes = variant.Content.LongLength,
                Width = variant.Width, Height = variant.Height, Sha256 = variant.Sha256,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PlaceMediaBlobDto?> GetBlobAsync(Guid placeId, Guid assetId, PlaceMediaVariant variant,
        bool includeNonPublic, CancellationToken cancellationToken = default)
    {
        var query = db.PlaceMediaBlobs.AsNoTracking().Where(blob =>
            blob.MediaAssetId == assetId && blob.MediaAsset.PlaceId == placeId && blob.Variant == variant.ToString());
        if (!includeNonPublic)
        {
            query = query.Where(blob =>
                blob.MediaAsset.VerificationStatus == nameof(PlaceMediaVerificationStatus.Verified)
                && blob.MediaAsset.IdentityVerified);
        }
        return await query.Select(blob => new PlaceMediaBlobDto(
            blob.Content, blob.MimeType, blob.Sha256, blob.CreatedAtUtc)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlaceMediaAsset>> ListPendingAsync(int skip, int take,
        CancellationToken cancellationToken = default) =>
        (await db.PlaceMediaAssets.AsNoTracking()
            .Where(asset => asset.VerificationStatus == nameof(PlaceMediaVerificationStatus.Pending))
            .OrderBy(asset => asset.CreatedAtUtc).Skip(skip).Take(take).ToArrayAsync(cancellationToken))
        .Select(ToDomain).ToArray();

    public Task QuarantineAsync(Guid placeId, Guid assetId, Guid actorId, string reason,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(placeId, assetId, actorId, PlaceMediaVerificationStatus.Quarantined, "Quarantined", reason, cancellationToken);

    public Task WithdrawAsync(Guid placeId, Guid assetId, Guid actorId, string reason,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(placeId, assetId, actorId, PlaceMediaVerificationStatus.Withdrawn, "Withdrawn", reason, cancellationToken);

    public async Task ReportAsync(Guid placeId, Guid assetId, Guid reporterUserId, string reason,
        CancellationToken cancellationToken = default)
    {
        if (!await db.PlaceMediaAssets.AsNoTracking().AnyAsync(
            asset => asset.Id == assetId && asset.PlaceId == placeId
                && asset.VerificationStatus == nameof(PlaceMediaVerificationStatus.Verified), cancellationToken))
            throw new KeyNotFoundException("No s’ha trobat la fotografia pública.");
        db.PlaceMediaReports.Add(new PlaceMediaReportRecord
        {
            Id = Guid.NewGuid(), MediaAssetId = assetId, ReporterUserId = reporterUserId,
            Reason = reason, Status = "Pending", CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlaceMediaModerationDto>> ListModerationHistoryAsync(Guid placeId,
        CancellationToken cancellationToken = default) =>
        await db.PlaceMediaModerationHistory.AsNoTracking()
            .Where(item => db.PlaceMediaAssets.Any(asset => asset.Id == item.MediaAssetId && asset.PlaceId == placeId))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new PlaceMediaModerationDto(item.MediaAssetId, item.ActorUserId, item.Action,
                item.Reason, item.CreatedAtUtc)).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PlaceMediaReportDto>> ListPendingReportsAsync(int skip, int take,
        CancellationToken cancellationToken = default) =>
        await db.PlaceMediaReports.AsNoTracking().Where(item => item.Status == "Pending")
            .OrderBy(item => item.CreatedAtUtc).Skip(skip).Take(take)
            .Select(item => new PlaceMediaReportDto(item.Id, item.MediaAssetId,
                db.PlaceMediaAssets.Where(asset => asset.Id == item.MediaAssetId).Select(asset => asset.PlaceId).Single(),
                item.ReporterUserId, item.Reason, item.Status, item.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task ResolveReportAsync(Guid reportId, Guid actorId, string decision, string reason,
        CancellationToken cancellationToken = default)
    {
        var report = await db.PlaceMediaReports.SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat la denúncia.");
        if (report.Status != "Pending") throw new InvalidOperationException("La denúncia ja està resolta.");
        report.Status = decision;
        report.ResolvedAtUtc = DateTimeOffset.UtcNow;
        report.ResolvedByUserId = actorId;
        db.PlaceMediaModerationHistory.Add(new PlaceMediaModerationRecord
        {
            Id = Guid.NewGuid(), MediaAssetId = report.MediaAssetId, ActorUserId = actorId,
            Action = $"Report{decision}", Reason = reason, CreatedAtUtc = report.ResolvedAtUtc.Value
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ChangeStatusAsync(Guid placeId, Guid assetId, Guid actorId,
        PlaceMediaVerificationStatus status, string action, string reason, CancellationToken cancellationToken)
    {
        var asset = await db.PlaceMediaAssets.SingleOrDefaultAsync(
            item => item.PlaceId == placeId && item.Id == assetId, cancellationToken)
            ?? throw new KeyNotFoundException("No s’ha trobat la imatge.");
        asset.VerificationStatus = status.ToString();
        asset.CanBePrimary = false;
        asset.IsPrimary = false;
        asset.RejectionReason = reason;
        asset.ModeratedAtUtc = DateTimeOffset.UtcNow;
        asset.ModeratorActorId = actorId;
        asset.UpdatedAtUtc = asset.ModeratedAtUtc.Value;
        db.PlaceMediaModerationHistory.Add(new PlaceMediaModerationRecord
        {
            Id = Guid.NewGuid(), MediaAssetId = assetId, ActorUserId = actorId,
            Action = action, Reason = reason, CreatedAtUtc = asset.UpdatedAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsQid(string? value) => value is { Length: >= 2 and <= 20 } &&
        (value[0] is 'Q' or 'q') && value[1..].All(char.IsAsciiDigit);

    private static PlaceMediaAsset ToDomain(PlaceMediaAssetRecord record) => new(
        record.Id, record.PlaceId, Enum.Parse<PlaceMediaSource>(record.Source), record.SourceOrganisation,
        record.ExternalId, new Uri(record.OriginalUrl),
        new Uri(record.ServedUrl, UriKind.RelativeOrAbsolute), record.Author, record.LicenseIdentifier, record.LicenseVersion,
        string.IsNullOrWhiteSpace(record.LicenseUrl) ? null : new Uri(record.LicenseUrl), record.Attribution,
        record.RetrievedAtUtc, record.PermanentStorageAllowed, record.TransformationAllowed, record.ContentHash,
        Enum.Parse<PlaceMediaVerificationStatus>(record.VerificationStatus), record.CanBePrimary, record.IsPrimary,
        record.IdentityVerified, record.ReuseConditions, record.WikidataQid, record.WikimediaFileName,
        string.IsNullOrWhiteSpace(record.EvidenceUrl) ? null : new Uri(record.EvidenceUrl), record.RejectionReason,
        record.CapturedAtUtc, record.Latitude, record.Longitude, record.HeadingDegrees, record.DistanceMeters,
        record.LastCheckedAtUtc, record.ModeratedAtUtc, record.ModeratorActorId,
        record.ContributorUserId, record.RightsDeclaration);

    private static void Apply(PlaceMediaAsset asset, PlaceMediaAssetRecord record)
    {
        record.Source = asset.Source.ToString(); record.SourceOrganisation = asset.SourceOrganisation;
        record.ExternalId = asset.ExternalId; record.OriginalUrl = asset.OriginalUri.ToString();
        record.ServedUrl = asset.ServedUri.ToString(); record.Author = asset.Author;
        record.LicenseIdentifier = asset.LicenseIdentifier; record.LicenseVersion = asset.LicenseVersion;
        record.LicenseUrl = asset.LicenseUri?.ToString(); record.Attribution = asset.Attribution;
        record.RetrievedAtUtc = asset.RetrievedAtUtc; record.PermanentStorageAllowed = asset.PermanentStorageAllowed;
        record.TransformationAllowed = asset.TransformationAllowed; record.ContentHash = asset.ContentHash;
        record.VerificationStatus = asset.VerificationStatus.ToString(); record.CanBePrimary = asset.CanBePrimary;
        record.IsPrimary = asset.IsPrimary; record.IdentityVerified = asset.IdentityVerified;
        record.ReuseConditions = asset.ReuseConditions; record.WikidataQid = asset.WikidataQid;
        record.WikimediaFileName = asset.WikimediaFileName; record.EvidenceUrl = asset.EvidenceUri?.ToString();
        record.RejectionReason = asset.RejectionReason; record.CapturedAtUtc = asset.CapturedAtUtc;
        record.Latitude = asset.Latitude; record.Longitude = asset.Longitude;
        record.HeadingDegrees = asset.HeadingDegrees; record.DistanceMeters = asset.DistanceMeters;
        record.LastCheckedAtUtc = asset.LastCheckedAtUtc; record.ModeratedAtUtc = asset.ModeratedAtUtc;
        record.ModeratorActorId = asset.ModeratorActorId;
        record.ContributorUserId = asset.ContributorUserId; record.RightsDeclaration = asset.RightsDeclaration;
    }
}
