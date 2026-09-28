using System.ComponentModel.DataAnnotations;

namespace Identity.Business.DTOs;

/// <summary>Details for creating an Identity account.</summary>
public sealed record RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required, MinLength(12), MaxLength(128)]
    public required string Password { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public required string DisplayName { get; init; }
}