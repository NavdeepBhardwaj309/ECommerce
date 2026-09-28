using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Data;

public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(64).IsRequired();
            entity.Property(role => role.NormalizedName).HasMaxLength(64).IsRequired();
            entity.HasIndex(role => role.NormalizedName).IsUnique();
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
            entity.Property<byte[]>("Version").IsRowVersion();
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}