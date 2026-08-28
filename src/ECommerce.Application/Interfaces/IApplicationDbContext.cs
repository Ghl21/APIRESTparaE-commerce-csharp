using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Abstracción del contexto de datos usada por los servicios de aplicación.
/// Permite que la capa de aplicación no dependa de la implementación concreta de EF Core.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<Role> Roles { get; }

    DbSet<UserRole> UserRoles { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Address> Addresses { get; }

    DbSet<Category> Categories { get; }

    DbSet<Product> Products { get; }

    DbSet<ProductImage> ProductImages { get; }

    DbSet<Review> Reviews { get; }

    DbSet<Cart> Carts { get; }

    DbSet<CartItem> CartItems { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    DbSet<Payment> Payments { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
