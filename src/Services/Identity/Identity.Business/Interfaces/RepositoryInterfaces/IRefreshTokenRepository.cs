namespace Identity.Business.Interfaces.RepositoryInterfaces;

public interface IRefreshTokenRepository
{
    Task StoreAsync(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<Guid?> RotateAsync(
        string currentTokenHash,
        string replacementTokenHash,
        DateTimeOffset now,
        DateTimeOffset replacementExpiresAt,
        CancellationToken cancellationToken);

    Task RevokeAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
}