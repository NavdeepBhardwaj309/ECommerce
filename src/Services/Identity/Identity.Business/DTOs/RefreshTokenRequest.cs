using System.ComponentModel.DataAnnotations;

namespace Identity.Business.DTOs;

/// <summary>Refresh token to exchange for a new access and refresh token pair.</summary>
public sealed record RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}