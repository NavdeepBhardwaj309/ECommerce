namespace Identity.Domain.Entities;

public sealed class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}