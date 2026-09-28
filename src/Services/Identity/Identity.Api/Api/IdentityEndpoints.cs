using Identity.Api.Api;
using Identity.Business.DTOs;
using Identity.Business.Interfaces.ServiceInterfaces;
using Identity.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Identity.Api;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var identity = endpoints.MapGroup("/api/identity").WithTags("Identity");
        var auth = identity.MapGroup("/auth");

        auth.MapPost("/register", async Task<IResult> (
            RegisterRequest request,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RegisterAsync(request, cancellationToken);
            return result.StatusCode == 201 && result.Value is not null
                ? TypedResults.Created($"/api/identity/users/{result.Value.Id}", result.Value)
                : ToProblem(result);
        })
        .WithName("RegisterIdentityUser")
        .WithSummary("Register a customer account")
        .WithDescription("Creates an account with the Customer role. Clients cannot select roles during registration.")
        .Produces<UserResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .AddEndpointFilter<RequestValidationFilter<RegisterRequest>>();

        auth.MapPost("/login", async Task<IResult> (
            LoginRequest request,
            IAuthService service,
            CancellationToken cancellationToken) =>
                ToProblemOrOk(await service.LoginAsync(request, cancellationToken)))
        .WithName("LoginIdentityUser")
        .WithSummary("Sign in and issue tokens")
        .Produces<AuthResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .AddEndpointFilter<RequestValidationFilter<LoginRequest>>();

        auth.MapPost("/refresh", async Task<IResult> (
            RefreshTokenRequest request,
            IAuthService service,
            CancellationToken cancellationToken) =>
                ToProblemOrOk(await service.RefreshAsync(request.RefreshToken, cancellationToken)))
        .WithName("RefreshIdentityTokens")
        .WithSummary("Rotate a refresh token")
        .Produces<AuthResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .AddEndpointFilter<RequestValidationFilter<RefreshTokenRequest>>();

        auth.MapPost("/revoke", async Task<IResult> (
            RevokeTokenRequest request,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            await service.RevokeAsync(request.RefreshToken, cancellationToken);
            return TypedResults.NoContent();
        })
        .WithName("RevokeIdentityRefreshToken")
        .WithSummary("Revoke a refresh token")
        .Produces(StatusCodes.Status204NoContent)
        .AddEndpointFilter<RequestValidationFilter<RevokeTokenRequest>>();

        auth.MapGet("/me", async Task<IResult> (
            HttpContext httpContext,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
            {
                return TypedResults.Unauthorized();
            }

            return ToProblemOrOk(await service.GetUserAsync(userId, cancellationToken));
        })
        .WithName("GetCurrentIdentityUser")
        .WithSummary("Get the authenticated account")
        .Produces<UserResponse>()
        .RequireAuthorization();

        identity.MapGet("/users/{userId:guid}", async Task<IResult> (
            Guid userId,
            HttpContext httpContext,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            var subject = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(subject, out var currentUserId) ||
                (currentUserId != userId && !httpContext.User.IsInRole(IdentityRoles.Admin)))
            {
                return TypedResults.Forbid();
            }

            return ToProblemOrOk(await service.GetUserAsync(userId, cancellationToken));
        })
        .WithName("GetIdentityUser")
        .WithSummary("Get an account by ID")
        .Produces<UserResponse>()
        .RequireAuthorization();

        identity.MapPut("/users/{userId:guid}", async Task<IResult> (
            Guid userId,
            UpdateUserRequest request,
            HttpContext httpContext,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            var subject = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(subject, out var currentUserId) ||
                (currentUserId != userId && !httpContext.User.IsInRole(IdentityRoles.Admin)))
            {
                return TypedResults.Forbid();
            }

            return ToProblemOrOk(await service.UpdateUserAsync(userId, request, cancellationToken));
        })
        .WithName("UpdateIdentityUser")
        .WithSummary("Update an account's email and display name")
        .Produces<UserResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization()
        .AddEndpointFilter<RequestValidationFilter<UpdateUserRequest>>();

        identity.MapDelete("/users/{userId:guid}", async Task<IResult> (
            Guid userId,
            HttpContext httpContext,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            var subject = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(subject, out var currentUserId) ||
                (currentUserId != userId && !httpContext.User.IsInRole(IdentityRoles.Admin)))
            {
                return TypedResults.Forbid();
            }

            var result = await service.DeleteUserAsync(userId, cancellationToken);
            return result.StatusCode == StatusCodes.Status204NoContent
                ? TypedResults.NoContent()
                : ToProblem(result);
        })
        .WithName("DeleteIdentityUser")
        .WithSummary("Delete an account")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        identity.MapPost("/admin/users/{userId:guid}/roles", async Task<IResult> (
            Guid userId,
            AssignRoleRequest request,
            IAuthService service,
            CancellationToken cancellationToken) =>
                ToProblemOrOk(await service.AssignRoleAsync(userId, request.Role, cancellationToken)))
        .WithName("AssignIdentityRole")
        .WithSummary("Assign a role to an account")
        .Produces<RoleAssignmentResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(policy => policy.RequireRole(IdentityRoles.Admin))
        .AddEndpointFilter<RequestValidationFilter<AssignRoleRequest>>();

        return endpoints;
    }

    private static IResult ToProblemOrOk<T>(AuthServiceResult<T> result)
        where T : class =>
        result.Value is not null && result.StatusCode is >= 200 and < 300
            ? TypedResults.Ok(result.Value)
            : ToProblem(result);

    private static IResult ToProblem<T>(AuthServiceResult<T> result)
        where T : class =>
        result.StatusCode == 400 && result.Details is { Count: > 0 }
            ? TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["identity"] = result.Details.ToArray()
            })
            : TypedResults.Problem(
                title: result.Error ?? "The request could not be completed.",
                statusCode: result.StatusCode);
}