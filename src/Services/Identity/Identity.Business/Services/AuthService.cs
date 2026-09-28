using Identity.Business.DTOs;
using Identity.Business.Interfaces.RepositoryInterfaces;
using Identity.Business.Interfaces.ServiceInterfaces;
using Identity.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Business.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    AuthOptions options,
    TimeProvider timeProvider) : IAuthService
{
    private readonly AuthOptions _options = options;

    public async Task<AuthServiceResult<UserResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var creation = await userRepository.CreateAsync(
            request.Email.Trim(),
            request.DisplayName.Trim(),
            request.Password,
            cancellationToken);

        if (creation.User is null)
        {
            var isConflict = creation.Errors.Any(error =>
                error.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("duplicate", StringComparison.OrdinalIgnoreCase));

            return new AuthServiceResult<UserResponse>(
                null,
                isConflict ? 409 : 400,
                isConflict ? "An account with this email already exists." : "Registration failed.",
                isConflict ? null : creation.Errors);
        }

        if (!await userRepository.AddToRoleAsync(
                creation.User.Id,
                IdentityRoles.Customer,
                cancellationToken))
        {
            return new AuthServiceResult<UserResponse>(null, 500, "Registration could not be completed.");
        }

        return new AuthServiceResult<UserResponse>(
            await ToUserResponseAsync(creation.User, cancellationToken), 201);
    }

    public async Task<AuthServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.SignInAsync(
            request.Email.Trim(),
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return new AuthServiceResult<AuthResponse>(null, 401, "Invalid email or password.");
        }

        return new AuthServiceResult<AuthResponse>(
            await CreateAuthResponseAsync(user, cancellationToken), 200);
    }

    public async Task<AuthServiceResult<AuthResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var replacement = CreateRefreshToken();
        var replacementHash = HashToken(replacement);
        var replacementExpiresAt = now.AddDays(_options.RefreshTokenDays);

        var userId = await refreshTokenRepository.RotateAsync(
            HashToken(refreshToken),
            replacementHash,
            now,
            replacementExpiresAt,
            cancellationToken);

        if (userId is null)
        {
            return new AuthServiceResult<AuthResponse>(null, 401, "Invalid or expired refresh token.");
        }

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return new AuthServiceResult<AuthResponse>(null, 401, "Invalid or expired refresh token.");
        }

        var roles = await userRepository.GetRolesAsync(user.Id, cancellationToken);
        var accessToken = tokenService.CreateAccessToken(user, roles);

        return new AuthServiceResult<AuthResponse>(new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            replacement,
            replacementExpiresAt), 200);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken cancellationToken) =>
        refreshTokenRepository.RevokeAsync(
            HashToken(refreshToken),
            timeProvider.GetUtcNow(),
            cancellationToken);

    public async Task<AuthServiceResult<UserResponse>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? new AuthServiceResult<UserResponse>(null, 404, "Account not found.")
            : new AuthServiceResult<UserResponse>(await ToUserResponseAsync(user, cancellationToken), 200);
    }

    public async Task<AuthServiceResult<UserResponse>> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var update = await userRepository.UpdateAsync(
            userId,
            request.Email.Trim(),
            request.DisplayName.Trim(),
            cancellationToken);

        if (update is null)
        {
            return new AuthServiceResult<UserResponse>(null, 404, "Account not found.");
        }

        if (update.User is null)
        {
            var isConflict = update.Errors.Any(error =>
                error.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("duplicate", StringComparison.OrdinalIgnoreCase));

            return new AuthServiceResult<UserResponse>(
                null,
                isConflict ? 409 : 400,
                isConflict ? "An account with this email already exists." : "Account update failed.",
                isConflict ? null : update.Errors);
        }

        return new AuthServiceResult<UserResponse>(
            await ToUserResponseAsync(update.User, cancellationToken), 200);
    }

    public async Task<AuthServiceResult<UserResponse>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var deleted = await userRepository.DeleteAsync(userId, cancellationToken);
        return deleted
            ? new AuthServiceResult<UserResponse>(null, 204)
            : new AuthServiceResult<UserResponse>(null, 404, "Account not found.");
    }

    public async Task<AuthServiceResult<RoleAssignmentResponse>> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var normalizedRole = role.Trim();
        if (normalizedRole is not IdentityRoles.Customer and not IdentityRoles.Admin)
        {
            return new AuthServiceResult<RoleAssignmentResponse>(null, 400, "Unsupported role.");
        }

        if (await userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            return new AuthServiceResult<RoleAssignmentResponse>(null, 404, "Account not found.");
        }

        if (!await userRepository.AddToRoleAsync(userId, normalizedRole, cancellationToken))
        {
            return new AuthServiceResult<RoleAssignmentResponse>(null, 409, "Role could not be assigned.");
        }

        return new AuthServiceResult<RoleAssignmentResponse>(
            new RoleAssignmentResponse(userId, normalizedRole), 200);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        AuthUser user,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var refreshToken = CreateRefreshToken();
        var refreshTokenExpiresAt = now.AddDays(_options.RefreshTokenDays);

        await refreshTokenRepository.StoreAsync(
            user.Id,
            HashToken(refreshToken),
            now,
            refreshTokenExpiresAt,
            cancellationToken);

        var roles = await userRepository.GetRolesAsync(user.Id, cancellationToken);
        var accessToken = tokenService.CreateAccessToken(user, roles);

        return new AuthResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    private async Task<UserResponse> ToUserResponseAsync(
        AuthUser user,
        CancellationToken cancellationToken) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            await userRepository.GetRolesAsync(user.Id, cancellationToken));

    private static string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}