using Identity.Business.Interfaces.RepositoryInterfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDbSqlConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'IdentityDbSqlConnection' is not configured.");
        }

        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}