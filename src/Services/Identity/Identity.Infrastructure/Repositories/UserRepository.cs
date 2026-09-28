using Identity.Business.DTOs;
using Identity.Business.Interfaces.RepositoryInterfaces;
using Identity.Infrastructure.Data;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class UserRepository(
    IdentityDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider) : IUserRepository
{
    public async Task<UserCreationResult> CreateAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = Normalize(email);
        if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return new UserCreationResult(null, ["An account with this email already exists."]);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName,
            CreatedAt = timeProvider.GetUtcNow()
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new UserCreationResult(ToAuthUser(user), []);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new UserCreationResult(null, ["An account with this email already exists."]);
        }
    }

    public async Task<AuthUser?> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = Normalize(email);
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.NormalizedEmail == normalizedEmail,
            cancellationToken);
        if (user is null)
        {
            passwordHasher.HashPassword(new User(), password);
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (user.LockoutEnd > now)
        {
            return null;
        }

        if (result == PasswordVerificationResult.Failed)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= 5)
            {
                user.AccessFailedCount = 0;
                user.LockoutEnd = now.AddMinutes(5);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAuthUser(user);
    }

    public async Task<AuthUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user is null ? null : ToAuthUser(user);
    }

    public async Task<UserUpdateResult?> UpdateAsync(
        Guid userId,
        string email,
        string displayName,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken);
        if (user is null)
        {
            return null;
        }

        var normalizedEmail = Normalize(email);
        if (user.NormalizedEmail != normalizedEmail && await dbContext.Users.AnyAsync(
                item => item.Id != userId && item.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return new UserUpdateResult(null, ["An account with this email already exists."]);
        }

        user.Email = email;
        user.NormalizedEmail = normalizedEmail;
        user.DisplayName = displayName;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new UserUpdateResult(ToAuthUser(user), []);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new UserUpdateResult(null, ["An account with this email already exists."]);
        }
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken);
        if (user is null)
        {
            return false;
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles.AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.Role.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> AddToRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var roleEntity = await dbContext.Roles.SingleOrDefaultAsync(
            item => item.NormalizedName == Normalize(role),
            cancellationToken);
        if (roleEntity is null || !await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            return false;
        }

        if (await dbContext.UserRoles.AnyAsync(
                item => item.UserId == userId && item.RoleId == roleEntity.Id,
                cancellationToken))
        {
            return false;
        }

        dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleEntity.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static AuthUser ToAuthUser(User user) =>
        new(user.Id, user.Email, user.DisplayName);
}