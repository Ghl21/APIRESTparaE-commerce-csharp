using System.ComponentModel.DataAnnotations;

namespace ECommerce.Infrastructure.Security;

/// <summary>Configuración de JWT leída de la sección "Jwt" de appsettings.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Emisor del token.</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audiencia válida del token.</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Clave simétrica usada para firmar (HS256). Debe tener al menos 32 caracteres.
    /// En producción debe provenir de variables de entorno o de un almacén de secretos.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "La clave de firma JWT debe tener al menos 32 caracteres.")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Vigencia del token de acceso en minutos.</summary>
    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>Vigencia del refresh token en días.</summary>
    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>Tolerancia de reloj aceptada al validar la expiración.</summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 0;
}
