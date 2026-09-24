using System.Data;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.TerritorialImports;

internal sealed class TerritorialImportWorkQueue(ZuppetoDbContext db) : ITerritorialImportWorkQueue
{
    public async Task<TerritorialImportWorkItem?> ClaimNextAsync(
        string owner,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var record = await db.TerritorialImports
            .FromSqlInterpolated($@"
                SELECT *
                FROM territorial_imports
                WHERE (status IN ('Queued', 'Uploaded', 'Mapped', 'Publishing')
                       OR (status = 'Validated' AND has_blocking_errors = FALSE))
                  AND cancellation_requested = FALSE
                  AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= {now})
                  AND (lease_expires_at_utc IS NULL OR lease_expires_at_utc <= {now})
                ORDER BY created_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT 1")
            .SingleOrDefaultAsync(ct);

        if (record is null)
        {
            await transaction.CommitAsync(ct);
            return null;
        }

        record.LeaseOwner = owner;
        record.LeaseExpiresAtUtc = now.Add(leaseDuration);
        record.LastHeartbeatAtUtc = now;
        record.ProcessingStartedAtUtc ??= now;
        record.AttemptCount++;
        record.NextAttemptAtUtc = null;
        record.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return ToWorkItem(record, record.Status == "Publishing", owner);
    }

    public async Task HeartbeatAsync(Guid importId, string owner, TimeSpan leaseDuration, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.TerritorialImports
            .Where(x => x.Id == importId && x.LeaseOwner == owner)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.LastHeartbeatAtUtc, now)
                .SetProperty(x => x.LeaseExpiresAtUtc, now.Add(leaseDuration))
                .SetProperty(x => x.UpdatedAtUtc, now), ct);
    }

    public async Task CompleteAsync(Guid importId, string owner, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.TerritorialImports
            .Where(x => x.Id == importId && x.LeaseOwner == owner)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.LeaseOwner, (string?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.NextAttemptAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.LastErrorCode, (string?)null)
                .SetProperty(x => x.LastErrorMessage, (string?)null)
                .SetProperty(x => x.IsRecoverable, false)
                .SetProperty(x => x.LastHeartbeatAtUtc, now)
                .SetProperty(x => x.UpdatedAtUtc, now), ct);
    }

    public async Task FailAsync(
        Guid importId,
        string owner,
        string errorCode,
        string safeMessage,
        bool recoverable,
        int maximumAttempts,
        CancellationToken ct = default)
    {
        var record = await db.TerritorialImports.SingleAsync(x => x.Id == importId && x.LeaseOwner == owner, ct);
        var now = DateTimeOffset.UtcNow;
        var willRetry = recoverable && record.AttemptCount < maximumAttempts;
        if (willRetry)
        {
            if (record.Status != "Publishing") record.Status = "Queued";
            var retrySeconds = Math.Min(60, 2 << Math.Max(0, record.AttemptCount - 1));
            record.NextAttemptAtUtc = now.AddSeconds(retrySeconds);
        }
        else
        {
            record.Status = "Failed";
            record.ProcessingCompletedAtUtc = now;
            record.NextAttemptAtUtc = null;
        }

        record.LastErrorCode = errorCode;
        record.LastErrorMessage = safeMessage.Length <= 1000 ? safeMessage : safeMessage[..1000];
        record.FailureReason = record.LastErrorMessage;
        record.IsRecoverable = willRetry;
        record.LeaseOwner = null;
        record.LeaseExpiresAtUtc = null;
        record.LastHeartbeatAtUtc = now;
        record.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelClaimedAsync(Guid importId, string owner, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.TerritorialImports
            .Where(x => x.Id == importId && x.LeaseOwner == owner)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.Status, "Cancelled")
                .SetProperty(x => x.CancellationRequested, false)
                .SetProperty(x => x.LeaseOwner, (string?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.NextAttemptAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.ProcessingCompletedAtUtc, now)
                .SetProperty(x => x.LastHeartbeatAtUtc, now)
                .SetProperty(x => x.UpdatedAtUtc, now), ct);
    }

    public async Task QueuePublicationAsync(Guid importId, Guid actorUserId, CancellationToken ct = default)
    {
        var record = await db.TerritorialImports.SingleAsync(x => x.Id == importId, ct);
        if (record.Status != "ReadyForReview" || record.HasBlockingErrors)
            throw new InvalidOperationException("La importació no està preparada per publicar.");

        var now = DateTimeOffset.UtcNow;
        record.Status = "Publishing";
        record.PublicationRequestedByUserId = actorUserId;
        record.CurrentStage = "Publication";
        record.ProcessingStartedAtUtc = now;
        record.ProcessingCompletedAtUtc = null;
        record.LastHeartbeatAtUtc = now;
        record.AttemptCount = 0;
        record.LastErrorCode = null;
        record.LastErrorMessage = null;
        record.IsRecoverable = false;
        record.CancellationRequested = false;
        record.LeaseOwner = null;
        record.LeaseExpiresAtUtc = null;
        record.NextAttemptAtUtc = now;
        record.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
    }

    private static TerritorialImportWorkItem ToWorkItem(TerritorialImportRecord record, bool isPublication, string owner) =>
        new(record.Id, record.DatasetSourceId, record.MappingTemplateId, record.ArtifactName,
            record.FileChecksum, record.FileSize, record.DatasetVersion,
            isPublication ? record.PublicationRequestedByUserId ?? record.CreatedByUserId : record.CreatedByUserId,
            isPublication, owner);
}
