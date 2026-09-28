namespace Identity.Business.DTOs;

/// <summary>Short-lived access token and rotating refresh token credentials.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);