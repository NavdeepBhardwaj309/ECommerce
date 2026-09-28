namespace Identity.Business.DTOs;

public sealed record UserUpdateResult(
    AuthUser? User,
    IReadOnlyList<string> Errors);