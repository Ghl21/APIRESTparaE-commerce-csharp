using System.Security.Cryptography;
using ECommerce.Application.Interfaces;

namespace ECommerce.Infrastructure.Security;

/// <summary>
/// Hashing de contraseñas con PBKDF2-HMAC-SHA256, sal aleatoria de 16 bytes y 100.000 iteraciones.
/// Formato almacenado: PBKDF2.{iteraciones}.{salBase64}.{hashBase64}
/// No requiere paquetes externos, por lo que la solución compila sin dependencias adicionales.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join('.', Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        var parts = hash.Split('.');

        if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedKey = Convert.FromBase64String(parts[3]);
            var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedKey.Length);

            // Comparación en tiempo constante para no filtrar información por tiempos de respuesta.
            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
