using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Libreta de direcciones del usuario autenticado.</summary>
[Authorize]
public class AddressesController : BaseApiController
{
    private readonly IAddressService _addressService;

    public AddressesController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    /// <summary>Lista las direcciones del usuario, con la predeterminada primero.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AddressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetMine() =>
        Ok(await _addressService.GetMineAsync(UserId, RequestAborted));

    /// <summary>Obtiene una dirección propia por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> GetById(int id) =>
        Ok(await _addressService.GetByIdAsync(id, UserId, RequestAborted));

    /// <summary>Registra una nueva dirección. La primera queda como predeterminada.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AddressDto>> Create([FromBody] SaveAddressRequest request)
    {
        var address = await _addressService.CreateAsync(UserId, request, RequestAborted);

        return CreatedAtAction(nameof(GetById), new { id = address.Id }, address);
    }

    /// <summary>Actualiza una dirección propia.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressDto>> Update(int id, [FromBody] SaveAddressRequest request) =>
        Ok(await _addressService.UpdateAsync(id, UserId, request, RequestAborted));

    /// <summary>Elimina una dirección propia.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _addressService.DeleteAsync(id, UserId, RequestAborted);

        return NoContent();
    }
}
