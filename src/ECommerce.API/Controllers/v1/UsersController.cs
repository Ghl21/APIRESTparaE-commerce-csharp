using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Administración de usuarios y roles. Requiere rol Admin.</summary>
[Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Lista los usuarios registrados con paginación.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] QueryParameters parameters) =>
        Ok(await _userService.GetAllAsync(parameters, RequestAborted));

    /// <summary>Obtiene un usuario por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(int id) =>
        Ok(await _userService.GetByIdAsync(id, RequestAborted));

    /// <summary>Activa o desactiva una cuenta. Al desactivarla se revocan sus sesiones.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SetActive(int id, [FromQuery] bool isActive) =>
        Ok(await _userService.SetActiveAsync(id, isActive, RequestAborted));

    /// <summary>Asigna un rol a un usuario.</summary>
    [HttpPost("{id:int}/roles")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> AssignRole(int id, [FromBody] AssignRoleRequest request) =>
        Ok(await _userService.AssignRoleAsync(id, request.RoleName, RequestAborted));

    /// <summary>Quita un rol a un usuario. Siempre debe conservar al menos uno.</summary>
    [HttpDelete("{id:int}/roles/{roleName}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> RemoveRole(int id, string roleName) =>
        Ok(await _userService.RemoveRoleAsync(id, roleName, RequestAborted));
}
