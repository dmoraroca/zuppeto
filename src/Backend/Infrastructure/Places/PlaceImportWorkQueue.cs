using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuppeto.Application.Places;
using Zuppeto.Infrastructure.Persistence;

namespace Zuppeto.Infrastructure.Places;

internal sealed class PlaceImportWorkQueue(ZuppetoDbContext db) : IPlaceImportWorkQueue
{
    public async Task<PlaceImportWorkItem?> ClaimNextAsync(string owner, TimeSpan leaseDuration, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var record = await db.PlaceImportRuns.FromSqlInterpolated($@"
            SELECT * FROM place_import_runs
            WHERE cancellation_requested = FALSE AND (
                (status = 'Prepared'
                 AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= {now})
                 AND (lease_expires_at_utc IS NULL OR lease_expires_at_utc <= {now}))
                OR (status = 'Running' AND lease_expires_at_utc <= {now})
            )
            ORDER BY created_at_utc,id FOR UPDATE SKIP LOCKED LIMIT 1").Include(x=>x.Source).SingleOrDefaultAsync(ct);
        if (record is null) { await transaction.CommitAsync(ct); return null; }
        record.Status="Running"; record.Stage="Discovery"; record.LeaseOwner=owner; record.LeaseExpiresAtUtc=now.Add(leaseDuration);
        record.LastHeartbeatAtUtc=now; record.ProcessingStartedAtUtc??=now; record.AttemptCount++; record.NextAttemptAtUtc=null; record.UpdatedAtUtc=now;
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return new(record.Id,record.Source.SourceKey,record.AccessMode,new(record.CountryId,record.TerritorialUnitId,record.CountryIso2,record.TerritorialName,
            record.OfficialCodeScheme,record.OfficialCode,JsonSerializer.Deserialize<string[]>(record.CategoriesJson)??[],null,int.MaxValue),owner);
    }

    public Task HeartbeatAsync(Guid id,string owner,TimeSpan lease,CancellationToken ct=default) { var now=DateTimeOffset.UtcNow; return db.PlaceImportRuns.Where(x=>x.Id==id&&x.LeaseOwner==owner).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.LastHeartbeatAtUtc,now).SetProperty(x=>x.LeaseExpiresAtUtc,now.Add(lease)).SetProperty(x=>x.UpdatedAtUtc,now),ct); }
    public Task CompleteAsync(Guid id,string owner,CancellationToken ct=default) => db.PlaceImportRuns.Where(x=>x.Id==id&&x.LeaseOwner==owner).ExecuteUpdateAsync(u=>u.SetProperty(x=>x.LeaseOwner,(string?)null).SetProperty(x=>x.LeaseExpiresAtUtc,(DateTimeOffset?)null).SetProperty(x=>x.NextAttemptAtUtc,(DateTimeOffset?)null),ct);
    public async Task FailAsync(Guid id,string owner,string code,string message,bool recoverable,int maximumAttempts,CancellationToken ct=default)
    {
        var x=await db.PlaceImportRuns.SingleAsync(r=>r.Id==id&&r.LeaseOwner==owner,ct); var now=DateTimeOffset.UtcNow;
        var retry=recoverable&&x.AttemptCount<maximumAttempts; x.Status=retry?"Prepared":"Failed"; x.Stage=retry?"RetryScheduled":"Failed";
        x.NextAttemptAtUtc=retry?now.AddSeconds(Math.Min(60,2<<Math.Max(0,x.AttemptCount-1))):null; x.ErrorCode=code; x.ErrorMessage=message[..Math.Min(1000,message.Length)]; x.IsRecoverable=retry; x.LeaseOwner=null; x.LeaseExpiresAtUtc=null; x.LastHeartbeatAtUtc=now; x.UpdatedAtUtc=now; if(!retry)x.ProcessingCompletedAtUtc=now; await db.SaveChangesAsync(ct);
    }
}
