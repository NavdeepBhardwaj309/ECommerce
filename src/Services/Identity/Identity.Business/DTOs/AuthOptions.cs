namespace Identity.Business.DTOs;

public sealed class AuthOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ECommerce.Identity";
    public string Audience { get; init; } = "ECommerce.Api";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}