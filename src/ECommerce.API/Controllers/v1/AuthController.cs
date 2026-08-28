using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Registro de clientes, inicio de sesión y administración de la sesión.</summary>
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Registra un nuevo cliente y devuelve sus tokens de acceso.</summary>
    /// <response code="201">Usuario creado correctamente.</response>
    /// <response code="409">El correo ya está registrado.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request, RequestAborted);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Inicia sesión con correo y contraseña.</summary>
    /// <response code="200">Autenticación correcta.</response>
    /// <response code="401">Credenciales inválidas o cuenta desactivada.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request) =>
        Ok(await _authService.LoginAsync(request, RequestAborted));

    /// <summary>Genera un nuevo token de acceso a partir de un refresh token vigente.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request) =>
        Ok(await _authService.RefreshTokenAsync(request, RequestAborted));

    /// <summary>Revoca un refresh token (cierre de sesión).</summary>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken, RequestAborted);

        return NoContent();
    }

    /// <summary>Devuelve el perfil del usuario autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me() =>
        Ok(await _authService.GetProfileAsync(UserId, RequestAborted));

    /// <summary>Cambia la contraseña del usuario autenticado y cierra sus demás sesiones.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(UserId, request, RequestAborted);

        return NoContent();
    }
}
