using Identity.Domain.Enums;
using Identity.Domain.Entities;
using Identity.Business.Interfaces.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        foreach (var roleName in new[] { IdentityRoles.Customer, IdentityRoles.Admin })
        {
            var normalizedName = roleName.ToUpperInvariant();
            if (!await dbContext.Roles.AnyAsync(role => role.NormalizedName == normalizedName))
            {
                dbContext.Roles.Add(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = normalizedName
                });
            }
        }
        await dbContext.SaveChangesAsync();

        var email = configuration["Identity:BootstrapAdminEmail"];
        var password = configuration["Identity:BootstrapAdminPassword"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both Identity:BootstrapAdminEmail and Identity:BootstrapAdminPassword must be configured.");
        }

        var admin = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.NormalizedEmail == email.Trim().ToUpperInvariant());
        if (admin is null)
        {
            var createResult = await userRepository.CreateAsync(
                email,
                "Administrator",
                password,
                CancellationToken.None);
            if (createResult.User is null)
            {
                throw new InvalidOperationException(
                    "The configured bootstrap administrator could not be created. Verify its password meets the Identity policy.");
            }

            admin = await dbContext.Users.AsNoTracking()
                .SingleAsync(user => user.Id == createResult.User.Id);
        }

        var roles = await userRepository.GetRolesAsync(admin.Id, CancellationToken.None);
        if (!roles.Contains(IdentityRoles.Admin, StringComparer.OrdinalIgnoreCase) &&
            !await userRepository.AddToRoleAsync(admin.Id, IdentityRoles.Admin, CancellationToken.None))
        {
            throw new InvalidOperationException("The bootstrap administrator role could not be assigned.");
        }
    }
}