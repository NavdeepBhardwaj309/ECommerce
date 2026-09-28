using System.ComponentModel.DataAnnotations;

namespace Identity.Business.DTOs;

public sealed record UpdateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public required string DisplayName { get; init; }
}