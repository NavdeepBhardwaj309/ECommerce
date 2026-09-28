using System.ComponentModel.DataAnnotations;

namespace Identity.Business.DTOs;

/// <summary>Refresh token to revoke.</summary>
public sealed record RevokeTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}