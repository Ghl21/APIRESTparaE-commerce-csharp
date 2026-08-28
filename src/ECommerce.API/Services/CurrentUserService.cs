using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using ECommerce.Infrastructure.Security;

namespace ECommerce.API.Services;

/// <summary>Lee los datos del usuario autenticado desde los claims del token.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var value = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin =>
        User?.IsInRole(Role.Admin) ?? false;

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public int GetRequiredUserId() =>
        UserId ?? throw new AuthenticationException("No hay un usuario autenticado en la petición actual.");

    /// <summary>Nombre del claim de rol emitido por la API.</summary>
    public static string RoleClaimType => JwtTokenService.RoleClaimType;
}
