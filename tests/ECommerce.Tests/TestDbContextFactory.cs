using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests;

/// <summary>Crea contextos en memoria con datos de prueba para los tests de servicios.</summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>Siembra un usuario, una categoría y dos productos con stock conocido.</summary>
    public static async Task<ApplicationDbContext> CreateWithCatalogAsync(string databaseName)
    {
        var context = Create(databaseName);

        var customerRole = new Role { Id = 2, Name = Role.Customer, Description = "Cliente" };

        var user = new User
        {
            Id = 1,
            FirstName = "Ana",
            LastName = "López",
            Email = "ana@test.com",
            PasswordHash = "PBKDF2.1.AAAA.BBBB",
            IsActive = true
        };

        user.UserRoles.Add(new UserRole { UserId = 1, RoleId = 2, Role = customerRole });

        var category = new Category { Id = 1, Name = "Electrónica", Slug = "electronica", IsActive = true };

        var laptop = new Product
        {
            Id = 1,
            Sku = "LAP-001",
            Name = "Laptop Pro 14",
            Slug = "laptop-pro-14",
            Price = 1200m,
            Stock = 10,
            CategoryId = 1,
            IsActive = true
        };

        var mouse = new Product
        {
            Id = 2,
            Sku = "MOU-001",
            Name = "Mouse Inalámbrico",
            Slug = "mouse-inalambrico",
            Price = 25m,
            DiscountPrice = 19.99m,
            Stock = 3,
            CategoryId = 1,
            IsActive = true
        };

        var address = new Address
        {
            Id = 1,
            UserId = 1,
            Alias = "Casa",
            RecipientName = "Ana López",
            Street = "Av. Siempre Viva 742",
            City = "Ciudad",
            State = "Estado",
            PostalCode = "01000",
            Country = "México",
            IsDefault = true
        };

        context.Roles.Add(customerRole);
        context.Users.Add(user);
        context.Categories.Add(category);
        context.Products.AddRange(laptop, mouse);
        context.Addresses.Add(address);
        context.Carts.Add(new Cart { Id = 1, UserId = 1 });

        await context.SaveChangesAsync();

        return context;
    }
}
