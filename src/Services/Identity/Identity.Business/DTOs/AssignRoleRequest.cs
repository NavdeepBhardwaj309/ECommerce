using System.ComponentModel.DataAnnotations;

namespace Identity.Business.DTOs;

/// <summary>Role to assign to an account. This operation is restricted to administrators.</summary>
public sealed record AssignRoleRequest
{
    [Required, StringLength(64, MinimumLength = 1)]
    public required string Role { get; init; }
}