namespace ECommerce.Application.Interfaces;

/// <summary>Servicio de hashing de contraseñas.</summary>
public interface IPasswordHasher
{
    /// <summary>Genera el hash con sal aleatoria en formato PBKDF2.</summary>
    string Hash(string password);

    /// <summary>Verifica una contraseña en texto plano contra el hash almacenado.</summary>
    bool Verify(string password, string hash);
}
