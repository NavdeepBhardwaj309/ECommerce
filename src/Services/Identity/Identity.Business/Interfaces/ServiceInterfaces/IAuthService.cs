using Identity.Business.DTOs;

namespace Identity.Business.Interfaces.ServiceInterfaces;

public interface IAuthService
{
    Task<AuthServiceResult<UserResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<AuthServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<AuthServiceResult<AuthResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);

    Task<AuthServiceResult<UserResponse>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<AuthServiceResult<UserResponse>> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken);

    Task<AuthServiceResult<UserResponse>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<AuthServiceResult<RoleAssignmentResponse>> AssignRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken);
}