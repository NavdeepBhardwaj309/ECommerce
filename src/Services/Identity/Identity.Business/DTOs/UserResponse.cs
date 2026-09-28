namespace Identity.Business.DTOs;

/// <summary>Public account details and assigned roles.</summary>
public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);