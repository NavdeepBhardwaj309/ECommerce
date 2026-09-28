using Identity.Business.DTOs;

namespace Identity.Business.Interfaces.RepositoryInterfaces;

public interface IUserRepository
{
    Task<UserCreationResult> CreateAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken);

    Task<AuthUser?> SignInAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserUpdateResult?> UpdateAsync(
        Guid userId,
        string email,
        string displayName,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken);
}