using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

/// <summary>Generación de tokens JWT y refresh tokens.</summary>
public interface IJwtTokenService
{
    /// <summary>Crea el token de acceso firmado para el usuario y sus roles.</summary>
    (string Token, DateTime ExpiresAtUtc, int ExpiresInSeconds) GenerateAccessToken(User user, IEnumerable<string> roles);

    /// <summary>Genera un refresh token criptográficamente aleatorio.</summary>
    string GenerateRefreshToken();

    /// <summary>Minutos de vigencia configurados para el refresh token.</summary>
    int RefreshTokenExpirationDays { get; }
}
