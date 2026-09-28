using Identity.Api;
using Identity.Business.DTOs;
using Identity.Business.Interfaces.ServiceInterfaces;
using Identity.Business.Services;
using Identity.Infrastructure;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Identity API",
		Version = "v1"
	});
	options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = JwtBearerDefaults.AuthenticationScheme,
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Enter a JWT access token."
	});
	options.OperationFilter<Identity.Api.Api.AuthorizeOperationFilter>();
});

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new();
if (Encoding.UTF8.GetByteCount(authOptions.SigningKey) < 32)
{
	throw new InvalidOperationException("Configure Jwt:SigningKey with at least 32 bytes using user-secrets or an environment variable.");
}

if (string.IsNullOrWhiteSpace(authOptions.Issuer) ||
	string.IsNullOrWhiteSpace(authOptions.Audience) ||
	authOptions.AccessTokenMinutes <= 0 ||
	authOptions.RefreshTokenDays <= 0)
{
	throw new InvalidOperationException("JWT issuer, audience, and positive token lifetimes must be configured.");
}

builder.Services.AddSingleton(authOptions);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.MapInboundClaims = false;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = authOptions.Issuer,
			ValidateAudience = true,
			ValidAudience = authOptions.Audience,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromSeconds(30),
			NameClaimType = JwtRegisteredClaimNames.Sub,
			RoleClaimType = "role"
		};
	});
builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
	Predicate = _ => false
});
app.MapHealthChecks("/ready");
app.MapIdentityEndpoints();

await IdentitySeeder.SeedAsync(app.Services, builder.Configuration);

app.Run();
