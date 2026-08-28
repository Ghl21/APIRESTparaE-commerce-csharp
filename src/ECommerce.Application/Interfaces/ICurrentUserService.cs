namespace ECommerce.Application.Interfaces;

/// <summary>Expone los datos del usuario autenticado en la petición actual.</summary>
public interface ICurrentUserService
{
    int? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }

    string? IpAddress { get; }

    /// <summary>Devuelve el identificador del usuario o lanza una excepción si no hay sesión.</summary>
    int GetRequiredUserId();
}
