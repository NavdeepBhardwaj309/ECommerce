namespace Identity.Business.DTOs;

/// <summary>Confirmation that a role was assigned to an account.</summary>
public sealed record RoleAssignmentResponse(Guid UserId, string Role);