using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Controlador base con las convenciones comunes de la API.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public abstract class BaseApiController : ControllerBase
{
    private ICurrentUserService? _currentUser;

    /// <summary>Datos del usuario autenticado en la petición actual.</summary>
    protected ICurrentUserService CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    /// <summary>Identificador del usuario autenticado.</summary>
    protected int UserId => CurrentUser.GetRequiredUserId();

    /// <summary>Indica si el usuario autenticado tiene el rol Admin.</summary>
    protected bool IsAdmin => CurrentUser.IsAdmin;

    /// <summary>Token de cancelación asociado a la petición HTTP.</summary>
    protected CancellationToken RequestAborted => HttpContext.RequestAborted;
}
