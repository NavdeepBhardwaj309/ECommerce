using System.ComponentModel.DataAnnotations;

namespace Identity.Business.DTOs;

/// <summary>Credentials for signing in to an Identity account.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required, MaxLength(128)]
    public required string Password { get; init; }
}