using ECommerce.Infrastructure.Security;
using Xunit;

namespace ECommerce.Tests;

/// <summary>
/// Ancla los hashes almacenados en el script 04_DatosIniciales.sql con las contraseñas
/// documentadas. Si alguien cambia el algoritmo de hashing o el script, esta prueba
/// falla y avisa de que la base sembrada dejaría de poder iniciar sesión.
/// </summary>
public class SeedDataHashTests
{
    private readonly PasswordHasher _hasher = new();

    private const string AdminHashEnScript =
        "PBKDF2.100000.n6PZaLxhYaMKbG0+n9LDyQ==.zIN8RyLT0lHE0BdDuiUFzFGXqWkHiaDjOxC258/AM7o=";

    private const string ClienteHashEnScript =
        "PBKDF2.100000.zgrWOsG7bwiYGabVcvUpLg==.LRQujG9CqQ7t8s69bWi/ZS9ei8T6cyJTPBYQLugf3lY=";

    [Fact]
    public void ElHashDelAdministradorSembradoPorSqlCorrespondeAsuContrasena()
    {
        Assert.True(_hasher.Verify("Admin123$", AdminHashEnScript));
    }

    [Fact]
    public void ElHashDelClienteSembradoPorSqlCorrespondeAsuContrasena()
    {
        Assert.True(_hasher.Verify("Cliente123$", ClienteHashEnScript));
    }

    [Fact]
    public void LosHashesSembradosNoAceptanOtraContrasena()
    {
        Assert.False(_hasher.Verify("Cliente123$", AdminHashEnScript));
        Assert.False(_hasher.Verify("Admin123$", ClienteHashEnScript));
    }
}
