namespace Identity.Business.DTOs;

public sealed record UserCreationResult(AuthUser? User, IReadOnlyList<string> Errors);