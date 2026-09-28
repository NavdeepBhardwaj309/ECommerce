using Identity.Business.Interfaces.RepositoryInterfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(IdentityDbContext dbContext) : IRefreshTokenRepository
{
    public async Task StoreAsync(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> RotateAsync(
        string currentTokenHash,
        string replacementTokenHash,
        DateTimeOffset now,
        DateTimeOffset replacementExpiresAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == currentTokenHash, cancellationToken);

        if (current is null || current.RevokedAt is not null || current.ExpiresAt <= now)
        {
            return null;
        }

        current.RevokedAt = now;
        current.ReplacedByTokenHash = replacementTokenHash;
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = current.UserId,
            TokenHash = replacementTokenHash,
            CreatedAt = now,
            ExpiresAt = replacementExpiresAt
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return current.UserId;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }
    }

    public async Task RevokeAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAt is not null)
        {
            return;
        }

        token.RevokedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}