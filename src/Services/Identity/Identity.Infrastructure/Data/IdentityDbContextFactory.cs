using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Data;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var apiSettingsPath = FindApiSettingsPath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiSettingsPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<IdentityDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("IdentityDbSqlConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings__IdentityDbSqlConnection or the local Identity Development settings before running EF migrations.");
        }

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new IdentityDbContext(options);
    }

    private static string FindApiSettingsPath()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "Services",
                "Identity",
                "Identity.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Run EF migration commands from the ECommerce repository or configure the connection string through environment variables.");
    }
}