namespace Zuppeto.Domain.Abstractions;
public interface ITotpRecoveryCodeRepository
{
    Task ReplaceAsync(Guid userId, IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default);
    Task<bool> ConsumeAsync(Guid userId, string hash, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}
