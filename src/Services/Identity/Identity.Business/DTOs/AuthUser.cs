namespace Identity.Business.DTOs;

public sealed record AuthUser(Guid Id, string Email, string DisplayName);