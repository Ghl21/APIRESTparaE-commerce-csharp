using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Seed;

/// <summary>
/// Siembra idempotente de datos mínimos: roles y usuario administrador.
/// Sirve como red de seguridad cuando la base se creó con los scripts pero sin datos,
/// y nunca sobrescribe información existente.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!await _context.Database.CanConnectAsync(ct))
        {
            _logger.LogWarning(
                "No fue posible conectar con la base de datos. Ejecute los scripts de la carpeta APIRESTparaE-commerce-SqlServer.");
            return;
        }

        await SeedRolesAsync(ct);
        await SeedAdminAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var required = new Dictionary<string, string>
        {
            [Role.Admin] = "Acceso total a la administración del catálogo, pedidos y usuarios.",
            [Role.Customer] = "Cliente de la tienda: compra, carrito y pedidos propios."
        };

        var existing = await _context.Roles.Select(r => r.Name).ToListAsync(ct);

        foreach (var (name, description) in required.Where(r => !existing.Contains(r.Key)))
        {
            _context.Roles.Add(new Role
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Rol {Role} creado por el seeder.", name);
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var email = (_configuration["Seed:AdminEmail"] ?? "admin@ecommerce.com").Trim().ToLowerInvariant();
        var password = _configuration["Seed:AdminPassword"] ?? "Admin123$";

        if (await _context.Users.AnyAsync(u => u.Email == email, ct))
        {
            return;
        }

        var adminRole = await _context.Roles.FirstAsync(r => r.Name == Role.Admin, ct);

        var admin = new User
        {
            FirstName = "Administrador",
            LastName = "General",
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        admin.UserRoles.Add(new UserRole { Role = adminRole, AssignedAt = DateTime.UtcNow });
        admin.Cart = new Cart { CreatedAt = DateTime.UtcNow };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Usuario administrador {Email} creado con la contraseña por defecto. Cámbiela antes de usar la API en producción.",
            email);
    }
}
