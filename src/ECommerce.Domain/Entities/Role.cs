using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Rol de autorización (Admin, Customer, ...).</summary>
public class Role : BaseEntity
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
