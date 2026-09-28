namespace Identity.Business.DTOs;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);